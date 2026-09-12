using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>A source VRM morph bind. OwnerIndex is a glTF node in VRM 1.0 and a mesh in VRM 0.x.</summary>
    public sealed class VrmMorphBinding
    {
        public int OwnerIndex { get; }
        public int MorphIndex { get; }
        public float Weight { get; }

        internal VrmMorphBinding(int ownerIndex, int morphIndex, float weight)
        {
            Checks.Require(ownerIndex >= 0 && morphIndex >= 0, "INVALID_VRM", "VRM morph bind index is invalid."); Checks.Finite(weight); Checks.Require(weight >= 0 && weight <= 1, "INVALID_VRM", "VRM morph bind weight must be between 0 and 1.");
            OwnerIndex = ownerIndex; MorphIndex = morphIndex; Weight = weight == 0 ? 0 : weight;
        }
    }

    /// <summary>Bounded VRM expression inventory. Binding payloads stay in the source adapter until a morph/material adapter exists.</summary>
    public sealed class VrmExpression
    {
        public string Name { get; }
        public string Preset { get; }
        public bool IsCustom { get; }
        public int MorphTargetBindCount { get; }
        public int MaterialBindCount { get; }
        public IReadOnlyList<VrmMorphBinding> MorphBindings { get; }

        internal VrmExpression(string name, string preset, bool isCustom, IEnumerable<VrmMorphBinding> morphBindings, int materialBindCount)
        {
            Checks.Name(name); Checks.Require(morphBindings != null && materialBindCount >= 0, "INVALID_VRM", "VRM expression bind count is invalid.");
            var bindings = morphBindings.ToArray(); Checks.Require(bindings.Length <= VrmMetadata.MaxExpressions, "BUDGET_EXCEEDED", "VRM morph bind count exceeds capacity.");
            Name = name; Preset = preset ?? ""; IsCustom = isCustom; MorphBindings = Array.AsReadOnly(bindings); MorphTargetBindCount = bindings.Length; MaterialBindCount = materialBindCount;
        }
    }

    /// <summary>Small, source-pinned VRM identity and humanoid mapping independent of UniVRM.</summary>
    public sealed class VrmMetadata
    {
        public const int MaxExpressions = 256;
        public string SourceHash { get; }
        public string Format { get; }
        public string SpecVersion { get; }
        public string Title { get; }
        public string Author { get; }
        public IReadOnlyDictionary<string, int> HumanoidNodes { get; }
        public IReadOnlyList<VrmExpression> Expressions { get; }
        public IReadOnlyList<string> Warnings { get; }

        internal VrmMetadata(string sourceHash, string format, string specVersion, string title, string author, IDictionary<string, int> humanoidNodes, IEnumerable<VrmExpression> expressions, IEnumerable<string> warnings)
        {
            Checks.HashText(sourceHash); Checks.Name(format); Checks.Name(specVersion); Checks.Require(humanoidNodes != null, "INVALID_VRM", "Humanoid mapping is required.");
            SourceHash = sourceHash; Format = format; SpecVersion = specVersion; Title = title ?? ""; Author = author ?? "";
            var expressionValues = (expressions ?? Array.Empty<VrmExpression>()).ToArray(); Checks.Require(expressionValues.Length <= MaxExpressions, "BUDGET_EXCEEDED", "VRM expression count exceeds capacity.");
            Expressions = Array.AsReadOnly(expressionValues); HumanoidNodes = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(humanoidNodes, StringComparer.Ordinal)); Warnings = Array.AsReadOnly((warnings ?? Array.Empty<string>()).ToArray());
        }
    }

    /// <summary>Reads only VRM 0.x/1.0 metadata from the GLB JSON extension.</summary>
    public static class VrmMetadataReader
    {
        public static bool ContainsVrm(byte[] bytes)
        {
            var extensions = GlbDocumentReader.Read(bytes).Root["extensions"] as JObject;
            return extensions != null && (extensions["VRMC_vrm"] is JObject || extensions["VRM"] is JObject);
        }

        public static VrmMetadata Read(byte[] bytes)
        {
            var document = GlbDocumentReader.Read(bytes); var extensions = document.Root["extensions"] as JObject;
            Checks.Require(extensions != null, "UNSUPPORTED_FORMAT", "The GLB does not contain a VRM extension.");
            var modern = extensions["VRMC_vrm"] as JObject; var legacy = extensions["VRM"] as JObject;
            Checks.Require(modern != null || legacy != null, "UNSUPPORTED_FORMAT", "The GLB does not contain a supported VRM extension.");
            if (modern != null) return ParseModern(document, modern);
            return ParseLegacy(document, legacy);
        }

        static VrmMetadata ParseModern(GlbDocument document, JObject extension)
        {
            string spec = StringProperty(extension, "specVersion", 64, "specVersion"); Checks.Require(spec == "1.0", "UNSUPPORTED_FORMAT", "Only VRM 1.0 metadata is supported.");
            var meta = extension["meta"] as JObject; Checks.Require(meta != null, "INVALID_VRM", "VRMC_vrm meta is required.");
            var map = ParseModernHumanoid(document.Root, extension["humanoid"] as JObject);
            var expressions = ParseModernExpressions(document.Root, extension);
            var warnings = new List<string> { "VRM 1.0 identity, humanoid and expression inventory were read; expression application, look-at, spring bones and material conversion remain separate adapters." };
            return new VrmMetadata(document.SourceHash, "vrm1", spec, OptionalString(meta, "name", 256), OptionalString(meta, "authors", 256), map, expressions, warnings);
        }

        static VrmMetadata ParseLegacy(GlbDocument document, JObject extension)
        {
            string spec = OptionalString(extension, "specVersion", 64); Checks.Require(spec == "0.0" || spec == "0.0.0" || spec == "", "UNSUPPORTED_FORMAT", "Only VRM 0.x metadata is supported.");
            var meta = extension["meta"] as JObject; Checks.Require(meta != null, "INVALID_VRM", "VRM meta is required.");
            var map = ParseLegacyHumanoid(document.Root, extension["humanoid"] as JObject);
            var expressions = ParseLegacyExpressions(document.Root, extension);
            var warnings = new List<string> { "VRM 0.x identity, humanoid and expression inventory were read; VRM 1.0 conversion, expression application and spring bones remain separate adapters." };
            return new VrmMetadata(document.SourceHash, "vrm0", spec == "" ? "0.0" : spec, OptionalString(meta, "title", 256), OptionalString(meta, "author", 256), map, expressions, warnings);
        }

        static IReadOnlyList<VrmExpression> ParseModernExpressions(JObject root, JObject extension)
        {
            var owner = extension["expressions"] as JObject; if (owner == null) return System.Array.Empty<VrmExpression>();
            var result = new List<VrmExpression>(); var names = new HashSet<string>(StringComparer.Ordinal);
            ParseModernExpressionGroup(owner["preset"], false, result, names, Array(root, "nodes").Count);
            ParseModernExpressionGroup(owner["custom"], true, result, names, Array(root, "nodes").Count);
            Checks.Require(result.Count <= VrmMetadata.MaxExpressions, "BUDGET_EXCEEDED", "VRM expression count exceeds capacity."); return result.AsReadOnly();
        }

        static void ParseModernExpressionGroup(JToken token, bool custom, IList<VrmExpression> result, ISet<string> names, int nodeCount)
        {
            if (token == null || token.Type == JTokenType.Null) return;
            var group = token as JObject; Checks.Require(group != null, "INVALID_VRM", "VRM expression group is invalid.");
            foreach (var property in group.Properties())
            {
                var value = property.Value as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM expression entry is invalid.");
                string name = StringPropertyName(property.Name, "expression name"); Checks.Require(names.Add(name), "INVALID_VRM", "VRM expression name repeats.");
                result.Add(new VrmExpression(name, custom ? "" : name, custom, ModernMorphBindings(value, nodeCount), BindCount(value, "materialColorBinds")));
            }
        }

        static IReadOnlyList<VrmExpression> ParseLegacyExpressions(JObject root, JObject extension)
        {
            var master = extension["blendShapeMaster"] as JObject; if (master == null) return System.Array.Empty<VrmExpression>();
            var groups = master["blendShapeGroups"] as JArray; Checks.Require(groups != null, "INVALID_VRM", "VRM blendShapeGroups is invalid.");
            int meshCount = Array(root, "meshes").Count;
            Checks.Require(groups.Count <= VrmMetadata.MaxExpressions, "BUDGET_EXCEEDED", "VRM expression count exceeds capacity.");
            var result = new List<VrmExpression>(); var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var token in groups)
            {
                var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM blendShapeGroup is invalid.");
                string preset = OptionalString(value, "presetName", 128); string name = OptionalString(value, "name", 128); if (name.Length == 0) name = preset;
                Checks.Require(name.Length > 0, "INVALID_VRM", "VRM blendShapeGroup needs a name or presetName."); Checks.Require(names.Add(name), "INVALID_VRM", "VRM expression name repeats.");
                result.Add(new VrmExpression(name, preset, preset.Length == 0, LegacyMorphBindings(value, meshCount), BindCount(value, "materialValues")));
            }
            return result.AsReadOnly();
        }

        static IReadOnlyList<VrmMorphBinding> ModernMorphBindings(JObject owner, int nodeCount)
        {
            var value = owner["morphTargetBinds"]; if (value == null || value.Type == JTokenType.Null) return System.Array.Empty<VrmMorphBinding>();
            var array = value as JArray; Checks.Require(array != null && array.Count <= VrmMetadata.MaxExpressions, "INVALID_VRM", "VRM expression bind list is invalid: morphTargetBinds");
            var result = new List<VrmMorphBinding>(array.Count);
            foreach (var token in array) { var bind = token as JObject; Checks.Require(bind != null, "INVALID_VRM", "VRM morph bind is invalid."); int node = IntProperty(bind, "node", 0, nodeCount - 1, "VRM morph bind node"); int index = IntProperty(bind, "index", 0, int.MaxValue, "VRM morph bind index"); float weight = NumberProperty(bind, "weight", 0, 1, "VRM morph bind weight"); result.Add(new VrmMorphBinding(node, index, weight)); }
            return result.AsReadOnly();
        }

        static IReadOnlyList<VrmMorphBinding> LegacyMorphBindings(JObject owner, int meshCount)
        {
            var value = owner["binds"]; if (value == null || value.Type == JTokenType.Null) return System.Array.Empty<VrmMorphBinding>();
            var array = value as JArray; Checks.Require(array != null && array.Count <= VrmMetadata.MaxExpressions, "INVALID_VRM", "VRM expression bind list is invalid: binds");
            var result = new List<VrmMorphBinding>(array.Count);
            foreach (var token in array) { var bind = token as JObject; Checks.Require(bind != null, "INVALID_VRM", "VRM morph bind is invalid."); int mesh = IntProperty(bind, "mesh", 0, meshCount - 1, "VRM morph bind mesh"); int index = IntProperty(bind, "index", 0, int.MaxValue, "VRM morph bind index"); float sourceWeight = NumberProperty(bind, "weight", 0, 100, "VRM morph bind weight"); result.Add(new VrmMorphBinding(mesh, index, sourceWeight / 100f)); }
            return result.AsReadOnly();
        }

        static int BindCount(JObject owner, string property)
        {
            var value = owner[property]; if (value == null || value.Type == JTokenType.Null) return 0;
            var array = value as JArray; Checks.Require(array != null && array.Count <= VrmMetadata.MaxExpressions, "INVALID_VRM", "VRM expression bind list is invalid: " + property); return array.Count;
        }

        static IDictionary<string, int> ParseModernHumanoid(JObject root, JObject humanoid)
        {
            Checks.Require(humanoid != null, "INVALID_VRM", "VRMC_vrm humanoid is required."); var bones = humanoid["humanBones"] as JObject; Checks.Require(bones != null, "INVALID_VRM", "VRMC_vrm humanBones is required.");
            var nodes = Array(root, "nodes"); var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in bones.Properties()) { var value = pair.Value as JObject; Checks.Require(value != null, "INVALID_VRM", "VRMC_vrm human bone entry is invalid."); AddBone(map, pair.Name, IntProperty(value, "node", 0, nodes.Count - 1, "human bone node")); }
            return map;
        }

        static IDictionary<string, int> ParseLegacyHumanoid(JObject root, JObject humanoid)
        {
            Checks.Require(humanoid != null, "INVALID_VRM", "VRM humanoid is required."); var bones = humanoid["humanBones"] as JArray; Checks.Require(bones != null, "INVALID_VRM", "VRM humanBones is required.");
            var nodes = Array(root, "nodes"); var map = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var token in bones) { var value = token as JObject; Checks.Require(value != null, "INVALID_VRM", "VRM human bone entry is invalid."); string bone = StringProperty(value, "bone", 128, "human bone name"); AddBone(map, bone, IntProperty(value, "node", 0, nodes.Count - 1, "human bone node")); }
            return map;
        }

        static void AddBone(IDictionary<string, int> map, string name, int node) { Checks.Require(map.TryAdd(name, node), "INVALID_VRM", "VRM humanoid mapping repeats a bone."); }
        static JArray Array(JObject owner, string property) { var value = owner[property] as JArray; Checks.Require(value != null, "INVALID_VRM", "VRM property is missing: " + property); return value; }
        static int IntProperty(JObject owner, string property, int minimum, int maximum, string label) { var token = owner[property]; Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_VRM", label + " is invalid."); int value = (int)token; Checks.Require(value >= minimum && value <= maximum, "INVALID_VRM", label + " is out of range."); return value; }
        static float NumberProperty(JObject owner, string property, float minimum, float maximum, string label) { var token = owner[property]; Checks.Require(token != null && (token.Type == JTokenType.Float || token.Type == JTokenType.Integer), "INVALID_VRM", label + " is invalid."); float value = (float)token; Checks.Finite(value); Checks.Require(value >= minimum && value <= maximum, "INVALID_VRM", label + " is out of range."); return value; }
        static string StringProperty(JObject owner, string property, int maximum, string label) { var token = owner[property]; Checks.Require(token != null && token.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)token) && ((string)token).Length <= maximum, "INVALID_VRM", label + " is invalid."); return (string)token; }
        static string OptionalString(JObject owner, string property, int maximum) { var token = owner[property]; if (token == null || token.Type == JTokenType.Null) return ""; Checks.Require(token.Type == JTokenType.String && ((string)token).Length <= maximum, "INVALID_VRM", "VRM text property is invalid: " + property); return (string)token; }
        static string StringPropertyName(string name, string label) { Checks.Require(!string.IsNullOrWhiteSpace(name) && name.Length <= 128 && name.IndexOf('\0') < 0, "INVALID_VRM", label + " is invalid."); return name; }
    }
}
