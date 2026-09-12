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
        public int MeshIndex { get; }
        public int SkinIndex { get; }
        public string Format { get; }
        public MeshData Mesh { get; }
        public MorphSet Morphs { get; }
        public SkeletonDefinition Skeleton { get; }
        public SkinBinding Binding { get; }
        public ImportedBoneMap BoneMap { get; }
        public IReadOnlyDictionary<int, Vec3> SourceNodeOrigins { get; }
        public IReadOnlyList<string> Warnings { get; }
        public IReadOnlyList<GlbImportDiagnostic> Diagnostics { get; }
        public IReadOnlyList<GlbMaterialSource> Materials { get; }
        public ImportedSourceHierarchy Hierarchy { get; }
        /// <summary>World affine of the selected skinned node instance, when an instance was selected.</summary>
        public SourceAffine InstanceWorldTransform { get; }

        internal ImportedSkinnedMeshSource(string sourceHash, int meshIndex, int skinIndex, MeshData mesh, MorphSet morphs, SkeletonDefinition skeleton, SkinBinding binding, IEnumerable<string> warnings, IDictionary<int, string> nodeToBone, IDictionary<int, Vec3> nodeOrigins, ImportedSourceHierarchy hierarchy, SourceAffine instanceWorldTransform = null, IEnumerable<GlbImportDiagnostic> diagnostics = null, IEnumerable<GlbMaterialSource> materials = null)
        {
            Checks.HashText(sourceHash); Checks.Require(meshIndex >= 0 && skinIndex >= 0 && mesh != null && skeleton != null && binding != null, "INVALID_IMPORT", "Skinned GLB result is incomplete.");
            Checks.Require(binding.MeshTopologyHash == mesh.TopologyHash && binding.SkeletonHash == skeleton.ContentHash, "INVALID_IMPORT", "Skinned GLB identities are inconsistent.");
            SourceHash = sourceHash; MeshIndex = meshIndex; SkinIndex = skinIndex; Format = "glb.v2.skin.v1"; Mesh = mesh; Morphs = morphs; Skeleton = skeleton; Binding = binding;
            BoneMap = new ImportedBoneMap(sourceHash, skeleton, nodeToBone);
            Checks.Require(nodeOrigins != null && nodeOrigins.Count == nodeToBone.Count && nodeToBone.Keys.All(nodeOrigins.ContainsKey), "INVALID_IMPORT", "Source node origins must cover imported joints.");
            foreach (var origin in nodeOrigins.Values) Checks.Finite(origin);
            SourceNodeOrigins = new System.Collections.ObjectModel.ReadOnlyDictionary<int, Vec3>(new Dictionary<int, Vec3>(nodeOrigins));
            hierarchy.ValidateJointOrigins(SourceNodeOrigins); Hierarchy = hierarchy;
            InstanceWorldTransform = instanceWorldTransform;
            Warnings = Array.AsReadOnly((warnings ?? Array.Empty<string>()).ToArray());
            Diagnostics = Array.AsReadOnly((diagnostics ?? GlbImportDiagnostics.Empty).ToArray());
            Materials = Array.AsReadOnly((materials ?? Array.Empty<GlbMaterialSource>()).ToArray());
        }
    }

    /// <summary>
    /// Imports one mesh and one skin while retaining the source node TRS/matrix and inverse-bind
    /// frames in the companion source-skin package. The authored skeleton still exposes the
    /// portable head/tail representation, while deformation uses the full source affine data.
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
            var meshes = Array(document.Root, "meshes"); var skins = Array(document.Root, "skins");
            Checks.Require(meshes.Count == 1 && skins.Count == 1, "UNSUPPORTED_FORMAT", "Import one mesh and one skin at a time; select both explicitly for a multi-resource GLB.");
            return Parse(document, 0, 0);
        }

        /// <summary>Reads one mesh and one skin by source index, retaining the source hash and slot order.</summary>
        public static ImportedSkinnedMeshSource Read(byte[] bytes, int meshIndex, int skinIndex)
        {
            return Parse(GlbDocumentReader.Read(bytes), meshIndex, skinIndex);
        }

        /// <summary>Reads one mesh/skin and retains the explicitly selected node instance affine.</summary>
        public static ImportedSkinnedMeshSource Read(byte[] bytes, int meshIndex, int skinIndex, SourceAffine instanceWorldTransform)
        {
            Checks.Require(instanceWorldTransform != null, "INVALID_IMPORT", "Selected skinned node instance transform is required.");
            return Parse(GlbDocumentReader.Read(bytes), meshIndex, skinIndex, instanceWorldTransform);
        }

        static ImportedSkinnedMeshSource Parse(GlbDocument document, int meshIndex, int skinIndex, SourceAffine instanceWorldTransform = null)
        {
            var root = document.Root;
            GlbImportDiagnostics.RequireSupportedRequiredExtensions(root);
            var meshes = Array(root, "meshes"); Checks.Require(meshIndex >= 0 && meshIndex < meshes.Count, "INVALID_IMPORT", "Selected GLB mesh index is out of range.");
            var meshToken = meshes[meshIndex] as JObject; Checks.Require(meshToken != null, "INVALID_IMPORT", "GLB mesh is invalid.");
            var primitives = Array(meshToken, "primitives"); Checks.Require(primitives.Count > 0 && primitives.Count <= AuthoringLimits.MaxSubmeshes, "BUDGET_EXCEEDED", "GLB primitive count exceeds the submesh budget.");
            var skins = Array(root, "skins"); Checks.Require(skinIndex >= 0 && skinIndex < skins.Count, "INVALID_IMPORT", "Selected GLB skin index is out of range.");
            var skin = skins[skinIndex] as JObject; Checks.Require(skin != null, "INVALID_IMPORT", "GLB skin is invalid.");
            var nodes = Array(root, "nodes");
            var joints = Array(skin, "joints"); Checks.Require(joints.Count > 0 && joints.Count <= SkeletonDefinition.MaxBones, "BUDGET_EXCEEDED", "GLB joint count exceeds the skeleton budget.");
            var jointNodes = joints.Select(token => IntToken(token, 0, nodes.Count - 1, "skin joint")).ToArray();
            Checks.Require(jointNodes.Distinct().Count() == jointNodes.Length, "INVALID_SKELETON", "GLB skin repeats a joint node.");
            var parentByNode = ReadParents(nodes);
            var sourceNodes = GlbNodeTransformReader.Read(nodes, document.SourceHash);
            var hierarchy = sourceNodes.Hierarchy;
            var world = sourceNodes.World.Select(frame => frame.TransformPoint(new Vec3())).ToArray();
            var jointToBone = BuildSkeleton(document.SourceHash, skin, nodes, joints, jointNodes, parentByNode, world, document);

            // Reuse the static mesh adapter after removing only skin attributes from a cloned JSON tree.
            // This keeps geometry/morph parsing in one module and preserves the original source hash.
            var staticRoot = (JObject)root.DeepClone(); staticRoot.Remove("skins");
            var staticMeshes = (JArray)staticRoot["meshes"]; var staticMesh = (JObject)staticMeshes[meshIndex];
            foreach (var token in (JArray)staticMesh["primitives"])
            {
                var primitive = token as JObject; Checks.Require(primitive != null, "INVALID_IMPORT", "GLB primitive is invalid.");
                var attributes = primitive["attributes"] as JObject; Checks.Require(attributes != null, "INVALID_IMPORT", "GLB primitive attributes are required.");
                foreach (var property in attributes.Properties().Where(p => p.Name.StartsWith("JOINTS_", StringComparison.Ordinal) || p.Name.StartsWith("WEIGHTS_", StringComparison.Ordinal)).ToArray()) property.Remove();
            }
            var baseSource = GlbImporter.ReadDocument(new GlbDocument(staticRoot, document.Bin, document.SourceHash), meshIndex);
            var rawWeights = new List<SkinBinding.VertexWeightInput>(); int vertexOffset = 0;
            for (int p = 0; p < primitives.Count; p++)
            {
                var primitive = (JObject)primitives[p]; var attributes = primitive["attributes"] as JObject; Checks.Require(attributes != null, "INVALID_IMPORT", "GLB primitive attributes are required.");
                int positionCount = AccessorCount(Array(root, "accessors"), IntProperty(attributes, "POSITION", 0, int.MaxValue, "POSITION"));
                var jointIds = new SortedDictionary<int, int>(); var weightIds = new SortedDictionary<int, int>();
                foreach (var property in attributes.Properties())
                {
                    if (TrySet(property.Name, "JOINTS_", out int jointSet)) Checks.Require(jointIds.TryAdd(jointSet, IntProperty(attributes, property.Name, 0, int.MaxValue, property.Name)), "INVALID_IMPORT", property.Name + " is repeated.");
                    if (TrySet(property.Name, "WEIGHTS_", out int weightSet)) Checks.Require(weightIds.TryAdd(weightSet, IntProperty(attributes, property.Name, 0, int.MaxValue, property.Name)), "INVALID_IMPORT", property.Name + " is repeated.");
                }
                Checks.Require(jointIds.Count > 0 && jointIds.Count == weightIds.Count && jointIds.Keys.SequenceEqual(weightIds.Keys), "UNSUPPORTED_FORMAT", "JOINTS_n and WEIGHTS_n sets must be paired from zero.");
                Checks.Require(jointIds.Keys.First() == 0 && jointIds.Keys.SequenceEqual(Enumerable.Range(0, jointIds.Count)), "UNSUPPORTED_FORMAT", "Skin attribute sets must be contiguous from zero.");
                var sets = jointIds.Keys.Select(set => Tuple.Create(
                    ReadJointVectors(Array(root, "accessors"), Array(root, "bufferViews"), document.Bin, jointIds[set]),
                    ReadWeightVectors(Array(root, "accessors"), Array(root, "bufferViews"), document.Bin, weightIds[set], "weights_" + set.ToString(System.Globalization.CultureInfo.InvariantCulture)))).ToArray();
                foreach (var set in sets) Checks.Require(set.Item1.Length == positionCount && set.Item2.Length == positionCount, "INVALID_IMPORT", "Skin attribute count differs from POSITION.");
                for (int v = 0; v < positionCount; v++)
                    foreach (var set in sets)
                        for (int i = 0; i < 4; i++)
                        {
                            float weight = set.Item2[v][i]; Checks.Finite(weight); Checks.Require(weight >= 0f && weight <= 1f, "INVALID_WEIGHT", "WEIGHTS component must be between zero and one.");
                            if (weight > 0) { Checks.Require(set.Item1[v][i] < jointNodes.Length, "INVALID_IMPORT", "Skin joint index is outside the skin."); rawWeights.Add(new SkinBinding.VertexWeightInput(vertexOffset + v, jointToBone.BoneIds[set.Item1[v][i]], weight)); }
                        }
                vertexOffset += positionCount;
            }
            Checks.Require(vertexOffset == baseSource.Mesh.VertexCount, "INVALID_IMPORT", "Skin vertex count differs from imported mesh.");
            var binding = SkinBinding.Create(baseSource.Mesh, jointToBone.Skeleton, rawWeights);
            var diagnostics = GlbImportDiagnostics.ForMesh(root, meshToken);
            var warnings = new List<string>(baseSource.Warnings.Where(item => !item.StartsWith("MATERIALS_NOT_RETAINED:", StringComparison.Ordinal) && !item.StartsWith("ANIMATIONS_NOT_RETAINED:", StringComparison.Ordinal) && !item.StartsWith("REQUIRED_EXTENSIONS_NOT_RETAINED:", StringComparison.Ordinal) && !item.StartsWith("EXTENSIONS_PARTIAL:", StringComparison.Ordinal)));
            warnings.AddRange(GlbImportDiagnostics.WarningText(diagnostics));
            warnings.Add("GLB source node TRS/matrix and inverse-bind affine frames are retained in the source-skin package; the authored skeleton publishes portable head/tail data.");
            if (instanceWorldTransform != null) warnings.Add("Selected skinned node instance world transform is retained for display and standard GLB output.");
            return new ImportedSkinnedMeshSource(document.SourceHash, meshIndex, skinIndex, baseSource.Mesh, baseSource.Morphs, jointToBone.Skeleton, binding, warnings, jointToBone.NodeToBone, jointNodes.ToDictionary(node => node, node => world[node]), hierarchy, instanceWorldTransform, diagnostics, baseSource.Materials);
        }

        sealed class SkeletonResult
        {
            public SkeletonDefinition Skeleton; public string[] BoneIds; public Dictionary<int, string> NodeToBone;
        }

        static SkeletonResult BuildSkeleton(string sourceHash, JObject skin, JArray nodes, JArray joints, int[] jointNodes, Dictionary<int, int> parentByNode, Vec3[] world, GlbDocument document)
        {
            var ids = jointNodes.ToDictionary(node => node, node => StableId(sourceHash + ":bone:" + node.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            Vec3[] heads = jointNodes.Select(node => world[node]).ToArray();
            var inverseToken = skin["inverseBindMatrices"]; if (inverseToken != null)
            {
                var matrices = GlbMatrixAccessorReader.Read(document, GlbMatrixAccessorReader.Integer(inverseToken, 0, int.MaxValue, "inverseBindMatrices"));
                Checks.Require(matrices.Length == jointNodes.Length, "INVALID_IMPORT", "Inverse bind matrix count differs from joints.");
                for (int i = 0; i < matrices.Length; i++) heads[i] = matrices[i].Inverse().TransformPoint(new Vec3());
            }
            var jointParents = ImportedJointHierarchy.ResolveParents(jointNodes, parentByNode);
            var bones = new List<BoneDefinition>(jointNodes.Length);
            for (int i = 0; i < jointNodes.Length; i++)
            {
                int node = jointNodes[i]; string parent = jointParents.TryGetValue(node, out var parentNode) ? ids[parentNode] : "";
                Vec3 tail = heads[i] + new Vec3(0, .05f, 0);
                var nodeToken = (JObject)nodes[node];
                // A helper node does not split a skin hierarchy; heads already include its translation.
                foreach (var child in jointNodes.OrderBy(value => value))
                    if (jointParents.TryGetValue(child, out var ancestor) && ancestor == node)
                    {
                        var candidate = heads[System.Array.IndexOf(jointNodes, child)];
                        if (DistanceSquared(heads[i], candidate) > 1e-12) { tail = candidate; break; }
                    }
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

        static int[][] ReadJointVectors(JArray accessors, JArray views, byte[] bin, int id)
        {
            var accessor = Accessor(accessors, id, "VEC4", new[] { 5121, 5123 }); Checks.Require(accessor["normalized"] == null || (bool)accessor["normalized"] == false, "UNSUPPORTED_FORMAT", "Normalized JOINTS are not supported."); return ReadIntegerVectors(accessor, views, bin, "joints");
        }

        static bool TrySet(string name, string prefix, out int index)
        {
            index = -1; if (!name.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string suffix = name.Substring(prefix.Length); Checks.Require(suffix.Length > 0 && suffix.All(char.IsDigit), "INVALID_IMPORT", name + " index is invalid.");
            long parsed = 0; foreach (char digit in suffix) { parsed = parsed * 10 + (digit - '0'); Checks.Require(parsed <= int.MaxValue, "INVALID_IMPORT", name + " index is out of range."); }
            index = (int)parsed; return true;
        }

        static int[][] ReadIntegerVectors(JObject accessor, JArray views, byte[] bin, string label)
        {
            int count = Count(accessor, AuthoringLimits.MaxVertices), viewId = IntProperty(accessor, "bufferView", 0, views.Count - 1, "bufferView"), viewOffset = IntOptional((JObject)views[viewId], "byteOffset"), accessorOffset = IntOptional(accessor, "byteOffset"); var view = (JObject)views[viewId]; int type = IntProperty(accessor, "componentType", 0, int.MaxValue, "componentType"), width = type == 5121 ? 1 : 2, stride = view["byteStride"] == null ? width * 4 : IntProperty(view, "byteStride", width * 4, 4096, "byteStride");
            ValidateRange(viewOffset, accessorOffset, stride, count, width * 4, IntProperty(view, "byteLength", 0, bin.Length, "byteLength"), bin.Length, label); var result = new int[count][];
            for (int i = 0; i < count; i++) { result[i] = new int[4]; for (int c = 0; c < 4; c++) { int offset = viewOffset + accessorOffset + i * stride + c * width; result[i][c] = width == 1 ? bin[offset] : BitConverter.ToUInt16(bin, offset); } } return result;
        }

        static float[][] ReadWeightVectors(JArray accessors, JArray views, byte[] bin, int id, string label)
        {
            var accessor = Accessor(accessors, id, "VEC4", new[] { 5121, 5123, 5126 });
            int type = IntProperty(accessor, "componentType", 0, int.MaxValue, "componentType");
            var normalizedToken = accessor["normalized"];
            Checks.Require(normalizedToken == null || normalizedToken.Type == Newtonsoft.Json.Linq.JTokenType.Boolean, "INVALID_IMPORT", label + " normalized must be a boolean.");
            bool normalized = normalizedToken != null && (bool)normalizedToken;
            Checks.Require(type == 5126 ? !normalized : normalized, "UNSUPPORTED_FORMAT", label + " integer weights must be normalized (float weights must not be normalized).");
            int width = type == 5121 ? 1 : type == 5123 ? 2 : 4;
            int count = Count(accessor, AuthoringLimits.MaxVertices), viewId = IntProperty(accessor, "bufferView", 0, views.Count - 1, "bufferView");
            var view = (JObject)views[viewId]; int viewOffset = IntOptional(view, "byteOffset"), accessorOffset = IntOptional(accessor, "byteOffset");
            int stride = view["byteStride"] == null ? width * 4 : IntProperty(view, "byteStride", width * 4, 4096, "byteStride");
            ValidateRange(viewOffset, accessorOffset, stride, count, width * 4, IntProperty(view, "byteLength", 0, bin.Length, "byteLength"), bin.Length, label);
            var result = new float[count][];
            for (int i = 0; i < count; i++)
            {
                result[i] = new float[4];
                for (int c = 0; c < 4; c++)
                {
                    int offset = viewOffset + accessorOffset + i * stride + c * width;
                    result[i][c] = type == 5126 ? BitConverter.ToSingle(bin, offset) : (type == 5121 ? bin[offset] / 255f : BitConverter.ToUInt16(bin, offset) / 65535f);
                    Checks.Finite(result[i][c]);
                }
            }
            return result;
        }

        static void ValidateRange(int viewOffset, int accessorOffset, int stride, int count, int elementBytes, int viewLength, int binLength, string label)
        { long start = (long)viewOffset + accessorOffset, end = start + (long)(count - 1) * stride + elementBytes; Checks.Require(start >= 0 && end <= binLength && end <= (long)viewOffset + viewLength, "INVALID_IMPORT", label + " accessor exceeds its bufferView."); }
        static double DistanceSquared(Vec3 a, Vec3 b) { double x = a.X - b.X, y = a.Y - b.Y, z = a.Z - b.Z; return x * x + y * y + z * z; }
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
