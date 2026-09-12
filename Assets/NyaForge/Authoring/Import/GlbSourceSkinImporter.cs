using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Complete one-mesh GLB source skin candidate: source frames, mesh and all JOINTS_n/WEIGHTS_n sets.</summary>
    public sealed class GlbSourceSkinImportResult
    {
        public string SourceHash { get; }
        public int MeshIndex { get; }
        public int SkinIndex { get; }
        public ImportedMeshSource MeshSource { get; }
        public SourceSkin Skin { get; }
        public SourceSkinBinding Binding { get; }
        internal GlbSourceSkinImportResult(string sourceHash, ImportedMeshSource meshSource, SourceSkin skin, SourceSkinBinding binding)
        {
            Checks.HashText(sourceHash); Checks.Require(meshSource != null && skin != null && binding != null, "INVALID_IMPORT", "Source skin result is incomplete.");
            Checks.Require(meshSource.SourceHash == sourceHash && skin.Nodes.SourceHash == sourceHash && binding.SourceHash == sourceHash, "INVALID_IMPORT", "Source skin result identities differ.");
            SourceHash = sourceHash; MeshIndex = meshSource.MeshIndex; SkinIndex = skin.SkinIndex; MeshSource = meshSource; Skin = skin; Binding = binding;
        }
    }

    /// <summary>Reads one GLB mesh and source skin without publishing a native graph.</summary>
    public static class GlbSourceSkinImporter
    {
        public static GlbSourceSkinImportResult Read(byte[] bytes, int skinIndex = 0)
        {
            var document = GlbDocumentReader.Read(bytes); var meshes = Array(document.Root, "meshes");
            Checks.Require(meshes.Count == 1 && meshes[0] is JObject, "UNSUPPORTED_FORMAT", "Source skin candidate imports one mesh at a time; select a mesh explicitly for a multi-mesh GLB.");
            return Read(document, 0, skinIndex);
        }

        /// <summary>Reads one mesh and one skin by their source indices without collapsing a multi-mesh GLB.</summary>
        public static GlbSourceSkinImportResult Read(byte[] bytes, int meshIndex, int skinIndex)
        {
            return Read(GlbDocumentReader.Read(bytes), meshIndex, skinIndex);
        }

        static GlbSourceSkinImportResult Read(GlbDocument document, int meshIndex, int skinIndex)
        {
            var root = document.Root; var meshes = Array(root, "meshes");
            Checks.Require(meshIndex >= 0 && meshIndex < meshes.Count && meshes[meshIndex] is JObject, "INVALID_IMPORT", "Source skin mesh index is out of range.");
            var buffers = Array(root, "buffers"); Checks.Require(buffers.Count == 1 && buffers[0] is JObject, "UNSUPPORTED_FORMAT", "Source skin candidate requires one embedded buffer.");
            var buffer = (JObject)buffers[0]; Checks.Require(buffer["uri"] == null && buffer["extensions"] == null, "UNSUPPORTED_FORMAT", "Source skin candidate requires the embedded GLB buffer.");
            int bufferLength = Integer(buffer["byteLength"], 1, AuthoringLimits.MaxBlobBytes, "buffer byteLength");
            Checks.Require(bufferLength <= document.Bin.Length && document.Bin.Length - bufferLength <= 3, "INVALID_IMPORT", "GLB buffer length differs from BIN payload.");
            var skins = Array(root, "skins"); Checks.Require(skinIndex >= 0 && skinIndex < skins.Count, "INVALID_IMPORT", "Source skin index is missing.");
            var skin = GlbSourceSkinReader.Read(document, GlbNodeTransformReader.Read(root["nodes"] as JArray, document.SourceHash), skinIndex);
            var staticRoot = (JObject)root.DeepClone(); staticRoot.Remove("skins");
            var staticMesh = (JObject)((JArray)staticRoot["meshes"])[meshIndex];
            foreach (var token in (JArray)staticMesh["primitives"])
            {
                var primitive = token as JObject; Checks.Require(primitive != null, "INVALID_IMPORT", "Source primitive is invalid.");
                var attributes = primitive["attributes"] as JObject; Checks.Require(attributes != null, "INVALID_IMPORT", "Source primitive attributes are required.");
                foreach (var property in attributes.Properties().Where(p => p.Name.StartsWith("JOINTS_", StringComparison.Ordinal) || p.Name.StartsWith("WEIGHTS_", StringComparison.Ordinal)).ToArray()) property.Remove();
            }
            var meshSource = GlbImporter.ReadDocument(new GlbDocument(staticRoot, document.Bin, document.SourceHash), meshIndex);
            var binding = ReadWeights(document, skin, meshSource.Mesh, bufferLength, meshIndex);
            return new GlbSourceSkinImportResult(document.SourceHash, meshSource, skin, binding);
        }

        static SourceSkinBinding ReadWeights(GlbDocument document, SourceSkin skin, MeshData mesh, int bufferLength, int meshIndex)
        {
            var root = document.Root; var meshes = Array(root, "meshes"); Checks.Require(meshIndex >= 0 && meshIndex < meshes.Count, "INVALID_IMPORT", "Source skin mesh index is out of range."); var meshToken = (JObject)meshes[meshIndex]; var primitives = Array(meshToken, "primitives");
            var accessors = Array(root, "accessors"); var views = Array(root, "bufferViews"); var result = new List<SourceSkinWeight>(); int vertexOffset = 0;
            for (int p = 0; p < primitives.Count; p++)
            {
                var primitive = primitives[p] as JObject; Checks.Require(primitive != null, "INVALID_IMPORT", "Source primitive is invalid.");
                var attributes = primitive["attributes"] as JObject; Checks.Require(attributes != null, "INVALID_IMPORT", "Source primitive attributes are required.");
                int positionId = Integer(attributes["POSITION"], 0, accessors.Count - 1, "POSITION accessor");
                int count = AccessorCount(accessors[positionId] as JObject, positionId);
                var sets = new SortedDictionary<int, Tuple<int[][], float[][]>>(); var jointIds = new SortedDictionary<int, int>(); var weightIds = new SortedDictionary<int, int>();
                foreach (var property in attributes.Properties())
                {
                    if (TrySet(property.Name, "JOINTS_", out int jointSet)) Checks.Require(jointIds.TryAdd(jointSet, Integer(property.Value, 0, accessors.Count - 1, property.Name)), "INVALID_IMPORT", property.Name + " is repeated.");
                    if (TrySet(property.Name, "WEIGHTS_", out int weightSet)) Checks.Require(weightIds.TryAdd(weightSet, Integer(property.Value, 0, accessors.Count - 1, property.Name)), "INVALID_IMPORT", property.Name + " is repeated.");
                }
                Checks.Require(jointIds.Count > 0 && jointIds.Count == weightIds.Count && jointIds.Keys.SequenceEqual(weightIds.Keys), "UNSUPPORTED_FORMAT", "JOINTS_n and WEIGHTS_n sets must be paired from zero.");
                Checks.Require(jointIds.Keys.First() == 0 && jointIds.Keys.SequenceEqual(Enumerable.Range(0, jointIds.Count)), "UNSUPPORTED_FORMAT", "Skin attribute sets must be contiguous from zero.");
                foreach (int set in jointIds.Keys)
                {
                    var joints = ReadJoints(accessors, views, document.Bin, bufferLength, jointIds[set], count, "JOINTS_" + set);
                    var weights = ReadWeights(accessors, views, document.Bin, bufferLength, weightIds[set], count, "WEIGHTS_" + set);
                    sets.Add(set, Tuple.Create(joints, weights));
                }
                for (int vertex = 0; vertex < count; vertex++)
                    foreach (var set in sets.Values)
                        for (int component = 0; component < 4; component++)
                            if (set.Item2[vertex][component] > 0) result.Add(new SourceSkinWeight(vertexOffset + vertex, set.Item1[vertex][component], set.Item2[vertex][component]));
                vertexOffset += count;
            }
            Checks.Require(vertexOffset == mesh.VertexCount, "INVALID_IMPORT", "Skin attributes do not cover the imported mesh vertices.");
            return SourceSkinBinding.Create(mesh, skin, result);
        }

        static bool TrySet(string name, string prefix, out int index)
        {
            index = -1; if (!name.StartsWith(prefix, StringComparison.Ordinal)) return false;
            string suffix = name.Substring(prefix.Length); Checks.Require(suffix.Length > 0 && suffix.All(char.IsDigit), "INVALID_IMPORT", name + " index is invalid.");
            long parsed = 0; foreach (char digit in suffix) { parsed = parsed * 10 + (digit - '0'); Checks.Require(parsed <= int.MaxValue, "INVALID_IMPORT", name + " index is out of range."); }
            index = (int)parsed; return true;
        }

        static int AccessorCount(JObject accessor, int id)
        { Checks.Require(accessor != null && (string)accessor["type"] == "VEC3", "UNSUPPORTED_FORMAT", "POSITION accessor is invalid."); return Integer(accessor["count"], 1, AuthoringLimits.MaxVertices, "POSITION count"); }

        static int[][] ReadJoints(JArray accessors, JArray views, byte[] bin, int bufferLength, int id, int expected, string label)
        {
            var accessor = Accessor(accessors, id, "VEC4", new[] {5121,5123}, label); Checks.Require(accessor["normalized"] == null || (accessor["normalized"].Type == JTokenType.Boolean && !(bool)accessor["normalized"]), "INVALID_IMPORT", label + " cannot be normalized.");
            int componentType = Integer(accessor["componentType"], 5121, 5123, label + " componentType"); Checks.Require(componentType == 5121 || componentType == 5123, "UNSUPPORTED_FORMAT", label + " componentType is unsupported."); int width = componentType == 5121 ? 1 : 2; var data = ReadRaw(accessor, views, bin, bufferLength, width * 4, expected, label);
            return data.Select(row => Enumerable.Range(0,4).Select(i => componentType == 5121 ? row[i] : row[i] | row[i+1] << 8).ToArray()).ToArray();
        }

        static float[][] ReadWeights(JArray accessors, JArray views, byte[] bin, int bufferLength, int id, int expected, string label)
        {
            var accessor = Accessor(accessors, id, "VEC4", new[] {5126}, label); var data = ReadRaw(accessor, views, bin, bufferLength, 16, expected, label);
            var result = new float[expected][]; for (int row = 0; row < expected; row++) { result[row] = new float[4]; for (int i = 0; i < 4; i++) result[row][i] = BitConverter.ToSingle(data[row], i * 4); }
            return result;
        }

        static byte[][] ReadRaw(JObject accessor, JArray views, byte[] bin, int bufferLength, int elementBytes, int expected, string label)
        {
            Checks.Require(Integer(accessor["count"], 1, AuthoringLimits.MaxVertices, label + " count") == expected, "INVALID_IMPORT", label + " count differs from POSITION.");
            Checks.Require(accessor["sparse"] == null, "UNSUPPORTED_FORMAT", label + " sparse accessor is not supported yet.");
            Checks.Require(accessor["extensions"] == null, "UNSUPPORTED_FORMAT", label + " accessor extensions require a dedicated adapter.");
            var viewId = Integer(accessor["bufferView"], 0, views.Count - 1, label + " bufferView"); var view = views[viewId] as JObject; Checks.Require(view != null, "INVALID_IMPORT", label + " bufferView is invalid.");
            Checks.Require(view["extensions"] == null, "UNSUPPORTED_FORMAT", label + " bufferView extensions require a dedicated adapter.");
            Checks.Require(Integer(view["buffer"], 0, 0, label + " buffer") == 0, "INVALID_IMPORT", label + " buffer reference is invalid.");
            int viewOffset = OptionalInteger(view["byteOffset"], label + " view offset"), accessorOffset = OptionalInteger(accessor["byteOffset"], label + " accessor offset");
            int stride = view["byteStride"] == null ? elementBytes : Integer(view["byteStride"], elementBytes, 252, label + " stride"); int viewLength = Integer(view["byteLength"], 1, AuthoringLimits.MaxBlobBytes, label + " view length");
            int component = Integer(accessor["componentType"], 0, int.MaxValue, label + " componentType"); int componentWidth = (component == 5121 || component == 5126) ? 1 : 2;
            Checks.Require(stride % 4 == 0 && accessorOffset % componentWidth == 0 && ((long)viewOffset + accessorOffset) % 4 == 0 && (long)viewOffset + viewLength <= bufferLength &&
                (long)accessorOffset + (long)(expected - 1) * stride + elementBytes <= viewLength, "INVALID_IMPORT", label + " range or alignment is invalid.");
            var result = new byte[expected][]; for (int row = 0; row < expected; row++) { result[row] = new byte[elementBytes]; Buffer.BlockCopy(bin, checked(viewOffset + accessorOffset + row * stride), result[row], 0, elementBytes); }
            return result;
        }

        static JObject Accessor(JArray accessors, int id, string type, int[] components, string label)
        { Checks.Require(id >= 0 && id < accessors.Count, "INVALID_IMPORT", label + " accessor is out of range."); var value = accessors[id] as JObject; Checks.Require(value != null && (string)value["type"] == type, "UNSUPPORTED_FORMAT", label + " accessor type is unsupported."); int component = Integer(value["componentType"], 0, int.MaxValue, label + " componentType"); Checks.Require(components.Contains(component), "UNSUPPORTED_FORMAT", label + " componentType is unsupported."); return value; }
        static int Integer(JToken token, int min, int max, string label)
        { Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_IMPORT", label + " must be an integer."); double value = (double)token; Checks.Require(value >= min && value <= max, "INVALID_IMPORT", label + " is out of range."); return (int)value; }
        static int OptionalInteger(JToken token, string label) => token == null ? 0 : Integer(token, 0, AuthoringLimits.MaxBlobBytes, label);
        static JArray Array(JObject owner, string name) { var value = owner[name] as JArray; Checks.Require(value != null, "INVALID_IMPORT", "GLB property is missing: " + name); return value; }
    }
}
