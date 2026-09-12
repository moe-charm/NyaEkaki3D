using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSpringCenterTests()
    {
        Test("Spring centers transport both histories independently and preserve elapsed time", () =>
        {
            var f = SpringFixture(8, new Vec3(1, 0, 0));
            var rootChain = new SpringBoneChain("root", new[] { new SpringBoneJointSettings(RootBone, 0, 0, 0, new Vec3(), 0) }, Array.Empty<int>());
            var chains = new[] { rootChain, f.Chain };
            var state = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, chains), .02f).State;
            var inputs = new Dictionary<string, PoseTransform> { [RootBone] = PoseTransform.Identity, [ChildBone] = PoseTransform.FromTranslation(new Vec3(1, 0, 0)) };
            var previous = new SpringCenterFrame(state, inputs);
            inputs[ChildBone] = PoseTransform.Identity; // Detached snapshot must retain the original center.
            var current = new SpringCenterFrame(state, new Dictionary<string, PoseTransform> {
                [RootBone] = PoseTransform.Identity,
                [ChildBone] = new PoseTransform(new Vec3(0, 2, 0), new Vec3(-2, 0, 0), new Vec3(0, 0, 2), new Vec3(10, 20, 30)) });
            var moved = SpringCenterMotion.Transfer(state, previous, current);
            Vec3 Expected(Vec3 p) => new Vec3(10 - 2 * p.Y, 20 + 2 * (p.X - 1), 30 + 2 * p.Z);
            SpringPointNear(Expected(state.CurrentTails[ChildBone]), moved.CurrentTails[ChildBone]);
            SpringPointNear(Expected(state.PreviousTails[ChildBone]), moved.PreviousTails[ChildBone]);
            SpringPointNear(state.CurrentTails[RootBone], moved.CurrentTails[RootBone]);
            SpringPointNear(state.PreviousTails[RootBone], moved.PreviousTails[RootBone]);
            Near(.02f, moved.PreviousDeltaTime); Equal(state.ChainHash, moved.ChainHash);
            var restored = SpringCenterMotion.Transfer(moved, current, previous);
            SpringPointNear(state.CurrentTails[ChildBone], restored.CurrentTails[ChildBone]);
            True(ReferenceEquals(state, SpringCenterMotion.Transfer(state, previous, previous)));
        });
        Test("Spring center movement and pause resume preserve local simulation", () =>
        {
            var f = SpringFixture(8, new Vec3(1, 0, 0)); var chains = new[] { f.Chain };
            var state = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, chains), .02f).State;
            var offset = new Vec3(3, 2, 1);
            var before = new SpringCenterFrame(state, new Dictionary<string, PoseTransform> { [ChildBone] = PoseTransform.Identity });
            var after = new SpringCenterFrame(state, new Dictionary<string, PoseTransform> { [ChildBone] = PoseTransform.FromTranslation(offset) });
            var moved = SpringCenterMotion.Transfer(state, before, after);
            var pose = PoseSet.Create(f.Skeleton, f.Pose.Poses.Select(b => new BonePose(b.BoneId, new PoseTransform(b.Transform.XAxis, b.Transform.YAxis, b.Transform.ZAxis, b.Transform.Translation + offset))));
            var paused = SpringBoneSimulator.Step(f.Skeleton, pose, chains, null, moved, 0);
            True(ReferenceEquals(moved, paused.State));
            var actual = SpringBoneSimulator.Step(f.Skeleton, pose, chains, null, paused.State, .01f);
            var expected = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, state, .01f);
            SpringPointNear(expected.State.CurrentTails[ChildBone] + offset, actual.State.CurrentTails[ChildBone]);
            SpringPointNear(actual.State.CurrentTails[ChildBone], actual.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3(0, 1, 0)));
        });
        Test("Spring center frames reject incomplete stale and singular inputs", () =>
        {
            var f = SpringFixture(0, new Vec3()); var state = SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, new[] { f.Chain });
            Expect("INVALID_SPRING_CENTER", () => new SpringCenterFrame(state, new Dictionary<string, PoseTransform>()));
            Expect("INVALID_SPRING_CENTER", () => new SpringCenterFrame(state, new Dictionary<string, PoseTransform> { [RootBone] = PoseTransform.Identity }));
            Expect("INVALID_POSE", () => new SpringCenterFrame(state, new Dictionary<string, PoseTransform> { [ChildBone] = default }));
            var frame = new SpringCenterFrame(state, new Dictionary<string, PoseTransform> { [ChildBone] = PoseTransform.Identity });
            var other = SpringFixture(2, new Vec3(1, 0, 0)); var changed = SpringBoneSimulator.CreateInitialState(other.Skeleton, other.Pose, new[] { other.Chain });
            Expect("SPRING_CENTER_CHANGED", () => SpringCenterMotion.Transfer(changed, frame, frame));
        });
    }
}
