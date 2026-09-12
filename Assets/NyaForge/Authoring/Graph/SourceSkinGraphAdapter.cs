using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Bridges a renderable graph value to the source-space skin deformer.</summary>
    /// <remarks>
    /// This is deliberately an adapter rather than a new persisted graph node. The graph keeps
    /// its authored BoneId/4-influence contract; complete source slots and bind matrices remain
    /// in ImportedRigSession. Callers must provide a palette in the source world space.
    /// </remarks>
    public static class SourceSkinGraphAdapter
    {
        public static GraphMeshValue Apply(GraphMeshValue input, SourceSkin skin,
            SourceSkinBinding binding, IReadOnlyList<SourceAffine> posedJointWorld)
        {
            return Apply(input, skin, binding, posedJointWorld, null);
        }

        static GraphMeshValue Apply(GraphMeshValue input, SourceSkin skin,
            SourceSkinBinding binding, IReadOnlyList<SourceAffine> posedJointWorld, GraphMeshValue appearance)
        {
            Checks.Require(input != null && input.Mesh != null, "INPUT_UNRESOLVED", "Source skin needs a renderable graph mesh.");
            Checks.Require(input.Polygon == null, "EDIT_MODE_UNSUPPORTED", "Source skin cannot deform a polygon editing value.");
            var mesh = SourceSkinDeformer.Apply(input.Mesh, skin, binding, posedJointWorld);
            return (appearance ?? input).WithMesh(mesh);
        }

        /// <summary>Applies complete source skin data to the mesh input of the authored SkinDeform node.</summary>
        public static GraphMeshValue ApplyToEvaluation(GraphEvaluation evaluation, AuthoringGraph graph,
            ImportedRigSession session)
        {
            Checks.Require(evaluation != null && graph != null && session != null, "INVALID_IMPORT", "Graph evaluation, graph and source session are required.");
            Checks.Require(session.SourceSkin != null && session.SourceSkinBinding != null,
                "IMPORT_SOURCE_SKIN_MISSING", "A complete source skin session is required.");
            var deformNodes = graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.SkinDeform).ToArray();
            Checks.Require(deformNodes.Length > 0, "IMPORT_GRAPH_UNSUPPORTED", "Source skin display needs a SkinDeform node.");
            GraphPoseValue pose = null; string skeletonHash = null; string poseHash = null;
            var overrides = new Dictionary<string, GraphMeshValue>(System.StringComparer.Ordinal);
            foreach (var deform in deformNodes)
            {
                Checks.Require(evaluation.MeshInputs.TryGetValue(deform.NodeId, out var input) && input != null,
                    "INPUT_UNRESOLVED", "The authored SkinDeform mesh input is unavailable.");
                var skeletonEdge = graph.Edges.SingleOrDefault(edge => edge.ToNode == deform.NodeId && edge.ToPort == "skeleton");
                GraphSkeletonValue skeleton = null;
                Checks.Require(skeletonEdge != null && evaluation.SkeletonOutputs.TryGetValue(skeletonEdge.FromNode, out skeleton),
                    "INPUT_UNRESOLVED", "The authored SkinDeform skeleton input is unavailable.");
                var poseEdge = graph.Edges.SingleOrDefault(edge => edge.ToNode == deform.NodeId && edge.ToPort == "pose");
                GraphPoseValue candidatePose = null;
                Checks.Require(poseEdge != null && evaluation.PoseOutputs.TryGetValue(poseEdge.FromNode, out candidatePose),
                    "INPUT_UNRESOLVED", "The authored SkinDeform pose input is unavailable.");
                if (pose == null) { pose = candidatePose; skeletonHash = skeleton.Skeleton.ContentHash; poseHash = pose.Pose.ContentHash; }
                else Checks.Require(skeletonHash == skeleton.Skeleton.ContentHash && poseHash == candidatePose.Pose.ContentHash,
                    "IMPORT_GRAPH_UNSUPPORTED", "Multiple SkinDeform nodes must share one skeleton and pose for source skin display.");
                overrides[deform.NodeId] = Apply(input, session, graph, candidatePose.Pose);
            }
            // Replace each authored SkinDeform output, then run the ordinary graph evaluator
            // again. Downstream EditMesh/Morph/Material nodes therefore see the source-skinned
            // mesh in the same order as the authored graph and cannot be overwritten by a late
            // display correction.
            var projected = GraphEvaluator.Evaluate(graph, overrides, true);
            Checks.Require(projected.IsComplete, "IMPORT_GRAPH_UNSUPPORTED", "Source skin display could not evaluate the downstream graph: " + string.Join("; ", projected.Diagnostics.Select(item => item.NodeId + "=" + item.Code + ":" + item.Message)));
            return projected.Output;
        }

        public static GraphMeshValue Apply(GraphMeshValue input, ImportedRigSession session,
            AuthoringGraph graph, PoseSet authoredPose)
        {
            Checks.Require(session != null && session.SourceSkin != null && session.SourceSkinBinding != null,
                "IMPORT_SOURCE_SKIN_MISSING", "A complete source skin session is required.");
            var result = Apply(input, session.SourceSkin, session.SourceSkinBinding,
                SourceSkinPosePalette.Build(session, graph, authoredPose));
            if (session.MeshInstanceTransform == null) return result;
            // A selected node instance is outside the skin palette. Apply its
            // affine after skinning so authored edits stay in source-local space
            // and the displayed/exported result matches the original scene.
            return result.WithMesh(SourceMeshTransform.Apply(result.Mesh, session.MeshInstanceTransform).Mesh);
        }

        static GraphMeshValue Apply(GraphMeshValue input, ImportedRigSession session,
            AuthoringGraph graph, PoseSet authoredPose, GraphMeshValue appearance)
        {
            Checks.Require(session != null && session.SourceSkin != null && session.SourceSkinBinding != null,
                "IMPORT_SOURCE_SKIN_MISSING", "A complete source skin session is required.");
            return Apply(input, session.SourceSkin, session.SourceSkinBinding,
                SourceSkinPosePalette.Build(session, graph, authoredPose), appearance);
        }
    }
}
