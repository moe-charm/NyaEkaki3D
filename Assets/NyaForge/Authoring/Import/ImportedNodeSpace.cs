using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Transforms source-local points for the translation-only import profile into posed avatar space.</summary>
    public sealed class ImportedNodeSpace
    {
        readonly ImportedRigSession session;
        readonly ImportedBoneMap mapping;
        readonly SkeletonDefinition skeleton;
        readonly PoseSet pose;

        public ImportedNodeSpace(ImportedRigSession session, AuthoringGraph graph, PoseSet pose)
        {
            Checks.Require(session != null, "INVALID_IMPORT", "Imported rig session is required.");
            mapping = session.Resolve(graph);
            Checks.Require(session.SourceNodeOrigins != null, "IMPORT_NODE_SPACE_MISSING", "Source node origins were not retained; reimport the source model.");
            skeleton = graph.Nodes[session.SkeletonNodeId].Skeleton;
            Checks.Require(pose != null, "INVALID_POSE", "Node conversion requires a pose.");
            this.pose = pose.ValidateFor(skeleton); this.session = session;
        }

        public Vec3 TransformPoint(int sourceNode, Vec3 sourceLocalPoint)
        {
            Checks.Finite(sourceLocalPoint);
            var id = mapping.Resolve(sourceNode);
            // Bone heads can come from inverse bind matrices, not from source node transforms.
            var restPoint = session.SourceNodeOrigins[sourceNode] + sourceLocalPoint;
            return pose.ByBoneId[id].Transform.TransformPoint(restPoint - skeleton.ById[id].Head);
        }
    }
}
