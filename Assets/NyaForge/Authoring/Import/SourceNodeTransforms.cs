using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Source-indexed local/world frames. No mesh, bind matrix or authored pose is inferred.</summary>
    public sealed class SourceNodeTransforms
    {
        public string SourceHash { get; }
        public ImportedSourceHierarchy Hierarchy { get; }
        public IReadOnlyList<SourceAffine> Local { get; }
        public IReadOnlyList<SourceAffine> World { get; }

        public SourceNodeTransforms(string sourceHash, IReadOnlyList<int> parents,
            IReadOnlyList<SourceAffine> local, IReadOnlyList<IReadOnlyList<int>> children)
        {
            Checks.HashText(sourceHash);
            ImportedSourceHierarchy.ValidateParents(parents);
            Checks.Require(local != null && local.Count == parents.Count && local.All(frame => frame != null),
                "INVALID_IMPORT", "Every source node requires a local affine transform.");
            var localCopy = local.ToArray(); var world = new SourceAffine[localCopy.Length];
            var path = new List<int>();
            for (int i = 0; i < localCopy.Length; i++)
            {
                int node = i;
                while (node >= 0 && world[node] == null) { path.Add(node); node = parents[node]; }
                for (int j = path.Count - 1; j >= 0; j--)
                {
                    node = path[j]; int parent = parents[node];
                    world[node] = parent < 0 ? localCopy[node] : world[parent].Compose(localCopy[node]);
                }
                path.Clear();
            }
            Hierarchy = new ImportedSourceHierarchy(parents, world.Select(frame => frame.TransformPoint(new Vec3())).ToArray(), children);
            SourceHash = sourceHash; Local = Array.AsReadOnly(localCopy); World = Array.AsReadOnly(world);
        }
    }
}
