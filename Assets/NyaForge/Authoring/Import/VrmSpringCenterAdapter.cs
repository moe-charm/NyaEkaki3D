using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    internal static class VrmSpringCenterAdapter
    {
        internal static IReadOnlyDictionary<string, PoseTransform> Create(IReadOnlyList<VrmSpringChainBinding> bindings,
            ImportedRigSession rig, AuthoringGraph graph, PoseSet pose)
        {
            rig.Resolve(graph);
            var skeleton = graph.Nodes[rig.SkeletonNodeId].Skeleton;
            pose.ValidateFor(skeleton);
            var nodeByBone = rig.NodeToBone.ToDictionary(pair => pair.Value, pair => pair.Key);
            var centers = new Dictionary<string, PoseTransform>();
            foreach (var chain in bindings)
            {
                var transform = PoseTransform.Identity;
                if (chain.CenterBoneId != "")
                {
                    var bone = skeleton.ById[chain.CenterBoneId]; var current = pose.ByBoneId[bone.BoneId].Transform;
                    var origin = rig.SourceNodeOrigins[nodeByBone[bone.BoneId]];
                    transform = new PoseTransform(current.XAxis, current.YAxis, current.ZAxis, current.TransformPoint(origin - bone.Head));
                }
                foreach (var pair in chain.Pairs) centers.Add(pair.HeadBoneId, transform);
            }
            return centers;
        }
    }
}
