using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>One expression translated to native MorphSet target IDs for a single GLB owner.</summary>
    public sealed class MappedVrmExpression
    {
        public string Name { get; }
        public string Preset { get; }
        public bool IsCustom { get; }
        public IReadOnlyDictionary<string, float> Weights { get; }

        internal MappedVrmExpression(VrmExpression source, IDictionary<string, float> weights)
        {
            Checks.Require(source != null && weights != null, "INVALID_VRM", "VRM expression mapping is incomplete.");
            Name = source.Name; Preset = source.Preset; IsCustom = source.IsCustom;
            Weights = new ReadOnlyDictionary<string, float>(new Dictionary<string, float>(weights, StringComparer.Ordinal));
        }
    }

    /// <summary>Maps bounded VRM morph indices to the stable IDs emitted by the one-mesh GLB importer.</summary>
    public static class VrmExpressionMapper
    {
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
                    Checks.Require(binding.MorphIndex < morphs.Targets.Count, "INVALID_VRM", "VRM expression morph index is outside the imported MorphSet.");
                    string targetId = morphs.Targets[binding.MorphIndex].TargetId;
                    Checks.Require(weights.TryAdd(targetId, binding.Weight), "INVALID_VRM", "VRM expression repeats a morph target bind.");
                }
                result.Add(new MappedVrmExpression(expression, weights));
            }
            return result.AsReadOnly();
        }
    }
}
