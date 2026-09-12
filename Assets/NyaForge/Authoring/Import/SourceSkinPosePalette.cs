using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Builds source-joint world frames from a validated authored pose.</summary>
    /// <remarks>
    /// A PoseTransform is authored around the BoneDefinition head. The source rest frame is
    /// anchored by composing P × T(-head) × sourceWorld, so the imported initial pose cancels
    /// exactly while arbitrary source joint bases remain intact.
    /// </remarks>
    public static class SourceSkinPosePalette
    {
        public static IReadOnlyList<SourceAffine> Build(ImportedRigSession session, AuthoringGraph graph, PoseSet authoredPose)
        {
            Checks.Require(session != null && graph != null && authoredPose != null, "INVALID_POSE", "Session, graph and authored pose are required.");
            var skin = session.SourceSkin;
            Checks.Require(skin != null, "IMPORT_SOURCE_SKIN_MISSING", "A complete source skin is required for source pose projection.");
            var mapping = session.Resolve(graph);
            var skeleton = graph.Nodes[session.SkeletonNodeId].Skeleton;
            var pose = authoredPose.ValidateFor(skeleton);
            var result = new SourceAffine[skin.Joints.Count];
            for (int slot = 0; slot < skin.Joints.Count; slot++)
            {
                int sourceNode = skin.Joints[slot];
                string boneId = mapping.Resolve(sourceNode);
                var bone = skeleton.ById[boneId];
                var authored = ToAffine(pose.ByBoneId[boneId].Transform);
                var anchor = SourceAffine.FromTrs(new Vec3(-bone.Head.X, -bone.Head.Y, -bone.Head.Z), new Vec4(0, 0, 0, 1), new Vec3(1, 1, 1));
                result[slot] = authored.Compose(anchor).Compose(skin.Nodes.World[sourceNode]);
            }
            return Array.AsReadOnly(result);
        }

        static SourceAffine ToAffine(PoseTransform pose)
        {
            return new SourceAffine(new[] {
                (double)pose.XAxis.X, pose.XAxis.Y, pose.XAxis.Z, 0,
                (double)pose.YAxis.X, pose.YAxis.Y, pose.YAxis.Z, 0,
                (double)pose.ZAxis.X, pose.ZAxis.Y, pose.ZAxis.Z, 0,
                (double)pose.Translation.X, pose.Translation.Y, pose.Translation.Z, 1 });
        }
    }
}
