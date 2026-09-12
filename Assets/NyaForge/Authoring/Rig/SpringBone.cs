using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Settings for one simulated bone. Values are intentionally independent of Unity or a VRM runtime.</summary>
    public sealed class SpringBoneJointSettings
    {
        public string BoneId { get; }
        public float HitRadius { get; }
        public float Stiffness { get; }
        public float GravityPower { get; }
        public Vec3 GravityDirection { get; }
        public float DragForce { get; }

        public SpringBoneJointSettings(string boneId, float hitRadius, float stiffness, float gravityPower, Vec3 gravityDirection, float dragForce)
        {
            Checks.Id(boneId); Checks.Finite(hitRadius); Checks.Finite(stiffness); Checks.Finite(gravityPower); Checks.Finite(gravityDirection); Checks.Finite(dragForce);
            Checks.Require(hitRadius >= 0f && hitRadius <= 10f, "INVALID_SPRING", "Spring hit radius must be between 0 and 10.");
            Checks.Require(stiffness >= 0f && stiffness <= 1f, "INVALID_SPRING", "Spring stiffness must be between 0 and 1.");
            Checks.Require(gravityPower >= 0f && gravityPower <= 1000f, "INVALID_SPRING", "Spring gravity power must be between 0 and 1000.");
            Checks.Require(dragForce >= 0f && dragForce <= 1f, "INVALID_SPRING", "Spring drag force must be between 0 and 1.");
            Checks.Require(LengthSquared(gravityDirection) > 1e-12f || gravityPower == 0f, "INVALID_SPRING", "A nonzero gravity direction is required when gravity is enabled.");
            BoneId = boneId; HitRadius = hitRadius; Stiffness = stiffness; GravityPower = gravityPower; GravityDirection = NormalizeOrZero(gravityDirection); DragForce = dragForce;
        }

        internal void Write(BinaryWriter writer)
        {
            writer.Write(BoneId); writer.Write(Checks.Canonical(HitRadius)); writer.Write(Checks.Canonical(Stiffness)); writer.Write(Checks.Canonical(GravityPower));
            writer.Write(Checks.Canonical(GravityDirection.X)); writer.Write(Checks.Canonical(GravityDirection.Y)); writer.Write(Checks.Canonical(GravityDirection.Z)); writer.Write(Checks.Canonical(DragForce));
        }

        static float LengthSquared(Vec3 value) { return value.X * value.X + value.Y * value.Y + value.Z * value.Z; }
        internal static Vec3 NormalizeOrZero(Vec3 value)
        {
            float length = (float)Math.Sqrt(LengthSquared(value));
            return length <= 1e-6f ? new Vec3() : value * (1f / length);
        }
    }

    /// <summary>A spherical collider in avatar-rest space.</summary>
    public sealed class SpringBoneCollider
    {
        public Vec3 Center { get; }
        public float Radius { get; }

        public SpringBoneCollider(Vec3 center, float radius)
        {
            Checks.Finite(center); Checks.Finite(radius); Checks.Require(radius >= 0f && radius <= 10f, "INVALID_SPRING", "Spring collider radius must be between 0 and 10.");
            Center = center; Radius = radius;
        }
    }

    /// <summary>Named collider collection referenced by chain-local indices.</summary>
    public sealed class SpringBoneColliderGroup
    {
        public const int MaxColliders = 64;
        public string Name { get; }
        public IReadOnlyList<SpringBoneCollider> Colliders { get; }

        public SpringBoneColliderGroup(string name, IEnumerable<SpringBoneCollider> colliders)
        {
            Checks.Name(name); Checks.Require(colliders != null, "INVALID_SPRING", "Spring colliders are required.");
            var values = colliders.ToArray(); Checks.Require(values.Length <= MaxColliders, "BUDGET_EXCEEDED", "Spring collider group exceeds capacity.");
            foreach (var collider in values) Checks.Require(collider != null, "INVALID_SPRING", "Spring collider cannot be null.");
            Name = name; Colliders = Array.AsReadOnly(values);
        }
    }

    /// <summary>One or more ordered joints and the collider groups affecting them.</summary>
    public sealed class SpringBoneChain
    {
        public const int MaxJoints = 1024;
        public string Name { get; }
        public IReadOnlyList<SpringBoneJointSettings> Joints { get; }
        public IReadOnlyList<int> ColliderGroupIndices { get; }
        public string ContentHash { get; }

        public SpringBoneChain(string name, IEnumerable<SpringBoneJointSettings> joints, IEnumerable<int> colliderGroupIndices)
        {
            Checks.Name(name); Checks.Require(joints != null && colliderGroupIndices != null, "INVALID_SPRING", "Spring chain data is required.");
            var jointValues = joints.ToArray(); var groupValues = colliderGroupIndices.ToArray();
            Checks.Require(jointValues.Length > 0 && jointValues.Length <= MaxJoints, "BUDGET_EXCEEDED", "Spring chain exceeds joint capacity.");
            foreach (var joint in jointValues) Checks.Require(joint != null, "INVALID_SPRING", "Spring joint cannot be null.");
            foreach (var index in groupValues) Checks.Require(index >= 0, "INVALID_SPRING", "Spring collider group index cannot be negative.");
            Name = name; Joints = Array.AsReadOnly(jointValues); ColliderGroupIndices = Array.AsReadOnly(groupValues);
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8))
            {
                writer.Write(1); writer.Write(Name); writer.Write(Joints.Count); foreach (var joint in Joints) joint.Write(writer);
                writer.Write(ColliderGroupIndices.Count); foreach (var index in ColliderGroupIndices) writer.Write(index);
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }
    }

    /// <summary>Detached Verlet state. The dictionaries are copied at construction and never mutated by the simulator.</summary>
    public sealed class SpringBoneState
    {
        public string SkeletonHash { get; }
        public string ChainHash { get; }
        public IReadOnlyDictionary<string, Vec3> PreviousTails { get; }
        public IReadOnlyDictionary<string, Vec3> CurrentTails { get; }

        private SpringBoneState(string skeletonHash, string chainHash, IDictionary<string, Vec3> previous, IDictionary<string, Vec3> current)
        {
            Checks.HashText(skeletonHash); Checks.HashText(chainHash); SkeletonHash = skeletonHash; ChainHash = chainHash;
            PreviousTails = new ReadOnlyDictionary<string, Vec3>(new Dictionary<string, Vec3>(previous, StringComparer.Ordinal));
            CurrentTails = new ReadOnlyDictionary<string, Vec3>(new Dictionary<string, Vec3>(current, StringComparer.Ordinal));
        }

        internal static SpringBoneState Create(string skeletonHash, string chainHash, IDictionary<string, Vec3> previous, IDictionary<string, Vec3> current)
        {
            return new SpringBoneState(skeletonHash, chainHash, previous, current);
        }
    }

    public sealed class SpringBoneSimulationResult
    {
        public PoseSet Pose { get; }
        public SpringBoneState State { get; }
        internal SpringBoneSimulationResult(PoseSet pose, SpringBoneState state) { Pose = pose; State = state; }
    }

    /// <summary>Small deterministic spring preview for authoring tools. It has no Unity, VRM, or scene-state dependency.</summary>
    public static class SpringBoneSimulator
    {
        public const int MaxChains = 256;
        public const int MaxTotalJoints = 1024;
        public const float MaxDeltaTime = 0.25f;

        public static SpringBoneState CreateInitialState(SkeletonDefinition skeleton, PoseSet pose, IReadOnlyList<SpringBoneChain> chains)
        {
            return CreateInitialState(skeleton, pose, chains, null);
        }

        public static SpringBoneState CreateInitialState(SkeletonDefinition skeleton, PoseSet pose, IReadOnlyList<SpringBoneChain> chains, IReadOnlyList<SpringBoneColliderGroup> colliders)
        {
            var valid = SpringSimulationInputs.Validate(skeleton, pose, chains, colliders, null);
            var tails = new Dictionary<string, Vec3>(StringComparer.Ordinal);
            foreach (var joint in valid.Joints) tails.Add(joint.BoneId, PosedTail(skeleton.ById[joint.BoneId], pose.ByBoneId[joint.BoneId].Transform));
            return SpringBoneState.Create(skeleton.ContentHash, valid.ChainHash, tails, tails);
        }

        public static SpringBoneSimulationResult Step(SkeletonDefinition skeleton, PoseSet pose, IReadOnlyList<SpringBoneChain> chains, IReadOnlyList<SpringBoneColliderGroup> colliders, SpringBoneState previous, float deltaTime)
        {
            Checks.Finite(deltaTime); Checks.Require(deltaTime >= 0f && deltaTime <= MaxDeltaTime, "INVALID_DELTA_TIME", "Spring simulation delta time must be between 0 and 0.25 seconds.");
            var valid = SpringSimulationInputs.Validate(skeleton, pose, chains, colliders, previous);
            if (previous == null) return new SpringBoneSimulationResult(pose.ValidateFor(skeleton), CreateInitialState(skeleton, pose, chains, colliders));

            var nextTails = new Dictionary<string, Vec3>(StringComparer.Ordinal);
            var replacements = new Dictionary<string, BonePose>(StringComparer.Ordinal);
            var jointsById = valid.Joints.ToDictionary(joint => joint.BoneId, StringComparer.Ordinal);
            foreach (var bone in SpringPoseHierarchy.ParentFirst(skeleton))
            {
                var bonePose = SpringPoseHierarchy.Inherit(bone, pose, replacements);
                if (!jointsById.TryGetValue(bone.BoneId, out var joint)) { replacements.Add(bone.BoneId, bonePose); continue; }
                Vec3 head = PosedHead(bonePose.Transform), targetTail = PosedTail(bone, bonePose.Transform);
                Vec3 oldTail = previous.CurrentTails[joint.BoneId], olderTail = previous.PreviousTails[joint.BoneId];
                float length = Distance(targetTail, head);
                Vec3 velocity = (oldTail - olderTail) * (1f - joint.DragForce);
                Vec3 candidate = oldTail + velocity;
                float stiffness = Math.Min(1f, joint.Stiffness * deltaTime);
                candidate = candidate + (targetTail - candidate) * stiffness;
                candidate = candidate + joint.GravityDirection * (joint.GravityPower * deltaTime * deltaTime);
                candidate = ResolveColliders(head, candidate, joint.HitRadius, valid.CollidersByBone[joint.BoneId]);
                candidate = Constrain(head, candidate, length, targetTail - head);
                nextTails.Add(joint.BoneId, candidate);
                Vec3 currentDirection = targetTail - head, desiredDirection = candidate - head;
                Mat3 rotation = RotationBetween(currentDirection, desiredDirection);
                PoseTransform transformed = Multiply(rotation, bonePose.Transform);
                replacements.Add(joint.BoneId, new BonePose(joint.BoneId, transformed));
            }
            var outputPoses = pose.Poses.Select(item => replacements.TryGetValue(item.BoneId, out var replacement) ? replacement : item);
            var output = PoseSet.Create(skeleton, outputPoses);
            return new SpringBoneSimulationResult(output, SpringBoneState.Create(skeleton.ContentHash, valid.ChainHash, previous.CurrentTails.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal), nextTails));
        }

        private static Vec3 PosedHead(PoseTransform transform) { return transform.TransformPoint(new Vec3()); }
        private static Vec3 PosedTail(BoneDefinition bone, PoseTransform transform) { return transform.TransformPoint(bone.Tail - bone.Head); }
        private static float Distance(Vec3 a, Vec3 b) { return Length(a - b); }
        private static float Length(Vec3 value) { return (float)Math.Sqrt(value.X * value.X + value.Y * value.Y + value.Z * value.Z); }
        private static Vec3 Normalize(Vec3 value, Vec3 fallback) { float length = Length(value); return length <= 1e-6f ? fallback : value * (1f / length); }
        private static Vec3 Constrain(Vec3 head, Vec3 tail, float length, Vec3 fallbackDirection) { return head + Normalize(tail - head, Normalize(fallbackDirection, new Vec3(0, 1, 0))) * length; }

        private static Vec3 ResolveColliders(Vec3 head, Vec3 candidate, float hitRadius, IReadOnlyList<SpringBoneCollider> colliders)
        {
            Vec3 result = candidate;
            foreach (var collider in colliders)
                {
                    Vec3 delta = result - collider.Center; float distance = Length(delta), minimum = collider.Radius + hitRadius;
                    if (distance < minimum)
                    {
                        Vec3 fallback = Normalize(result - head, new Vec3(0, 1, 0));
                        result = collider.Center + Normalize(delta, fallback) * minimum;
                    }
                }
            return result;
        }

        private struct Mat3
        {
            internal Vec3 X, Y, Z;
            internal Mat3(Vec3 x, Vec3 y, Vec3 z) { X = x; Y = y; Z = z; }
            internal Vec3 Apply(Vec3 value) { return X * value.X + Y * value.Y + Z * value.Z; }
        }

        private static Mat3 RotationBetween(Vec3 from, Vec3 to)
        {
            Vec3 a = Normalize(from, new Vec3(0, 1, 0)), b = Normalize(to, a); float dot = Math.Max(-1f, Math.Min(1f, Dot(a, b))); Vec3 axis = Cross(a, b); float axisLength = Length(axis);
            if (axisLength <= 1e-6f)
            {
                if (dot > 0f) return IdentityMatrix();
                Vec3 reference = Math.Abs(a.X) < Math.Abs(a.Y) && Math.Abs(a.X) < Math.Abs(a.Z) ? new Vec3(1, 0, 0) : Math.Abs(a.Y) < Math.Abs(a.Z) ? new Vec3(0, 1, 0) : new Vec3(0, 0, 1);
                axis = Normalize(Cross(a, reference), new Vec3(1, 0, 0)); return AxisAngle(axis, (float)Math.PI);
            }
            axis = axis * (1f / axisLength); return AxisAngle(axis, (float)Math.Atan2(axisLength, dot));
        }

        private static Mat3 AxisAngle(Vec3 axis, float angle)
        {
            float c = (float)Math.Cos(angle), s = (float)Math.Sin(angle), t = 1f - c, x = axis.X, y = axis.Y, z = axis.Z;
            return new Mat3(new Vec3(t * x * x + c, t * x * y + s * z, t * x * z - s * y), new Vec3(t * x * y - s * z, t * y * y + c, t * y * z + s * x), new Vec3(t * x * z + s * y, t * y * z - s * x, t * z * z + c));
        }

        private static Mat3 IdentityMatrix() { return new Mat3(new Vec3(1, 0, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1)); }
        private static Mat3 ComposeBasis(Mat3 left, PoseTransform right) { return new Mat3(left.Apply(right.XAxis), left.Apply(right.YAxis), left.Apply(right.ZAxis)); }
        private static PoseTransform Multiply(Mat3 left, PoseTransform right) { var matrix = ComposeBasis(left, right); return new PoseTransform(matrix.X, matrix.Y, matrix.Z, right.Translation); }
        private static float Dot(Vec3 a, Vec3 b) { return a.X * b.X + a.Y * b.Y + a.Z * b.Z; }
        private static Vec3 Cross(Vec3 a, Vec3 b) { return new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }
    }
}
