using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    private const string RootBone = "00000000-0000-0000-0000-000000000001";
    private const string ChildBone = "00000000-0000-0000-0000-000000000002";

    static void RunSpringBoneTests()
    {
        RunSpringPoseTests();
        RunSpringColliderScopeTests();
        RunSpringConstraintTests();
        Test("SpringBone first step initializes a detached rest state", () =>
        {
            var fixture = SpringFixture(); var initial = SpringBoneSimulator.CreateInitialState(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain });
            var result = SpringBoneSimulator.Step(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain }, Array.Empty<SpringBoneColliderGroup>(), initial, 0f);
            Equal(fixture.Pose.ContentHash, result.Pose.ContentHash); Equal(fixture.Skeleton.ContentHash, result.State.SkeletonHash); Equal(initial.CurrentTails[ChildBone].Y, result.State.PreviousTails[ChildBone].Y);
        });

        Test("SpringBone gravity rotates the joint while preserving length", () =>
        {
            var fixture = SpringFixture(gravityPower: 8f, gravityDirection: new Vec3(1, 0, 0)); var initial = SpringBoneSimulator.CreateInitialState(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain });
            var result = SpringBoneSimulator.Step(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain }, Array.Empty<SpringBoneColliderGroup>(), initial, .1f);
            Vec3 head = result.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3()); Vec3 tail = result.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3(0, 1, 0));
            Near(1f, Distance(head, tail)); True(tail.X > head.X); True(result.State.CurrentTails[ChildBone].X > 0f); False(result.Pose.ContentHash == fixture.Pose.ContentHash);
        });

        Test("SpringBone rejects invalid time, duplicate joints, and missing bones", () =>
        {
            var fixture = SpringFixture(); var initial = SpringBoneSimulator.CreateInitialState(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain });
            Expect("INVALID_DELTA_TIME", () => SpringBoneSimulator.Step(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain }, Array.Empty<SpringBoneColliderGroup>(), initial, .3f));
            var duplicate = new SpringBoneChain("duplicate", new[] { fixture.Joint, fixture.Joint }, Array.Empty<int>());
            Expect("DUPLICATE_SPRING_JOINT", () => SpringBoneSimulator.CreateInitialState(fixture.Skeleton, fixture.Pose, new[] { duplicate }));
            var unknown = new SpringBoneChain("unknown", new[] { new SpringBoneJointSettings("00000000-0000-0000-0000-000000000099", 0, 0, 0, new Vec3(), 0) }, Array.Empty<int>());
            Expect("SPRING_BONE_UNKNOWN", () => SpringBoneSimulator.CreateInitialState(fixture.Skeleton, fixture.Pose, new[] { unknown }));
        });

        Test("SpringBone state is copied and repeated simulation is deterministic", () =>
        {
            var fixture = SpringFixture(gravityPower: 4f, gravityDirection: new Vec3(1, 0, 0)); var initial = SpringBoneSimulator.CreateInitialState(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain });
            var first = SpringBoneSimulator.Step(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain }, Array.Empty<SpringBoneColliderGroup>(), initial, .1f);
            var second = SpringBoneSimulator.Step(fixture.Skeleton, fixture.Pose, new[] { fixture.Chain }, Array.Empty<SpringBoneColliderGroup>(), initial, .1f);
            Equal(first.Pose.ContentHash, second.Pose.ContentHash); Equal(first.State.CurrentTails[ChildBone].X, second.State.CurrentTails[ChildBone].X); False(object.ReferenceEquals(initial.CurrentTails, first.State.CurrentTails));
        });
    }

    private sealed class SpringFixtureData
    {
        internal SkeletonDefinition Skeleton; internal PoseSet Pose; internal SpringBoneChain Chain; internal SpringBoneJointSettings Joint;
    }

    private static SpringFixtureData SpringFixture(float gravityPower = 0f, Vec3 gravityDirection = default(Vec3), float hitRadius = 0f)
    {
        var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(RootBone, "Root", "", new Vec3(0, 0, 0), new Vec3(0, 1, 0)), new BoneDefinition(ChildBone, "Child", RootBone, new Vec3(0, 1, 0), new Vec3(0, 2, 0)) });
        var pose = PoseSet.Create(skeleton, new[] { new BonePose(RootBone, PoseTransform.Identity), new BonePose(ChildBone, PoseTransform.FromTranslation(new Vec3(0, 1, 0))) });
        var joint = new SpringBoneJointSettings(ChildBone, hitRadius, 0f, gravityPower, gravityDirection, 0f);
        return new SpringFixtureData { Skeleton = skeleton, Pose = pose, Joint = joint, Chain = new SpringBoneChain("tail", new[] { joint }, Array.Empty<int>()) };
    }

    private static float Distance(Vec3 a, Vec3 b) { float x = a.X - b.X, y = a.Y - b.Y, z = a.Z - b.Z; return (float)Math.Sqrt(x * x + y * y + z * z); }
}
