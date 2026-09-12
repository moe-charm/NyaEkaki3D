using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>All source nodes, indexed by original node index. Origins use source rest space.</summary>
    public sealed class ImportedSourceHierarchy
    {
        public const int MaxNodes = 4096;
        public IReadOnlyList<int> Parents { get; }
        public IReadOnlyList<Vec3> Origins { get; }
        public IReadOnlyList<IReadOnlyList<int>> Children { get; }

        public ImportedSourceHierarchy(IReadOnlyList<int> parents, IReadOnlyList<Vec3> origins, IReadOnlyList<IReadOnlyList<int>> children = null)
        {
            ValidateParents(parents);
            Checks.Require(origins != null && origins.Count == parents.Count, "INVALID_IMPORT", "Source hierarchy origins must cover every node.");
            foreach (var origin in origins) Checks.Finite(origin);
            var ordered = new IReadOnlyList<int>[parents.Count];
            Checks.Require(children == null || children.Count == parents.Count, "INVALID_IMPORT", "Child lists must cover every source node.");
            var seen = new HashSet<int>();
            for (int i = 0; i < parents.Count; i++)
            {
                var list = children == null ? Enumerable.Range(0, parents.Count).Where(n => parents[n] == i).ToArray() : children[i]?.ToArray();
                Checks.Require(list != null, "INVALID_IMPORT", "Source child list is missing.");
                foreach (int child in list) Checks.Require(child >= 0 && child < parents.Count && parents[child] == i && seen.Add(child), "INVALID_IMPORT", "Source children disagree with parents.");
                ordered[i] = Array.AsReadOnly(list);
            }
            Checks.Require(seen.Count == parents.Count(p => p >= 0), "INVALID_IMPORT", "Source child list is incomplete.");
            Children = Array.AsReadOnly(ordered);
            Parents = Array.AsReadOnly(parents.ToArray()); Origins = Array.AsReadOnly(origins.ToArray());
        }

        internal static ImportedSourceHierarchy FromTranslations(IReadOnlyList<int> parents, IReadOnlyList<Vec3> local, IReadOnlyList<IReadOnlyList<int>> children = null)
        {
            ValidateParents(parents);
            Checks.Require(local != null && local.Count == parents.Count, "INVALID_IMPORT", "Source translations must cover every node.");
            var world = new Vec3[parents.Count]; var done = new bool[parents.Count]; var path = new List<int>();
            for (int i = 0; i < parents.Count; i++)
            {
                int node = i;
                while (node >= 0 && !done[node]) { path.Add(node); node = parents[node]; }
                for (int j = path.Count - 1; j >= 0; j--)
                {
                    node = path[j]; Checks.Finite(local[node]);
                    world[node] = parents[node] < 0 ? local[node] : world[parents[node]] + local[node];
                    Checks.Finite(world[node]); done[node] = true;
                }
                path.Clear();
            }
            return new ImportedSourceHierarchy(parents, world, children);
        }

        internal void ValidateJointOrigins(IReadOnlyDictionary<int, Vec3> origins)
        {
            Checks.Require(origins != null, "INVALID_IMPORT", "Hierarchy requires known joint origins.");
            foreach (var pair in origins)
                Checks.Require(pair.Key >= 0 && pair.Key < Origins.Count && Origins[pair.Key].Equals(pair.Value), "INVALID_IMPORT", "Hierarchy and joint origins disagree.");
        }

        internal static void ValidateParents(IReadOnlyList<int> parents)
        {
            Checks.Require(parents != null && parents.Count > 0 && parents.Count <= MaxNodes, "BUDGET_EXCEEDED", "Source hierarchy requires 1..4096 nodes.");
            for (int i = 0; i < parents.Count; i++)
                Checks.Require(parents[i] >= -1 && parents[i] < parents.Count && parents[i] != i, "INVALID_IMPORT", "Source parent is invalid.");
            var state = new byte[parents.Count];
            for (int i = 0; i < parents.Count; i++)
            {
                int node = i;
                while (node >= 0 && state[node] == 0) { state[node] = 1; node = parents[node]; }
                Checks.Require(node < 0 || state[node] == 2, "BONE_CYCLE", "Source hierarchy contains a cycle.");
                node = i;
                while (node >= 0 && state[node] == 1) { state[node] = 2; node = parents[node]; }
            }
        }
    }
}
