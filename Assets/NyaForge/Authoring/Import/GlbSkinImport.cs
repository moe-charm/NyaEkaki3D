using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>A static mesh plus the bounded rest skeleton and normalized skin weights read from GLB.</summary>
    public sealed class ImportedSkinnedMeshSource
    {
        public string SourceHash { get; }
        public string Format { get; }
        public MeshData Mesh { get; }
        public MorphSet Morphs { get; }
        public SkeletonDefinition Skeleton { get; }
        public SkinBinding Binding { get; }
        public ImportedBoneMap BoneMap { get; }
        public IReadOnlyDictionary<int, Vec3> SourceNodeOrigins { get; }
        public IReadOnlyList<string> Warnings { get; }

        internal ImportedSkinnedMeshSource(string sourceHash, MeshData mesh, MorphSet morphs, SkeletonDefinition skeleton, SkinBinding binding, IEnumerable<string> warnings, IDictionary<int, string> nodeToBone, IDictionary<int, Vec3> nodeOrigins)
        {
            Checks.HashText(sourceHash); Checks.Require(mesh != null && skeleton != null && binding != null, "INVALID_IMPORT", "Skinned GLB result is incomplete.");
            Checks.Require(binding.MeshTopologyHash == mesh.TopologyHash && binding.SkeletonHash == skeleton.ContentHash, "INVALID_IMPORT", "Skinned GLB identities are inconsistent.");
            SourceHash = sourceHash; Format = "glb.v2.skin.v1"; Mesh = mesh; Morphs = morphs; Skeleton = skeleton; Binding = binding;
            BoneMap = new ImportedBoneMap(sourceHash, skeleton, nodeToBone);
            Checks.Require(nodeOrigins != null && nodeOrigins.Count == nodeToBone.Count && nodeToBone.Keys.All(nodeOrigins.ContainsKey), "INVALID_IMPORT", "Source node origins must cover imported joints.");
            foreach (var origin in nodeOrigins.Values) Checks.Finite(origin);
            SourceNodeOrigins = new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(new Dictionary<int, Vec3>(nodeOrigins));
            Warnings = Array.AsReadOnly((warnings ?? Array.Empty<string>()).ToArray());
        }
    }

    /// <summary>
    /// Imports the deliberately narrow translation-only GLB skin profile. It keeps one mesh,
    /// all of its triangle primitives, one skin and four-or-fewer influences per vertex.
    /// General node rotation/scale, multiple skins and VRM metadata belong to later adapters.
    /// </summary>
    public static class GlbSkinImporter
    {
        /// <summary>Reads only the bounded container and reports whether a non-empty skin array exists.</summary>
        public static bool ContainsSkin(byte[] bytes)
        {
            var skins = GlbDocumentReader.Read(bytes).Root["skins"] as JArray;
            return skins != null && skins.Count > 0;
        }

        public static ImportedSkinnedMeshSource Read(byte[] bytes)
        {
            var document = GlbDocumentReader.Read(bytes);
            return Parse(document);
        }

        static ImportedSkinnedMeshSource Parse(GlbDocument document)
        {
            var root = document.Root;
            var meshes = Array(root, "meshes"); Checks.Require(meshes.Count == 1, "UNSUPPORTED_FORMAT", "Import one mesh at a time.");
            var meshToken = meshes[0] as JObject; Checks.Require(meshToken != null, "INVALID_IMPORT", "GLB mesh is invalid.");
            var primitives = Array(meshToken, "primitives"); Checks.Require(primitives.Count > 0 && primitives.Count <= AuthoringLimits.MaxSubmeshes, "BUDGET_EXCEEDED", "GLB primitive count exceeds the submesh budget.");
            var skins = Array(root, "skins"); Checks.Require(skins.Count == 1, "UNSUPPORTED_FORMAT", "A skinned import requires exactly one GLB skin.");
            var skin = skins[0] as JObject; Checks.Require(skin != null, "INVALID_IMPORT", "GLB skin is invalid.");
            var nodes = Array(root, "nodes");
            var joints = Array(skin, "joints"); Checks.Require(joints.Count > 0 && joints.Count <= SkeletonDefinition.MaxBones, "BUDGET_EXCEEDED", "GLB joint count exceeds the skeleton budget.");
            var jointNodes = joints.Select(token => IntToken(token, 0, nodes.Count - 1, "skin joint")).ToArray();
            Checks.Require(jointNodes.Distinct().Count() == jointNodes.Length, "INVALID_SKELETON", "GLB skin repeats a joint node.");
            var parentByNode = ReadParents(nodes);
            var local = nodes.Select(ReadLocalTranslation).ToArray();
            var world = new Vec3[nodes.Count]; var state = new byte[nodes.Count];
            for (int i = 0; i < nodes.Count; i++) ResolveWorld(i, parentByNode, local, world, state);
            var jointToBone = BuildSkeleton(document.SourceHash, skin, nodes, joints, jointNodes, parentByNode, world, document.Bin, root);

            // Reuse the static mesh adapter after removing only skin attributes from a cloned JSON tree.
            // This keeps geometry/morph parsing in one module and preserves the original source hash.
            var staticRoot = (JObject)root.DeepClone(); staticRoot.Remove("skins");
            var staticMeshes = (JArray)staticRoot["meshes"]; var staticMesh = (JObject)staticMeshes[0];
            foreach (var token in (JArray)staticMesh["primitives"])
            {
                var primitive = token as JObject; Checks.Require(primitive != null, "INVALID_IMPORT", "GLB primitive is invalid.");
                var attributes = primitive["attributes"] as JObject; Checks.Require(attributes != null, "INVALID_IMPORT", "GLB primitive attributes are required.");
                attributes.Remove("JOINTS_0"); attributes.Remove("WEIGHTS_0");
            }
            var baseSource = GlbImporter.ReadDocument(new GlbDocument(staticRoot, document.Bin, document.SourceHash));
            var rawWeights = new List<SkinBinding.VertexWeightInput>(); int vertexOffset = 0;
            for (int p = 0; p < primitives.Count; p++)
            {
                var primitive = (JObject)primitives[p]; var attributes = primitive["attributes"] as JObject; Checks.Require(attributes != null, "INVALID_IMPORT", "GLB primitive attributes are required.");
                Checks.Require(attributes["JOINTS_0"] != null && attributes["WEIGHTS_0"] != null, "UNSUPPORTED_FORMAT", "Every skinned primitive needs JOINTS_0 and WEIGHTS_0.");
                Checks.Require(attributes["JOINTS_1"] == null && attributes["WEIGHTS_1"] == null, "UNSUPPORTED_FORMAT", "Only one four-influence skin set is supported.");
                int positionCount = AccessorCount(Array(root, "accessors"), IntProperty(attributes, "POSITION", 0, int.MaxValue, "POSITION"));
                var jointValues = ReadJointVectors(Array(root, "accessors"), Array(root, "bufferViews"), document.Bin, IntProperty(attributes, "JOINTS_0", 0, int.MaxValue, "JOINTS_0"));
                var weightValues = ReadFloatVectors(Array(root, "accessors"), Array(root, "bufferViews"), document.Bin, IntProperty(attributes, "WEIGHTS_0", 0, int.MaxValue, "WEIGHTS_0"), "VEC4", "weights");
                Checks.Require(jointValues.Length == positionCount && weightValues.Length == positionCount, "INVALID_IMPORT", "Skin attribute count differs from POSITION.");
                for (int v = 0; v < positionCount; v++)
                    for (int i = 0; i < 4; i++) if (weightValues[v][i] > 0) { Checks.Require(jointValues[v][i] < jointNodes.Length, "INVALID_IMPORT", "Skin joint index is outside the skin."); rawWeights.Add(new SkinBinding.VertexWeightInput(vertexOffset + v, jointToBone.BoneIds[jointValues[v][i]], weightValues[v][i])); }
                vertexOffset += positionCount;
            }
            Checks.Require(vertexOffset == baseSource.Mesh.VertexCount, "INVALID_IMPORT", "Skin vertex count differs from imported mesh.");
            var binding = SkinBinding.Create(baseSource.Mesh, jointToBone.Skeleton, rawWeights);
            var warnings = new List<string>(baseSource.Warnings) { "GLB skin weights were imported into a translation-only rest skeleton; inverse-bind rotation and scale are outside this adapter." };
            return new ImportedSkinnedMeshSource(document.SourceHash, baseSource.Mesh, baseSource.Morphs, jointToBone.Skeleton, binding, warnings, jointToBone.NodeToBone, jointNodes.ToDictionary(node => node, node => world[node]));
        }

        sealed class SkeletonResult
        {
            public SkeletonDefinition Skeleton; public string[] BoneIds; public Dictionary<int, string> NodeToBone;
        }

        static SkeletonResult BuildSkeleton(string sourceHash, JObject skin, JArray nodes, JArray joints, int[] jointNodes, Dictionary<int, int> parentByNode, Vec3[] world, byte[] bin, JObject root)
        {
            var ids = jointNodes.ToDictionary(node => node, node => StableId(sourceHash + ":bone:" + node.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Vec3[] heads = jointNodes.Select(node => world[node]).ToArray();
            var inverseToken = skin["inverseBindMatrices"]; if (inverseToken != null)
            {
                var matrices = ReadFloatVectors(Array(root, "accessors"), Array(root, "bufferViews"), bin, IntToken(inverseToken, 0, int.MaxValue, "inverseBindMatrices"), "MAT4", "inverse bind matrix");
                Checks.Require(matrices.Length == jointNodes.Length, "INVALID_IMPORT", "Inverse bind matrix count differs from joints.");
                for (int i = 0; i < matrices.Length; i++) { ValidateTranslationMatrix(matrices[i], "inverse bind matrix"); heads[i] = new Vec3(-matrices[i][12], -matrices[i][13], -matrices[i][14]); }
            }
            var bones = new List<BoneDefinition>(jointNodes.Length);
            for (int i = 0; i < jointNodes.Length; i++)
            {
                int node = jointNodes[i]; string parent = parentByNode.ContainsKey(node) && ids.ContainsKey(parentByNode[node]) ? ids[parentByNode[node]] : "";
                Vec3 tail = heads[i] + new Vec3(0, .05f, 0);
                var nodeToken = (JObject)nodes[node]; var children = nodeToken["children"] as JArray;
                if (children != null) foreach (var childToken in children) { int child = IntToken(childToken, 0, nodes.Count - 1, "node child"); if (ids.ContainsKey(child)) { var candidate = heads[System.Array.IndexOf(jointNodes, child)]; if (DistanceSquared(heads[i], candidate) > 1e-12) { tail = candidate; break; } } }
                bones.Add(new BoneDefinition(ids[node], NodeName(nodeToken, i), parent, heads[i], tail));
            }
            return new SkeletonResult { Skeleton = new SkeletonDefinition(bones), BoneIds = jointNodes.Select(node => ids[node]).ToArray(), NodeToBone = ids };
        }

        static string NodeName(JObject node, int index) { var name = node["name"]; return name != null && name.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)name) ? (string)name : "Joint " + index.ToString(System.Globalization.CultureInfo.InvariantCulture); }

        static Dictionary<int, int> ReadParents(JArray nodes)
        {
            var result = new Dictionary<int, int>();
            for (int i = 0; i < nodes.Count; i++) { var node = nodes[i] as JObject; Checks.Require(node != null, "INVALID_IMPORT", "GLB node is invalid."); var children = node["children"] as JArray; if (children == null) continue; foreach (var childToken in children) { int child = IntToken(childToken, 0, nodes.Count - 1, "node child"); Checks.Require(!result.ContainsKey(child), "INVALID_SKELETON", "GLB node has multiple parents."); result[child] = i; } }
            return result;
        }

        static Vec3 ReadLocalTranslation(JToken token)
        {
            var node = token as JObject; Checks.Require(node != null, "INVALID_IMPORT", "GLB node is invalid.");
            Checks.Require(!(node["matrix"] != null && (node["translation"] != null || node["rotation"] != null || node["scale"] != null)), "UNSUPPORTED_FORMAT", "GLB node cannot mix matrix and TRS transforms.");
            if (node["matrix"] != null) { var matrix = Numbers(node["matrix"], 16, "node matrix"); ValidateTranslationMatrix(matrix, "node matrix"); return new Vec3(matrix[12], matrix[13], matrix[14]); }
            if (node["rotation"] != null) { var rotation = Numbers(node["rotation"], 4, "node rotation"); Checks.Require(Math.Abs(rotation[0]) < 1e-5 && Math.Abs(rotation[1]) < 1e-5 && Math.Abs(rotation[2]) < 1e-5 && Math.Abs(rotation[3] - 1) < 1e-5, "UNSUPPORTED_FORMAT", "Only identity node rotation is supported."); }
            if (node["scale"] != null) { var scale = Numbers(node["scale"], 3, "node scale"); Checks.Require(Math.Abs(scale[0] - 1) < 1e-5 && Math.Abs(scale[1] - 1) < 1e-5 && Math.Abs(scale[2] - 1) < 1e-5, "UNSUPPORTED_FORMAT", "Only unit node scale is supported."); }
            var translation = node["translation"]; if (translation == null) return new Vec3(); var values = Numbers(translation, 3, "node translation"); return new Vec3(values[0], values[1], values[2]);
        }

        static Vec3 ResolveWorld(int index, Dictionary<int, int> parents, Vec3[] local, Vec3[] world, byte[] state)
        {
            if (state[index] == 2) return world[index]; Checks.Require(state[index] == 0, "BONE_CYCLE", "GLB node hierarchy contains a cycle."); state[index] = 1; world[index] = parents.ContainsKey(index) ? ResolveWorld(parents[index], parents, local, world, state) + local[index] : local[index]; state[index] = 2; return world[index];
        }

        static int[][] ReadJointVectors(JArray accessors, JArray views, byte[] bin, int id)
        {
            var accessor = Accessor(accessors, id, "VEC4", new[] { 5121, 5123 }); Checks.Require(accessor["normalized"] == null || (bool)accessor["normalized"] == false, "UNSUPPORTED_FORMAT", "Normalized JOINTS_0 is not supported."); return ReadIntegerVectors(accessor, views, bin, "joints");
        }

        static int[][] ReadIntegerVectors(JObject accessor, JArray views, byte[] bin, string label)
        {
            int count = Count(accessor, AuthoringLimits.MaxVertices), viewId = IntProperty(accessor, "bufferView", 0, views.Count - 1, "bufferView"), viewOffset = IntOptional((JObject)views[viewId], "byteOffset"), accessorOffset = IntOptional(accessor, "byteOffset"); var view = (JObject)views[viewId]; int type = IntProperty(accessor, "componentType", 0, int.MaxValue, "componentType"), width = type == 5121 ? 1 : 2, stride = view["byteStride"] == null ? width * 4 : IntProperty(view, "byteStride", width * 4, 4096, "byteStride");
            ValidateRange(viewOffset, accessorOffset, stride, count, width * 4, IntProperty(view, "byteLength", 0, bin.Length, "byteLength"), bin.Length, label); var result = new int[count][];
            for (int i = 0; i < count; i++) { result[i] = new int[4]; for (int c = 0; c < 4; c++) { int offset = viewOffset + accessorOffset + i * stride + c * width; result[i][c] = width == 1 ? bin[offset] : BitConverter.ToUInt16(bin, offset); } } return result;
        }

        static float[][] ReadFloatVectors(JArray accessors, JArray views, byte[] bin, int id, string type, string label)
        {
            var accessor = Accessor(accessors, id, type, new[] { 5126 }); int components = type == "VEC4" ? 4 : type == "MAT4" ? 16 : 0; int count = Count(accessor, AuthoringLimits.MaxVertices); int viewId = IntProperty(accessor, "bufferView", 0, views.Count - 1, "bufferView"); var view = (JObject)views[viewId]; int viewOffset = IntOptional(view, "byteOffset"), accessorOffset = IntOptional(accessor, "byteOffset"), stride = view["byteStride"] == null ? components * 4 : IntProperty(view, "byteStride", components * 4, 4096, "byteStride"); ValidateRange(viewOffset, accessorOffset, stride, count, components * 4, IntProperty(view, "byteLength", 0, bin.Length, "byteLength"), bin.Length, label);
            var result = new float[count][]; for (int i = 0; i < count; i++) { result[i] = new float[components]; for (int c = 0; c < components; c++) { result[i][c] = BitConverter.ToSingle(bin, viewOffset + accessorOffset + i * stride + c * 4); Checks.Finite(result[i][c]); } } return result;
        }

        static void ValidateTranslationMatrix(float[] m, string label)
        {
            Checks.Require(m.Length == 16, "INVALID_IMPORT", label + " must contain 16 values.");
            int[] identity = { 0, 5, 10, 15 }; for (int i = 0; i < 16; i++) if (!identity.Contains(i) && i != 12 && i != 13 && i != 14) Checks.Require(Math.Abs(m[i]) < 1e-5, "UNSUPPORTED_FORMAT", "Only translation " + label + " is supported.");
            foreach (int i in identity) Checks.Require(Math.Abs(m[i] - 1) < 1e-5, "UNSUPPORTED_FORMAT", "Only unit-scale " + label + " is supported.");
        }

        static void ValidateRange(int viewOffset, int accessorOffset, int stride, int count, int elementBytes, int viewLength, int binLength, string label)
        { long start = (long)viewOffset + accessorOffset, end = start + (long)(count - 1) * stride + elementBytes; Checks.Require(start >= 0 && end <= binLength && end <= (long)viewOffset + viewLength, "INVALID_IMPORT", label + " accessor exceeds its bufferView."); }
        static double DistanceSquared(Vec3 a, Vec3 b) { double x = a.X - b.X, y = a.Y - b.Y, z = a.Z - b.Z; return x * x + y * y + z * z; }
        static float[] Numbers(JToken token, int count, string label) { var array = token as JArray; Checks.Require(array != null && array.Count == count, "INVALID_IMPORT", label + " must have " + count + " values."); var result = array.Select(v => { Checks.Require(v.Type == JTokenType.Float || v.Type == JTokenType.Integer, "INVALID_IMPORT", label + " contains a non-number."); float value = (float)v; Checks.Finite(value); return value; }).ToArray(); return result; }
        static JArray Array(JObject owner, string property) { var value = owner[property] as JArray; Checks.Require(value != null, "INVALID_IMPORT", "GLB property is missing: " + property); return value; }
        static int IntToken(JToken token, int minimum, int maximum, string label) { Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_IMPORT", label + " is invalid."); int value = (int)token; Checks.Require(value >= minimum && value <= maximum, "INVALID_IMPORT", label + " is out of range."); return value; }
        static int IntProperty(JObject owner, string property, int minimum, int maximum, string label) { return IntToken(owner[property], minimum, maximum, label); }
        static int IntOptional(JObject owner, string property) { return owner[property] == null ? 0 : IntProperty(owner, property, 0, int.MaxValue, property); }
        static int AccessorCount(JArray accessors, int id) { return Count(Accessor(accessors, id, "VEC3", new[] { 5126 }), AuthoringLimits.MaxVertices); }
        static int Count(JObject accessor, int maximum) { return IntProperty(accessor, "count", 1, maximum, "accessor count"); }
        static JObject Accessor(JArray accessors, int id, string type, int[] componentTypes) { Checks.Require(id >= 0 && id < accessors.Count, "INVALID_IMPORT", "GLB accessor reference is out of range."); var accessor = accessors[id] as JObject; Checks.Require(accessor != null && (string)accessor["type"] == type, "UNSUPPORTED_FORMAT", "GLB accessor type is unsupported."); Checks.Require(componentTypes.Contains(IntProperty(accessor, "componentType", 0, int.MaxValue, "componentType")), "UNSUPPORTED_FORMAT", "GLB accessor component type is unsupported."); Checks.Require(accessor["sparse"] == null, "UNSUPPORTED_FORMAT", "Sparse accessors are not supported yet."); return accessor; }
        static string StableId(string text) { using (var sha = SHA256.Create()) return new Guid(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Take(16).ToArray()).ToString("D"); }
    }
}
