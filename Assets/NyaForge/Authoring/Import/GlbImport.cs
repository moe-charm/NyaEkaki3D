using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>CPU-readable result of the deliberately small runtime GLB boundary.</summary>
    public sealed class ImportedMeshSource
    {
        public string SourceHash { get; }
        public int MeshIndex { get; }
        public string Format { get; }
        public MeshData Mesh { get; }
        public MorphSet Morphs { get; }
        public IReadOnlyList<string> Warnings { get; }
        internal ImportedMeshSource(string sourceHash, int meshIndex, MeshData mesh, MorphSet morphs, IEnumerable<string> warnings)
        {
            Checks.HashText(sourceHash); Checks.Require(meshIndex >= 0 && mesh != null, "INVALID_IMPORT", "Imported mesh is required.");
            SourceHash = sourceHash; MeshIndex = meshIndex; Format = "glb.v2"; Mesh = mesh; Morphs = morphs;
            Warnings = Array.AsReadOnly((warnings ?? Array.Empty<string>()).ToArray());
        }
    }

    /// <summary>
    /// Imports one static GLB mesh and its POSITION morph targets. Multiple triangle
    /// primitives are combined into bounded submeshes while preserving primitive-local
    /// vertex order. Scene hierarchy, materials and skin bindings remain out of scope.
    /// </summary>
    public static class GlbImporter
    {
        public static ImportedMeshSource Read(byte[] bytes)
        {
            var document = GlbDocumentReader.Read(bytes);
            var meshes = Array(document.Root, "meshes");
            Checks.Require(meshes.Count == 1, "UNSUPPORTED_FORMAT", "Import one mesh at a time; select a mesh explicitly for a multi-mesh GLB.");
            return ReadDocument(document, 0);
        }

        /// <summary>Reads one source mesh by its GLB index without changing the source hash.</summary>
        public static ImportedMeshSource Read(byte[] bytes, int meshIndex)
        {
            return ReadDocument(GlbDocumentReader.Read(bytes), meshIndex);
        }

        internal static ImportedMeshSource ReadDocument(GlbDocument document) { return ReadDocument(document, 0); }

        internal static ImportedMeshSource ReadDocument(GlbDocument document, int meshIndex)
        {
            Checks.Require(document != null, "INVALID_IMPORT", "GLB document is required.");
            return Parse(document.Root, document.Bin, document.SourceHash, meshIndex);
        }

        static ImportedMeshSource Parse(JObject root, byte[] bin, string sourceHash, int meshIndex)
        {
            Checks.Require((string)root["asset"]?["version"] == "2.0", "UNSUPPORTED_FORMAT", "GLB asset version must be 2.0.");
            var buffers = Array(root, "buffers"); Checks.Require(buffers.Count == 1, "UNSUPPORTED_FORMAT", "Only one GLB buffer is supported.");
            int byteLength = Int(buffers[0], "byteLength", 0, bin.Length); Checks.Require(byteLength <= bin.Length, "INVALID_IMPORT", "GLB buffer exceeds its BIN chunk.");
            var views = Array(root, "bufferViews"); var accessors = Array(root, "accessors"); var meshes = Array(root, "meshes");
            Checks.Require(meshIndex >= 0 && meshIndex < meshes.Count, "INVALID_IMPORT", "Selected GLB mesh index is out of range.");
            var meshToken = meshes[meshIndex] as JObject; Checks.Require(meshToken != null, "INVALID_IMPORT", "GLB mesh is invalid.");
            Checks.Require(root["skins"] == null || root["skins"] is JArray, "INVALID_IMPORT", "GLB skins property is invalid.");
            Checks.Require(root["skins"] == null || ((JArray)root["skins"]).Count == 0, "UNSUPPORTED_FORMAT", "Skin bindings are not imported yet.");
            var primitives = Array(meshToken, "primitives"); Checks.Require(primitives.Count > 0 && primitives.Count <= AuthoringLimits.MaxSubmeshes, "BUDGET_EXCEEDED", "GLB primitive count exceeds the submesh budget.");
            var parts = primitives.Select(token => { var primitive = token as JObject; Checks.Require(primitive != null, "INVALID_IMPORT", "GLB primitive is invalid."); return ReadPrimitive(primitive, accessors, views, bin); }).ToArray();
            bool hasNormals = AttributePresence(parts, p => p.Normals.Length > 0, "NORMAL");
            bool hasTangents = AttributePresence(parts, p => p.Tangents.Length > 0, "TANGENT");
            bool hasUv0 = AttributePresence(parts, p => p.Uv0.Length > 0, "TEXCOORD_0");
            int vertexCount = parts.Sum(p => p.Positions.Length); Checks.Require(vertexCount <= AuthoringLimits.MaxVertices, "BUDGET_EXCEEDED", "GLB vertex count exceeds the authoring budget.");
            int indexCount = parts.Sum(p => p.Indices.Length); Checks.Require(indexCount <= AuthoringLimits.MaxIndices, "BUDGET_EXCEEDED", "GLB index count exceeds the authoring budget.");
            var positions = parts.SelectMany(p => p.Positions).ToArray();
            var normals = hasNormals ? parts.SelectMany(p => p.Normals).ToArray() : System.Array.Empty<Vec3>();
            var tangents = hasTangents ? parts.SelectMany(p => p.Tangents).ToArray() : System.Array.Empty<Vec4>();
            var uv0 = hasUv0 ? parts.SelectMany(p => p.Uv0).ToArray() : System.Array.Empty<Vec2>();
            var submeshes = new int[parts.Length][]; int vertexOffset = 0;
            for (int i = 0; i < parts.Length; i++) { submeshes[i] = parts[i].Indices.Select(index => checked(index + vertexOffset)).ToArray(); vertexOffset += parts[i].Positions.Length; }
            var mesh = new MeshData(positions, normals, tangents, uv0, submeshes);
            MorphSet morphs = ParseMorphs(meshToken, parts, mesh, sourceHash, meshIndex);
            var warnings = new List<string> { "Imported as " + parts.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) + " static triangle primitive(s); original glTF scene hierarchy, materials and skin bindings are not retained." };
            if (morphs != null) warnings.Add("POSITION morph targets were retained; normal/tangent morph deltas are not imported.");
            return new ImportedMeshSource(sourceHash, meshIndex, mesh, morphs, warnings);
        }

        sealed class PrimitiveData
        {
            public Vec3[] Positions; public Vec3[] Normals; public Vec4[] Tangents; public Vec2[] Uv0; public int[] Indices; public Vec3[][] MorphDeltas;
        }

        static PrimitiveData ReadPrimitive(JObject primitive, JArray accessors, JArray views, byte[] bin)
        {
            Checks.Require(primitive != null, "INVALID_IMPORT", "GLB primitive is invalid.");
            Checks.Require(primitive["mode"] == null || Int(primitive, "mode", 4, 4) == 4, "UNSUPPORTED_FORMAT", "Only triangle primitives are supported.");
            var attributes = (JObject)primitive["attributes"]; Checks.Require(attributes != null, "INVALID_IMPORT", "GLB primitive attributes are required.");
            Checks.Require(attributes["JOINTS_0"] == null && attributes["WEIGHTS_0"] == null, "UNSUPPORTED_FORMAT", "Skin attributes are not imported yet.");
            int positionAccessor = AccessorId(attributes, "POSITION"); var positions = Vec3Accessor(accessors, views, bin, positionAccessor, "position");
            var normals = OptionalVec3(attributes, "NORMAL", accessors, views, bin, positions.Length, "normal");
            var tangents = OptionalVec4(attributes, "TANGENT", accessors, views, bin, positions.Length, "tangent");
            var uv0 = OptionalVec2(attributes, "TEXCOORD_0", accessors, views, bin, positions.Length, "texcoord_0");
            int[] indices = primitive["indices"] == null ? Enumerable.Range(0, positions.Length).ToArray() : IndexAccessor(accessors, views, bin, Int(primitive, "indices", 0, accessors.Count - 1));
            Checks.Require(indices.Length > 0 && indices.Length % 3 == 0 && indices.All(index => index >= 0 && index < positions.Length), "INVALID_IMPORT", "Triangle indices are invalid.");
            var targetTokens = primitive["targets"] as JArray;
            var morphs = new Vec3[targetTokens == null ? 0 : targetTokens.Count][];
            for (int i = 0; i < morphs.Length; i++)
            {
                var target = targetTokens[i] as JObject; Checks.Require(target != null && target["POSITION"] != null, "UNSUPPORTED_FORMAT", "Every morph target needs a POSITION accessor.");
                morphs[i] = Vec3Accessor(accessors, views, bin, Int(target, "POSITION", 0, accessors.Count - 1), "morph position");
                Checks.Require(morphs[i].Length == positions.Length, "INVALID_IMPORT", "Morph POSITION count must match the primitive base mesh.");
            }
            return new PrimitiveData { Positions = positions, Normals = normals, Tangents = tangents, Uv0 = uv0, Indices = indices, MorphDeltas = morphs };
        }

        static bool AttributePresence(PrimitiveData[] parts, Func<PrimitiveData, bool> selector, string name)
        {
            bool present = selector(parts[0]); Checks.Require(parts.All(p => selector(p) == present), "UNSUPPORTED_FORMAT", "All primitives must use the same " + name + " attribute layout."); return present;
        }

        static MorphSet ParseMorphs(JToken meshToken, PrimitiveData[] parts, MeshData mesh, string sourceHash, int meshIndex)
        {
            int morphCount = parts[0].MorphDeltas.Length; Checks.Require(parts.All(p => p.MorphDeltas.Length == morphCount), "UNSUPPORTED_FORMAT", "All primitives must use the same morph target layout.");
            if (morphCount == 0) return null;
            var result = new List<MorphTarget>();
            var extras = meshToken["extras"] as JObject; var names = extras?["targetNames"] as JArray;
            int vertexOffset = 0;
            for (int i = 0; i < morphCount; i++)
            {
                var entries = new List<MorphDelta>(); vertexOffset = 0;
                foreach (var part in parts) { foreach (var delta in part.MorphDeltas[i].Select((delta, index) => new MorphDelta(index + vertexOffset, delta))) if (delta.Delta.X != 0 || delta.Delta.Y != 0 || delta.Delta.Z != 0) entries.Add(delta); vertexOffset += part.Positions.Length; }
                string name = "Morph " + i.ToString(System.Globalization.CultureInfo.InvariantCulture);
                if (names != null && i < names.Count && names[i].Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)names[i])) name = (string)names[i];
                result.Add(MorphTarget.Create(mesh, MorphTargetId(sourceHash, meshIndex, i), name, entries));
            }
            return MorphSet.Create(mesh, result);
        }

        internal static string MorphTargetId(string sourceHash, int index)
        {
            return MorphTargetId(sourceHash, 0, index);
        }

        internal static string MorphTargetId(string sourceHash, int meshIndex, int index)
        {
            Checks.HashText(sourceHash); Checks.Require(meshIndex >= 0 && index >= 0, "INVALID_MORPH", "Morph target identity must be non-negative.");
            return StableId(sourceHash + ":mesh:" + meshIndex.ToString(System.Globalization.CultureInfo.InvariantCulture) + ":morph:" + index.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        static string StableId(string text)
        {
            using (var sha = SHA256.Create()) return new Guid(sha.ComputeHash(Encoding.UTF8.GetBytes(text)).Take(16).ToArray()).ToString("D");
        }

        static JArray Array(JObject owner, string property) { var value = owner[property] as JArray; Checks.Require(value != null, "INVALID_IMPORT", "GLB property is missing: " + property); return value; }
        static int Int(JToken owner, string property, int minimum, int maximum)
        { var token = owner[property]; Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_IMPORT", "GLB integer property is invalid: " + property); int value = (int)token; Checks.Require(value >= minimum && value <= maximum, "INVALID_IMPORT", "GLB integer property is out of range: " + property); return value; }
        static int AccessorId(JObject attributes, string name) { return Int(attributes, name, 0, int.MaxValue); }
        static int[] IndexAccessor(JArray accessors, JArray views, byte[] bin, int accessorId)
        {
            var accessor = Accessor(accessors, accessorId, "SCALAR", new[] { 5121, 5123, 5125 }); int components = 1; var result = new int[Count(accessor, AuthoringLimits.MaxIndices)];
            for (int i = 0; i < result.Length; i++) result[i] = ReadComponent(accessor, views, bin, i, components, 0);
            return result;
        }
        static Vec3[] Vec3Accessor(JArray accessors, JArray views, byte[] bin, int accessorId, string label)
        { var accessor = Accessor(accessors, accessorId, "VEC3", new[] { 5126 }); return ReadVectors(accessor, views, bin, 3, label).Select(v => new Vec3(v[0], v[1], v[2])).ToArray(); }
        static Vec3[] OptionalVec3(JObject attrs, string name, JArray accessors, JArray views, byte[] bin, int count, string label)
        { if (attrs[name] == null) return System.Array.Empty<Vec3>(); var result = Vec3Accessor(accessors, views, bin, Int(attrs, name, 0, accessors.Count - 1), label); Checks.Require(result.Length == count, "INVALID_IMPORT", label + " count differs from POSITION."); return result; }
        static Vec4[] OptionalVec4(JObject attrs, string name, JArray accessors, JArray views, byte[] bin, int count, string label)
        { if (attrs[name] == null) return System.Array.Empty<Vec4>(); var accessor = Accessor(accessors, Int(attrs, name, 0, accessors.Count - 1), "VEC4", new[] { 5126 }); var result = ReadVectors(accessor, views, bin, 4, label).Select(v => new Vec4(v[0], v[1], v[2], v[3])).ToArray(); Checks.Require(result.Length == count, "INVALID_IMPORT", label + " count differs from POSITION."); return result; }
        static Vec2[] OptionalVec2(JObject attrs, string name, JArray accessors, JArray views, byte[] bin, int count, string label)
        { if (attrs[name] == null) return System.Array.Empty<Vec2>(); var accessor = Accessor(accessors, Int(attrs, name, 0, accessors.Count - 1), "VEC2", new[] { 5126 }); var result = ReadVectors(accessor, views, bin, 2, label).Select(v => new Vec2(v[0], v[1])).ToArray(); Checks.Require(result.Length == count, "INVALID_IMPORT", label + " count differs from POSITION."); return result; }
        static JObject Accessor(JArray accessors, int id, string type, int[] componentTypes)
        { Checks.Require(id >= 0 && id < accessors.Count, "INVALID_IMPORT", "GLB accessor reference is out of range."); var accessor = accessors[id] as JObject; Checks.Require(accessor != null && (string)accessor["type"] == type, "UNSUPPORTED_FORMAT", "GLB accessor type is unsupported."); int component = Int(accessor, "componentType", 0, int.MaxValue); Checks.Require(componentTypes.Contains(component), "UNSUPPORTED_FORMAT", "GLB accessor component type is unsupported."); Checks.Require(accessor["sparse"] == null, "UNSUPPORTED_FORMAT", "Sparse accessors are not supported yet."); return accessor; }
        static int Count(JObject accessor) { return Count(accessor, AuthoringLimits.MaxVertices); }
        static int Count(JObject accessor, int maximum) { return Int(accessor, "count", 1, maximum); }
        static float[][] ReadVectors(JObject accessor, JArray views, byte[] bin, int components, string label)
        {
            int count = Count(accessor), accessorOffset = accessor["byteOffset"] == null ? 0 : Int(accessor, "byteOffset", 0, int.MaxValue); int viewId = Int(accessor, "bufferView", 0, views.Count - 1); var view = (JObject)views[viewId];
            int viewOffset = view["byteOffset"] == null ? 0 : Int(view, "byteOffset", 0, int.MaxValue); int stride = view["byteStride"] == null ? components * 4 : Int(view, "byteStride", components * 4, 4096); int viewLength = Int(view, "byteLength", 0, bin.Length);
            int start = checked(viewOffset + accessorOffset); int last = checked(start + (count - 1) * stride + components * 4); Checks.Require(start >= 0 && last <= bin.Length && start + (count - 1) * stride + components * 4 <= viewOffset + viewLength, "INVALID_IMPORT", label + " accessor exceeds its bufferView.");
            var result = new float[count][]; for (int i = 0; i < count; i++) { result[i] = new float[components]; for (int c = 0; c < components; c++) result[i][c] = BitConverter.ToSingle(bin, start + i * stride + c * 4); }
            return result;
        }
        static int ReadComponent(JObject accessor, JArray views, byte[] bin, int index, int components, int component)
        {
            int count = Count(accessor); Checks.Require(index >= 0 && index < count, "INVALID_IMPORT", "Index accessor is out of range."); int accessorOffset = accessor["byteOffset"] == null ? 0 : Int(accessor, "byteOffset", 0, int.MaxValue); int viewId = Int(accessor, "bufferView", 0, views.Count - 1); var view = (JObject)views[viewId]; int viewOffset = view["byteOffset"] == null ? 0 : Int(view, "byteOffset", 0, int.MaxValue); int componentType = Int(accessor, "componentType", 0, int.MaxValue); int width = componentType == 5121 ? 1 : componentType == 5123 ? 2 : 4; int stride = view["byteStride"] == null ? width * components : Int(view, "byteStride", width * components, 4096); int offset = checked(viewOffset + accessorOffset + index * stride + component * width); Checks.Require(offset + width <= bin.Length, "INVALID_IMPORT", "Index accessor exceeds its bufferView.");
            if (componentType == 5121) return bin[offset]; if (componentType == 5123) return BitConverter.ToUInt16(bin, offset); return checked((int)BitConverter.ToUInt32(bin, offset));
        }
    }
}
