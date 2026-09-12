using System.Collections.Generic;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Resolves VRM1 head/tail topology independently of dynamics and runtime state.</summary>
    public static class Vrm1SpringChainResolver
    {
        public static IReadOnlyList<VrmSpringChainBinding> Resolve(VrmSpringSession source, ImportedRigSession rig, AuthoringGraph graph)
        {
            Checks.Require(source != null && rig != null, "INVALID_VRM", "Spring and rig sessions are required.");
            rig.ValidateSource(source.SourceHash);
            Checks.Require(source.Format == "vrm1", "UNSUPPORTED_FORMAT", "This resolver requires VRM1 joint pairs.");
            var map = rig.Resolve(graph);
            Checks.Require(rig.SourceNodeOrigins != null, "IMPORT_NODE_SPACE_MISSING", "Source node origins are required for joint pairs.");
            var skeleton = graph.Nodes[rig.SkeletonNodeId].Skeleton;
            var result = new List<VrmSpringChainBinding>();
            var owner = new Dictionary<string, int>();
            foreach (var chain in source.SpringBones)
            {
                Checks.Require(chain != null && chain.RootBoneNodes.Count == 0 && chain.Joints.Count >= 2, "INVALID_VRM_SPRING_CHAIN", "VRM1 preview requires at least a head and a tail joint.");
                var pairs = new List<VrmSpringPairBinding>(); var covered = new HashSet<string>();
                for (int i = 0; i + 1 < chain.Joints.Count; i++)
                {
                    var head = chain.Joints[i]; var tail = chain.Joints[i + 1];
                    Checks.Require(head != null && tail != null, "INVALID_VRM_SPRING_CHAIN", "Spring joint cannot be null.");
                    string headId = map.Resolve(head.NodeIndex), tailId = map.Resolve(tail.NodeIndex);
                    Checks.Require(headId != tailId, "INVALID_VRM_SPRING_CHAIN", "Head and tail must be distinct nodes.");
                    foreach (var id in PathToAncestor(skeleton, tailId, headId)) covered.Add(id);
                    pairs.Add(new VrmSpringPairBinding(headId, tailId, rig.SourceNodeOrigins[head.NodeIndex], rig.SourceNodeOrigins[tail.NodeIndex], head));
                }
                foreach (var id in covered)
                {
                    Checks.Require(!owner.ContainsKey(id), "DUPLICATE_SPRING_JOINT", "Spring chains overlap, including skipped joints.");
                    owner.Add(id, result.Count);
                }
                string center = chain.CenterNodeIndex < 0 ? "" : map.Resolve(chain.CenterNodeIndex);
                if (center != "") PathToAncestor(skeleton, pairs[0].HeadBoneId, center);
                foreach (int group in chain.ColliderGroupIndices)
                    Checks.Require(group >= 0 && group < source.ColliderGroups.Count, "SPRING_COLLIDER_MISSING", "Referenced collider group is missing.");
                result.Add(new VrmSpringChainBinding(chain.Name, center, pairs, chain.ColliderGroupIndices));
            }
            for (int i = 0; i < result.Count; i++)
            {
                string ancestor = result[i].CenterBoneId;
                while (ancestor != "")
                {
                    Checks.Require(!owner.TryGetValue(ancestor, out var chainIndex) || chainIndex == i, "INVALID_VRM_SPRING_CENTER", "Center cannot belong to another spring chain or its descendants.");
                    ancestor = skeleton.ById[ancestor].ParentBoneId;
                }
            }
            return result.AsReadOnly();
        }

        static List<string> PathToAncestor(SkeletonDefinition skeleton, string node, string ancestor)
        {
            var path = new List<string>();
            while (node != "")
            {
                path.Add(node);
                if (node == ancestor) return path;
                node = skeleton.ById[node].ParentBoneId;
            }
            throw new AuthoringException("INVALID_VRM_SPRING_CHAIN", "Spring head/center must be an ancestor of its tail/head.");
        }
    }
}
