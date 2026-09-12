using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Rig
{
    /// <summary>One rest-pose bone. Coordinates are in the owning mesh's avatar-rest space.</summary>
    public sealed class BoneDefinition
    {
        public string BoneId { get; }
        public string Name { get; }
        public string ParentBoneId { get; }
        public Vec3 Head { get; }
        public Vec3 Tail { get; }

        public BoneDefinition(string boneId, string name, string parentBoneId, Vec3 head, Vec3 tail)
        {
            Checks.Id(boneId); Checks.Name(name); Checks.Finite(head); Checks.Finite(tail);
            Checks.Require(parentBoneId == "" || Guid.TryParseExact(parentBoneId, "D", out _), "INVALID_BONE", "Parent bone identity must be empty or a canonical UUID.");
            double dx = tail.X - head.X, dy = tail.Y - head.Y, dz = tail.Z - head.Z;
            Checks.Require(dx * dx + dy * dy + dz * dz > 1e-12, "INVALID_BONE", "Bone head and tail must be distinct.");
            BoneId = boneId; Name = name; ParentBoneId = parentBoneId ?? ""; Head = head; Tail = tail;
        }
    }

    /// <summary>Immutable skeleton rest pose with stable IDs and deterministic content identity.</summary>
    public sealed class SkeletonDefinition
    {
        public const int MaxBones = 512;
        public IReadOnlyList<BoneDefinition> Bones { get; }
        public IReadOnlyDictionary<string, BoneDefinition> ById { get; }
        public string ContentHash { get; }

        public SkeletonDefinition(IEnumerable<BoneDefinition> bones)
        {
            Checks.Require(bones != null, "INVALID_SKELETON", "Skeleton bones are required.");
            var items = bones.ToArray();
            Checks.Require(items.Length > 0 && items.Length <= MaxBones, "BUDGET_EXCEEDED", "Skeleton must contain 1 to 512 bones.");
            var byId = new Dictionary<string, BoneDefinition>(StringComparer.Ordinal);
            foreach (var bone in items) Checks.Require(bone != null && byId.TryAdd(bone.BoneId, bone), "DUPLICATE_BONE", "Bone identity must be unique.");
            foreach (var bone in items)
                Checks.Require(bone.ParentBoneId == "" || byId.ContainsKey(bone.ParentBoneId), "BONE_PARENT_MISSING", "Bone parent does not exist.");
            foreach (var bone in items) Visit(bone.BoneId, byId, new HashSet<string>(StringComparer.Ordinal), new HashSet<string>(StringComparer.Ordinal));
            Bones = Array.AsReadOnly(items);
            ById = new ReadOnlyDictionary<string, BoneDefinition>(byId);
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(1); writer.Write(items.Length);
                foreach (var bone in items.OrderBy(b => b.BoneId, StringComparer.Ordinal))
                {
                    writer.Write(bone.BoneId); writer.Write(bone.Name); writer.Write(bone.ParentBoneId);
                    Write(writer, bone.Head); Write(writer, bone.Tail);
                }
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        static void Visit(string id, IReadOnlyDictionary<string, BoneDefinition> byId, ISet<string> active, ISet<string> complete)
        {
            if (complete.Contains(id)) return;
            Checks.Require(active.Add(id), "BONE_CYCLE", "Skeleton parent hierarchy contains a cycle.");
            var parent = byId[id].ParentBoneId;
            if (parent != "") Visit(parent, byId, active, complete);
            active.Remove(id); complete.Add(id);
        }

        static void Write(BinaryWriter writer, Vec3 value) { writer.Write(Checks.Canonical(value.X)); writer.Write(Checks.Canonical(value.Y)); writer.Write(Checks.Canonical(value.Z)); }
    }
}
