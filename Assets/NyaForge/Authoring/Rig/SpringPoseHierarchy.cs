using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Evaluates absolute poses parent-first while preserving the input's relative transforms.</summary>
    internal static class SpringPoseHierarchy
    {
        internal static IEnumerable<BoneDefinition> ParentFirst(SkeletonDefinition skeleton)
        {
            var visited = new HashSet<string>(StringComparer.Ordinal);
            var ordered = new List<BoneDefinition>();
            foreach (var bone in skeleton.Bones.OrderBy(b => b.BoneId, StringComparer.Ordinal))
                Visit(bone, skeleton, visited, ordered);
            return ordered;
        }

        static void Visit(BoneDefinition bone, SkeletonDefinition skeleton, ISet<string> visited, IList<BoneDefinition> ordered)
        {
            if (!visited.Add(bone.BoneId)) return;
            if (bone.ParentBoneId != "") Visit(skeleton.ById[bone.ParentBoneId], skeleton, visited, ordered);
            ordered.Add(bone);
        }

        internal static BonePose Inherit(BoneDefinition bone, PoseSet input, IReadOnlyDictionary<string, BonePose> evaluated)
        {
            var original = input.ByBoneId[bone.BoneId];
            if (bone.ParentBoneId == "") return original;
            var parent = input.ByBoneId[bone.ParentBoneId];
            var updated = evaluated[bone.ParentBoneId];
            if (ReferenceEquals(parent, updated)) return original;
            var before = parent.Transform; var after = updated.Transform; var child = original.Transform;
            // after * inverse(before) * child, including authored head offsets.
            var origin = after.TransformPoint(before.InverseTransformPoint(child.Translation));
            var x = TransferVector(before, after, child.XAxis);
            var y = TransferVector(before, after, child.YAxis);
            var z = TransferVector(before, after, child.ZAxis);
            return new BonePose(bone.BoneId, new PoseTransform(x, y, z, origin));
        }

        static Vec3 TransferVector(PoseTransform before, PoseTransform after, Vec3 vector)
        {
            var local = before.InverseTransformPoint(before.Translation + vector);
            return after.XAxis * local.X + after.YAxis * local.Y + after.ZAxis * local.Z;
        }
    }
}
