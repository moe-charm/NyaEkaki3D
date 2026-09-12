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
            Checks.Require(deformNodes.Length == 1, "IMPORT_GRAPH_UNSUPPORTED", "Source skin display needs exactly one SkinDeform node.");
            var deform = deformNodes[0];
            Checks.Require(evaluation.MeshInputs.TryGetValue(deform.NodeId, out var input) && input != null,
                "INPUT_UNRESOLVED", "The authored SkinDeform mesh input is unavailable.");
            var poseEdge = graph.Edges.SingleOrDefault(edge => edge.ToNode == deform.NodeId && edge.ToPort == "pose");
            GraphPoseValue pose = null;
            Checks.Require(poseEdge != null && evaluation.PoseOutputs.TryGetValue(poseEdge.FromNode, out pose),
                "INPUT_UNRESOLVED", "The authored SkinDeform pose input is unavailable.");
            // Apply the source palette to the complete authored output.  This preserves
            // edits made after SkinDeform (EditMesh, morph and material graph stages).
            return Apply(evaluation.Output, session, graph, pose.Pose);
        }

        public static GraphMeshValue Apply(GraphMeshValue input, ImportedRigSession session,
            AuthoringGraph graph, PoseSet authoredPose)
        {
            Checks.Require(session != null && session.SourceSkin != null && session.SourceSkinBinding != null,
                "IMPORT_SOURCE_SKIN_MISSING", "A complete source skin session is required.");
            return Apply(input, session.SourceSkin, session.SourceSkinBinding,
                SourceSkinPosePalette.Build(session, graph, authoredPose));
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
