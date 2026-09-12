using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    public sealed partial class GraphNode
    {
        public static GraphNode MorphSetNode(string id, MorphSet morphs)
        {
            Checks.Require(morphs != null, "INVALID_MORPH", "Morph set payload is required.");
            return new GraphNode(id, BuiltinNodes.MorphSet, 1, null, Identity, 0, 0, 0, true, "", "", Empty, "") { Morphs = morphs };
        }

        public static GraphNode MorphDeformNode(string id, IReadOnlyDictionary<string, float> weights = null)
        {
            return new GraphNode(id, BuiltinNodes.MorphDeform, 1, null, Identity, 0, 0, 0, true, "", "", Empty, "")
            {
                MorphWeights = CopyMorphWeights(weights)
            };
        }

        internal static IReadOnlyDictionary<string, float> CopyMorphWeights(IReadOnlyDictionary<string, float> weights)
        {
            var copy = new Dictionary<string, float>(StringComparer.Ordinal);
            if (weights == null) return new ReadOnlyDictionary<string, float>(copy);
            Checks.Require(weights.Count <= MorphSet.MaxTargets, "BUDGET_EXCEEDED", "Morph weight count exceeds capacity.");
            foreach (var pair in weights)
            {
                Checks.Id(pair.Key); Checks.Finite(pair.Value);
                Checks.Require(pair.Value >= 0 && pair.Value <= 1, "INVALID_MORPH_WEIGHT", "Morph weight must be between 0 and 1.");
                Checks.Require(!copy.ContainsKey(pair.Key), "DUPLICATE_MORPH", "Morph weight identity must be unique.");
                copy.Add(pair.Key, pair.Value);
            }
            return new ReadOnlyDictionary<string, float>(copy.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
        }
    }
}
