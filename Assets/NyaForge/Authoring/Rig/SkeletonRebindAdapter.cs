using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Rig
{
    /// <summary>
    /// Explicitly transfers a skin binding or pose from one rest skeleton to
    /// another.  Bone names and array positions are intentionally ignored:
    /// callers must provide a complete, one-to-one stable BoneId map.
    /// </summary>
    public static class SkeletonRebindAdapter
    {
        /// <summary>Maximum rest-frame discrepancy accepted for a mapped bone.</summary>
        public const float RestPositionTolerance = 0.0001f;

        public static IReadOnlyDictionary<string, string> ValidateMap(
            SkeletonDefinition source, SkeletonDefinition target,
            IReadOnlyDictionary<string, string> sourceToTarget)
        {
            Checks.Require(source != null && target != null, "INVALID_SKELETON", "Source and target skeletons are required.");
            Checks.Require(sourceToTarget != null, "SKELETON_REBIND_REQUIRED", "An explicit source-to-target bone map is required.");
            Checks.Require(sourceToTarget.Count == source.Bones.Count, "SKELETON_REBIND_INCOMPLETE", "The bone map must cover every source bone exactly once.");

            var values = new HashSet<string>(StringComparer.Ordinal);
            var copy = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var bone in source.Bones)
            {
                Checks.Require(sourceToTarget.TryGetValue(bone.BoneId, out var targetId),
                    "SKELETON_REBIND_INCOMPLETE", "The bone map is missing a source BoneId.");
                Checks.Id(targetId);
                Checks.Require(target.ById.ContainsKey(targetId), "SKELETON_REBIND_TARGET_BONE", "The mapped target BoneId does not exist.");
                Checks.Require(values.Add(targetId), "SKELETON_REBIND_AMBIGUOUS", "Two source bones map to the same target BoneId.");
                copy.Add(bone.BoneId, targetId);
            }
            Checks.Require(sourceToTarget.Keys.All(source.ById.ContainsKey),
                "SKELETON_REBIND_SOURCE_BONE", "The bone map contains an unknown source BoneId.");

            foreach (var bone in source.Bones)
            {
                var targetBone = target.ById[copy[bone.BoneId]];
                var expectedParent = bone.ParentBoneId == "" ? "" : copy[bone.ParentBoneId];
                Checks.Require(targetBone.ParentBoneId == expectedParent,
                    "SKELETON_REBIND_HIERARCHY", "Mapped bones must preserve the parent hierarchy.");
                Checks.Require(Near(bone.Head, targetBone.Head) && Near(bone.Tail, targetBone.Tail),
                    "SKELETON_REBIND_REST", "Mapped bones must share the same rest-space head and tail.");
            }
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(copy);
        }

        public static SkinBinding RebindBinding(SkinBinding binding, MeshData mesh,
            SkeletonDefinition source, SkeletonDefinition target,
            IReadOnlyDictionary<string, string> sourceToTarget)
        {
            Checks.Require(binding != null && mesh != null, "INVALID_SKIN", "Binding and mesh are required.");
            var map = ValidateMap(source, target, sourceToTarget);
            binding.ValidateFor(mesh, source);
            var raw = binding.Weights.OrderBy(pair => pair.Key)
                .SelectMany(pair => pair.Value.Select(weight =>
                    new SkinBinding.VertexWeightInput(pair.Key, map[weight.BoneId], weight.Weight)));
            return SkinBinding.Create(mesh, target, raw);
        }

        public static PoseSet RebindPose(PoseSet pose, SkeletonDefinition source,
            SkeletonDefinition target, IReadOnlyDictionary<string, string> sourceToTarget)
        {
            Checks.Require(pose != null, "INVALID_POSE", "Pose is required.");
            var map = ValidateMap(source, target, sourceToTarget);
            pose.ValidateFor(source);
            var mapped = pose.Poses.Select(item => new BonePose(map[item.BoneId], item.Transform)).ToList();
            var mappedTargets = new HashSet<string>(mapped.Select(item => item.BoneId), StringComparer.Ordinal);
            foreach (var bone in target.Bones)
                if (!mappedTargets.Contains(bone.BoneId))
                    mapped.Add(new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head)));
            return PoseSet.Create(target, mapped);
        }

        static bool Near(Vec3 a, Vec3 b)
        {
            return Math.Abs(a.X - b.X) <= RestPositionTolerance
                && Math.Abs(a.Y - b.Y) <= RestPositionTolerance
                && Math.Abs(a.Z - b.Z) <= RestPositionTolerance;
        }
    }
}
