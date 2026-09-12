using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>One expression translated to native MorphSet target IDs for a single GLB owner.</summary>
    public sealed class MappedVrmExpression
    {
        public string Name { get; private set; }
        public string Preset { get; private set; }
        public bool IsCustom { get; private set; }
        public IReadOnlyDictionary<string, float> Weights { get; private set; }

        internal MappedVrmExpression(VrmExpression source, IDictionary<string, float> weights)
        {
            Checks.Require(source != null && weights != null, "INVALID_VRM", "VRM expression mapping is incomplete.");
            Initialize(source.Name, source.Preset, source.IsCustom, weights);
        }

        internal MappedVrmExpression(string name, string preset, bool isCustom, IDictionary<string, float> weights)
        { Initialize(name, preset, isCustom, weights); }

        void Initialize(string name, string preset, bool isCustom, IDictionary<string, float> weights)
        {
            Checks.Name(name); Checks.Require(weights != null && weights.Count <= MorphSet.MaxTargets, "BUDGET_EXCEEDED", "VRM mapped expression exceeds capacity.");
            var copy = new Dictionary<string, float>(StringComparer.Ordinal);
            foreach (var pair in weights) { Checks.Id(pair.Key); Checks.Finite(pair.Value); Checks.Require(pair.Value >= 0 && pair.Value <= 1, "INVALID_MORPH_WEIGHT", "VRM mapped expression weight must be between 0 and 1."); Checks.Require(copy.TryAdd(pair.Key, pair.Value), "DUPLICATE_MORPH", "VRM mapped expression target repeats."); }
            Name = name; Preset = preset ?? ""; IsCustom = isCustom; Weights = new ReadOnlyDictionary<string, float>(copy.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        }
    }

    /// <summary>Maps bounded VRM morph indices to the stable IDs emitted by the one-mesh GLB importer.</summary>
    public static class VrmExpressionMapper
    {
        /// <summary>Resolves the owner for the single-mesh GLB profile before mapping expression binds.</summary>
        public static IReadOnlyList<MappedVrmExpression> ResolveForImportedMesh(byte[] bytes, VrmMetadata metadata, MorphSet morphs)
        {
            var document = GlbDocumentReader.Read(bytes);
            Checks.Require(metadata != null && metadata.SourceHash == document.SourceHash, "SOURCE_CHANGED", "VRM metadata belongs to another source file.");
            Checks.Require(morphs != null, "INVALID_VRM", "Imported MorphSet is required.");
            var meshes = document.Root["meshes"] as JArray; Checks.Require(meshes != null && meshes.Count == 1, "UNSUPPORTED_FORMAT", "Expression mapping requires one mesh.");
            int ownerIndex = 0;
            if (metadata.Format == "vrm1")
            {
                var nodes = document.Root["nodes"] as JArray; Checks.Require(nodes != null, "INVALID_VRM", "VRM expression mapping requires nodes.");
                var owners = new List<int>();
                for (int i = 0; i < nodes.Count; i++) { var node = nodes[i] as JObject; if (node == null) throw new AuthoringException("INVALID_VRM", "VRM expression node is invalid."); var mesh = node["mesh"]; if (mesh != null && mesh.Type == JTokenType.Integer && (int)mesh == 0) owners.Add(i); else if (mesh != null && mesh.Type != JTokenType.Integer) throw new AuthoringException("INVALID_VRM", "VRM expression node mesh index is invalid."); }
                Checks.Require(owners.Count == 1, "UNSUPPORTED_FORMAT", "Expression mapping requires exactly one VRM mesh owner node."); ownerIndex = owners[0];
            }
            else Checks.Require(metadata.Format == "vrm0", "UNSUPPORTED_FORMAT", "Expression mapping does not support this VRM format.");
            return ResolveForSingleOwner(metadata, morphs, ownerIndex);
        }

        public static IReadOnlyList<MappedVrmExpression> ResolveForSingleOwner(VrmMetadata metadata, MorphSet morphs, int ownerIndex)
        {
            Checks.Require(metadata != null && morphs != null, "INVALID_VRM", "VRM metadata and MorphSet are required."); Checks.Require(ownerIndex >= 0, "INVALID_VRM", "VRM owner index is invalid.");
            var result = new List<MappedVrmExpression>(metadata.Expressions.Count);
            foreach (var expression in metadata.Expressions)
            {
                var weights = new Dictionary<string, float>(StringComparer.Ordinal);
                foreach (var binding in expression.MorphBindings)
                {
                    Checks.Require(binding.OwnerIndex == ownerIndex, "UNSUPPORTED_FORMAT", "VRM expression references more than the selected mesh owner.");
                    string targetId = GlbImporter.MorphTargetId(metadata.SourceHash, binding.MorphIndex);
                    Checks.Require(morphs.ById.ContainsKey(targetId), "INVALID_VRM", "VRM expression morph index is outside the imported MorphSet.");
                    Checks.Require(weights.TryAdd(targetId, binding.Weight), "INVALID_VRM", "VRM expression repeats a morph target bind.");
                }
                result.Add(new MappedVrmExpression(expression, weights));
            }
            return result.AsReadOnly();
        }
    }
}
