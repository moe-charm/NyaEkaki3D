using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Small, source-pinned VRM identity and humanoid mapping independent of UniVRM.</summary>
    public sealed class VrmMetadata
    {
        public string SourceHash { get; }
        public string Format { get; }
        public string SpecVersion { get; }
        public string Title { get; }
        public string Author { get; }
        public IReadOnlyDictionary<string, int> HumanoidNodes { get; }
        public IReadOnlyList<string> Warnings { get; }

        internal VrmMetadata(string sourceHash, string format, string specVersion, string title, string author, IDictionary<string, int> humanoidNodes, IEnumerable<string> warnings)
        {
            Checks.HashText(sourceHash); Checks.Name(format); Checks.Name(specVersion); Checks.Require(humanoidNodes != null, "INVALID_VRM", "Humanoid mapping is required.");
            SourceHash = sourceHash; Format = format; SpecVersion = specVersion; Title = title ?? ""; Author = author ?? "";
            HumanoidNodes = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(humanoidNodes, StringComparer.Ordinal)); Warnings = Array.AsReadOnly((warnings ?? Array.Empty<string>()).ToArray());
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
            var warnings = new List<string> { "VRM 1.0 metadata was read; expressions, look-at, spring bones and material conversion remain separate adapters." };
            return new VrmMetadata(document.SourceHash, "vrm1", spec, OptionalString(meta, "name", 256), OptionalString(meta, "authors", 256), map, warnings);
        }

        static VrmMetadata ParseLegacy(GlbDocument document, JObject extension)
        {
            string spec = OptionalString(extension, "specVersion", 64); Checks.Require(spec == "0.0" || spec == "0.0.0" || spec == "", "UNSUPPORTED_FORMAT", "Only VRM 0.x metadata is supported.");
            var meta = extension["meta"] as JObject; Checks.Require(meta != null, "INVALID_VRM", "VRM meta is required.");
            var map = ParseLegacyHumanoid(document.Root, extension["humanoid"] as JObject);
            var warnings = new List<string> { "VRM 0.x metadata was read; VRM 1.0 conversion and expressions/spring bones remain separate adapters." };
            return new VrmMetadata(document.SourceHash, "vrm0", spec == "" ? "0.0" : spec, OptionalString(meta, "title", 256), OptionalString(meta, "author", 256), map, warnings);
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
        static string StringProperty(JObject owner, string property, int maximum, string label) { var token = owner[property]; Checks.Require(token != null && token.Type == JTokenType.String && !string.IsNullOrWhiteSpace((string)token) && ((string)token).Length <= maximum, "INVALID_VRM", label + " is invalid."); return (string)token; }
        static string OptionalString(JObject owner, string property, int maximum) { var token = owner[property]; if (token == null || token.Type == JTokenType.Null) return ""; Checks.Require(token.Type == JTokenType.String && ((string)token).Length <= maximum, "INVALID_VRM", "VRM text property is invalid: " + property); return (string)token; }
    }
}
