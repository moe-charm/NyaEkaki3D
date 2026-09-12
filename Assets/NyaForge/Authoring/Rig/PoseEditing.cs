using System;
using System.Linq;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Small deterministic pose edits shared by GUI and future command adapters.</summary>
    public static class PoseEditing
    {
        public static PoseSet Rebind(PoseSet pose, SkeletonDefinition skeleton)
        {
            Checks.Require(pose != null && skeleton != null, "INVALID_POSE", "Pose and skeleton are required.");
            return PoseSet.Create(skeleton, pose.Poses);
        }

        public static PoseSet SetRotationZ(PoseSet pose, SkeletonDefinition skeleton, string boneId, float degrees)
        {
            Checks.Require(pose != null && skeleton != null, "INVALID_POSE", "Pose and skeleton are required.");
            Checks.Id(boneId); Checks.Finite(degrees); Checks.Require(skeleton.ById.ContainsKey(boneId), "POSE_BONE_UNKNOWN", "Pose bone is not in the skeleton.");
            var valid = pose.ValidateFor(skeleton);
            var changed = valid.Poses.Select(item => item.BoneId == boneId ? new BonePose(item.BoneId, PoseTransform.RotationZ(degrees, item.Transform.Translation)) : item);
            return PoseSet.Create(skeleton, changed);
        }

        public static PoseSet SetRotationEuler(PoseSet pose, SkeletonDefinition skeleton, string boneId, float xDegrees, float yDegrees, float zDegrees)
        {
            Checks.Require(pose != null && skeleton != null, "INVALID_POSE", "Pose and skeleton are required.");
            Checks.Id(boneId); Checks.Finite(xDegrees); Checks.Finite(yDegrees); Checks.Finite(zDegrees);
            Checks.Require(skeleton.ById.ContainsKey(boneId), "POSE_BONE_UNKNOWN", "Pose bone is not in the skeleton.");
            var valid = pose.ValidateFor(skeleton);
            var changed = valid.Poses.Select(item => item.BoneId == boneId
                ? new BonePose(item.BoneId, PoseTransform.RotationEuler(xDegrees, yDegrees, zDegrees, item.Transform.Translation))
                : item);
            return PoseSet.Create(skeleton, changed);
        }
    }
}
