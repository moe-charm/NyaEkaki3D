using System;
using System.Linq;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Pure rest-skeleton edits. Changing a bone deliberately changes the skeleton identity.</summary>
    public static class SkeletonEditing
    {
        public static SkeletonDefinition MoveBone(SkeletonDefinition skeleton, string boneId, Vec3 headDelta, Vec3 tailDelta)
        {
            Checks.Require(skeleton != null, "INVALID_SKELETON", "Skeleton is required."); Checks.Id(boneId); Checks.Finite(headDelta); Checks.Finite(tailDelta);
            Checks.Require(skeleton.ById.ContainsKey(boneId), "BONE_NOT_FOUND", "Bone is not in the skeleton.");
            var moved = skeleton.Bones.Select(bone => bone.BoneId == boneId ? new BoneDefinition(bone.BoneId, bone.Name, bone.ParentBoneId, bone.Head + headDelta, bone.Tail + tailDelta) : bone);
            return new SkeletonDefinition(moved);
        }
    }
}
