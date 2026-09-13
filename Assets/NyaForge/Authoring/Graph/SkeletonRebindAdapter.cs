using System.Collections.Generic;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Immutable graph-level wrapper for an explicit skeleton rebind.</summary>
    public static class GraphSkeletonRebindAdapter
    {
        public static AuthoringGraph Rebind(AuthoringGraph graph, MeshData mesh,
            string skeletonNodeId, string bindingNodeId, string poseNodeId,
            SkeletonDefinition targetSkeleton,
            IReadOnlyDictionary<string, string> sourceToTarget)
        {
            Checks.Require(graph != null && mesh != null && targetSkeleton != null,
                "INVALID_SKELETON", "Graph, mesh and target skeleton are required.");
            Checks.Id(skeletonNodeId); Checks.Id(bindingNodeId); Checks.Id(poseNodeId);
            Checks.Require(graph.Nodes.TryGetValue(skeletonNodeId, out var skeletonNode) && skeletonNode.TypeId == BuiltinNodes.Skeleton && skeletonNode.Skeleton != null,
                "SKELETON_REBIND_NODE", "The source skeleton node is missing.");
            Checks.Require(graph.Nodes.TryGetValue(bindingNodeId, out var bindingNode) && bindingNode.TypeId == BuiltinNodes.SkinBind && bindingNode.Binding != null,
                "SKELETON_REBIND_NODE", "The source skin binding node is missing.");
            Checks.Require(graph.Nodes.TryGetValue(poseNodeId, out var poseNode) && poseNode.TypeId == BuiltinNodes.Pose && poseNode.Pose != null,
                "SKELETON_REBIND_NODE", "The source pose node is missing.");

            var binding = SkeletonRebindAdapter.RebindBinding(bindingNode.Binding, mesh,
                skeletonNode.Skeleton, targetSkeleton, sourceToTarget);
            var pose = SkeletonRebindAdapter.RebindPose(poseNode.Pose,
                skeletonNode.Skeleton, targetSkeleton, sourceToTarget);
            return graph
                .ReplaceNode(GraphNode.SkeletonNode(skeletonNodeId, targetSkeleton))
                .ReplaceNode(GraphNode.SkinBindNode(bindingNodeId, binding))
                .ReplaceNode(GraphNode.PoseNode(poseNodeId, pose));
        }
    }
}
