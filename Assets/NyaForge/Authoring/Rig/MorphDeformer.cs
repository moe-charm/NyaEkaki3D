using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Applies rest-space morph deltas without baking a pose into the source mesh.</summary>
    public static class MorphDeformer
    {
        public static MeshData Apply(MeshData mesh, MorphSet morphs, IReadOnlyDictionary<string, float> weights)
        {
            Checks.Require(mesh != null && morphs != null && weights != null, "INVALID_MORPH", "Mesh, morph set and weights are required.");
            var valid = morphs.ValidateFor(mesh); var positions = mesh.Positions.ToArray();
            foreach (var pair in weights)
            {
                Checks.Id(pair.Key); Checks.Finite(pair.Value); Checks.Require(pair.Value >= 0 && pair.Value <= 1, "INVALID_MORPH_WEIGHT", "Morph weight must be between 0 and 1.");
                Checks.Require(valid.ById.TryGetValue(pair.Key, out var target), "MORPH_NOT_FOUND", "Morph weight references an unknown target.");
                if (pair.Value == 0) continue;
                foreach (var delta in target.Deltas) positions[delta.Key] = positions[delta.Key] + delta.Value * pair.Value;
            }
            return mesh.WithPositions(positions);
        }
    }
}
