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
            var normals = mesh.Normals.ToArray(); var tangents = mesh.Tangents.ToArray(); bool normalChanged = false;
            foreach (var pair in weights)
            {
                Checks.Id(pair.Key); Checks.Finite(pair.Value); Checks.Require(pair.Value >= 0 && pair.Value <= 1, "INVALID_MORPH_WEIGHT", "Morph weight must be between 0 and 1.");
                Checks.Require(valid.ById.TryGetValue(pair.Key, out var target), "MORPH_NOT_FOUND", "Morph weight references an unknown target.");
                if (pair.Value == 0) continue;
                foreach (var delta in target.Deltas) positions[delta.Key] = positions[delta.Key] + delta.Value * pair.Value;
                foreach (var delta in target.NormalDeltas) { normals[delta.Key] = normals[delta.Key] + delta.Value * pair.Value; normalChanged = true; }
                foreach (var delta in target.TangentDeltas) { var value = tangents[delta.Key]; var xyz = new Vec3(value.X, value.Y, value.Z) + delta.Value * pair.Value; tangents[delta.Key] = new Vec4(xyz.X, xyz.Y, xyz.Z, value.W); }
            }
            if (normalChanged)
                for (int i = 0; i < normals.Length; i++)
                {
                    var n = normals[i]; var length = Math.Sqrt((double)n.X*n.X + (double)n.Y*n.Y + (double)n.Z*n.Z);
                    Checks.Require(length > 0 && !double.IsNaN(length) && !double.IsInfinity(length), "INVALID_MORPH", "Normal morph produced a zero or non-finite direction.");
                    normals[i] = new Vec3((float)(n.X/length), (float)(n.Y/length), (float)(n.Z/length));
                }
            return new MeshData(positions, normals, tangents, mesh.Uv0.ToArray(), mesh.Submeshes.ToArray());
        }
    }
}
