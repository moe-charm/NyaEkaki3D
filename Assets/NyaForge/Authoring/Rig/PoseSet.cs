using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Complete deterministic pose for one skeleton identity.</summary>
    public sealed class PoseSet
    {
        public string SkeletonHash { get; }
        public IReadOnlyList<BonePose> Poses { get; }
        public IReadOnlyDictionary<string, BonePose> ByBoneId { get; }
        public string ContentHash { get; }

        private PoseSet(string skeletonHash, IReadOnlyList<BonePose> poses)
        {
            Checks.HashText(skeletonHash); SkeletonHash = skeletonHash; Poses = poses;
            ByBoneId = new ReadOnlyDictionary<string, BonePose>(poses.ToDictionary(p => p.BoneId, StringComparer.Ordinal));
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(1); writer.Write(skeletonHash); writer.Write(poses.Count);
                foreach (var pose in poses.OrderBy(p => p.BoneId, StringComparer.Ordinal))
                {
                    writer.Write(pose.BoneId); Write(writer, pose.Transform);
                }
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        public static PoseSet Create(SkeletonDefinition skeleton, IEnumerable<BonePose> poses)
        {
            Checks.Require(skeleton != null && poses != null, "INVALID_POSE", "Skeleton and poses are required.");
            var items = poses.ToArray(); ValidateUnique(items);
            Checks.Require(items.Length == skeleton.Bones.Count, "POSE_BONE_MISSING", "Pose must contain every skeleton bone.");
            foreach (var bone in skeleton.Bones) Checks.Require(items.Any(p => p.BoneId == bone.BoneId), "POSE_BONE_MISSING", "Pose is missing a skeleton bone.");
            foreach (var pose in items) Checks.Require(skeleton.ById.ContainsKey(pose.BoneId), "POSE_BONE_UNKNOWN", "Pose contains a bone outside the skeleton.");
            return new PoseSet(skeleton.ContentHash, Array.AsReadOnly(items.OrderBy(p => p.BoneId, StringComparer.Ordinal).ToArray()));
        }

        internal static PoseSet FromSerialized(string skeletonHash, IEnumerable<BonePose> poses)
        {
            Checks.HashText(skeletonHash); Checks.Require(poses != null, "INVALID_POSE", "Serialized poses are required.");
            var items = poses.ToArray(); ValidateUnique(items);
            Checks.Require(items.Length > 0 && items.Length <= SkeletonDefinition.MaxBones, "BUDGET_EXCEEDED", "Pose count exceeds capacity.");
            return new PoseSet(skeletonHash, Array.AsReadOnly(items.OrderBy(p => p.BoneId, StringComparer.Ordinal).ToArray()));
        }

        public PoseSet ValidateFor(SkeletonDefinition skeleton)
        {
            Checks.Require(skeleton != null, "INVALID_POSE", "Skeleton is required.");
            Checks.Require(SkeletonHash == skeleton.ContentHash, "POSE_SKELETON_CHANGED", "Pose belongs to another skeleton.");
            return Create(skeleton, Poses);
        }

        static void ValidateUnique(BonePose[] items)
        {
            Checks.Require(items.Length > 0 && items.Length <= SkeletonDefinition.MaxBones, "BUDGET_EXCEEDED", "Pose count exceeds capacity.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pose in items) Checks.Require(pose != null && ids.Add(pose.BoneId), "DUPLICATE_POSE", "Pose must contain each bone identity once.");
        }

        internal static void Write(BinaryWriter writer, PoseTransform transform)
        {
            Write(writer, transform.XAxis); Write(writer, transform.YAxis); Write(writer, transform.ZAxis); Write(writer, transform.Translation);
        }
        internal static PoseTransform Read(BinaryReader reader)
        {
            return new PoseTransform(ReadVector(reader), ReadVector(reader), ReadVector(reader), ReadVector(reader));
        }
        static void Write(BinaryWriter writer, Vec3 value) { writer.Write(Checks.Canonical(value.X)); writer.Write(Checks.Canonical(value.Y)); writer.Write(Checks.Canonical(value.Z)); }
        static Vec3 ReadVector(BinaryReader reader) { return new Vec3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); }
    }
}
