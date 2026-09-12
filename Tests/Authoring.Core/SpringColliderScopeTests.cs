using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSpringColliderScopeTests()
    {
        Test("Spring chain colliders do not leak into unreferenced independent bones", () =>
        {
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(RootBone, "A", "", new Vec3(), new Vec3(0, 1, 0)),
                new BoneDefinition(ChildBone, "B", "", new Vec3(), new Vec3(0, 1, 0)) });
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(RootBone, PoseTransform.Identity), new BonePose(ChildBone, PoseTransform.Identity) });
            var a = new SpringBoneJointSettings(RootBone, 0, 0, 0, new Vec3(), 0);
            var b = new SpringBoneJointSettings(ChildBone, 0, 0, 0, new Vec3(), 0);
            var group = new SpringBoneColliderGroup("body", new[] { new SpringBoneCollider(new Vec3(.1f, 1, 0), .4f) });
            var groups = new[] { group };
            SpringBoneSimulationResult Run(bool includeA, bool share, SpringBoneColliderGroup[] colliders)
            {
                var chainB = new SpringBoneChain("B", new[] { b }, share ? new[] { 0 } : Array.Empty<int>());
                var chains = includeA ? new[] { new SpringBoneChain("A", new[] { a }, new[] { 0 }), chainB } : new[] { chainB };
                var initial = SpringBoneSimulator.CreateInitialState(skeleton, pose, chains, colliders);
                return SpringBoneSimulator.Step(skeleton, pose, chains, colliders, initial, .1f);
            }
            var baseline = Run(false, false, groups);
            var isolated = Run(true, false, groups);
            SpringPointNear(baseline.State.CurrentTails[ChildBone], isolated.State.CurrentTails[ChildBone]);
            True(Distance(isolated.State.CurrentTails[RootBone], new Vec3(0, 1, 0)) > .1f);
            var changed = Run(true, false, new[] { new SpringBoneColliderGroup("changed", new[] { new SpringBoneCollider(new Vec3(-.1f, 1, 0), .5f) }) });
            SpringPointNear(baseline.State.CurrentTails[ChildBone], changed.State.CurrentTails[ChildBone]);
            var shared = Run(true, true, groups);
            SpringPointNear(shared.State.CurrentTails[RootBone], shared.State.CurrentTails[ChildBone]);
            True(Distance(shared.State.CurrentTails[ChildBone], baseline.State.CurrentTails[ChildBone]) > .1f);
        });

        Test("Spring validates null collider groups before publishing state", () =>
        {
            var f = SpringFixture();
            var chain = new SpringBoneChain("referenced", new[] { f.Joint }, new[] { 0 });
            var groups = new SpringBoneColliderGroup[] { null };
            Expect("INVALID_SPRING", () => SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, new[] { chain }, groups));
            Expect("INVALID_SPRING", () => SpringBoneSimulator.Step(f.Skeleton, f.Pose, new[] { chain }, groups, null, .1f));
            Expect("INVALID_SPRING", () => SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, new[] { f.Chain }, groups));
        });
    }
}
