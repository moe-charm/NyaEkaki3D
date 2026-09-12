using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSpringTimeTests()
    {
        Test("Spring variable-step inertia and gravity match elapsed time", () =>
        {
            var joint = new SpringBoneJointSettings(ChildBone, 0, 0, 8, new Vec3(1, 0, 0), 0);
            var first = SpringTimeIntegration.Predict(joint, new Vec3(), new Vec3(), new Vec3(), .1f, 0);
            var second = SpringTimeIntegration.Predict(joint, first, new Vec3(), new Vec3(), .2f, .1f);
            Near(.04f, first.X); Near(.36f, second.X);
            var free = new SpringBoneJointSettings(ChildBone, 0, 0, 0, new Vec3(), 0);
            var next = SpringTimeIntegration.Predict(free, new Vec3(.1f, 0, 0), new Vec3(), new Vec3(), .025f, .1f);
            Near(.125f, next.X);
        });
        Test("Spring drag has the same velocity retention across subdivided time", () =>
        {
            var joint = new SpringBoneJointSettings(ChildBone, 0, 0, 0, new Vec3(), .1f);
            var start = new Vec3(.01f, 0, 0);
            var whole = SpringTimeIntegration.Predict(joint, start, new Vec3(), start, .02f, .01f);
            var half = SpringTimeIntegration.Predict(joint, start, new Vec3(), start, .01f, .01f);
            var end = SpringTimeIntegration.Predict(joint, half, start, start, .01f, .01f);
            Near((whole.X - start.X) / .02f, (end.X - half.X) / .01f);
        });
        Test("Spring fixed and alternating frame intervals stay close over equal duration", () =>
        {
            var f = SpringFixture(1, new Vec3(1, 0, 0)); var chains = new[] { f.Chain };
            SpringBoneState Run(bool alternating)
            {
                var state = SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, chains);
                for (int i = 0; i < 60; i++) state = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, state, alternating ? (i % 2 == 0 ? .005f : .015f) : .01f).State;
                return state;
            }
            var fixedStep = Run(false); var variable = Run(true);
            True(Distance(fixedStep.CurrentTails[ChildBone], variable.CurrentTails[ChildBone]) < .002f);
            True(float.IsFinite(variable.CurrentTails[ChildBone].X));
        });
        Test("Spring pause retains history and resumes as if no zero-time calls occurred", () =>
        {
            var f = SpringFixture(8, new Vec3(1, 0, 0)); var chains = new[] { f.Chain };
            var initial = SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, chains);
            var first = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, initial, .02f);
            var state = first.State;
            for (int i = 0; i < 10; i++)
            {
                var paused = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, state, 0);
                SpringPointNear(first.State.CurrentTails[ChildBone], paused.State.CurrentTails[ChildBone]);
                SpringPointNear(first.State.PreviousTails[ChildBone], paused.State.PreviousTails[ChildBone]);
                True(ReferenceEquals(first.State, paused.State));
                SpringPointNear(first.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3(0, 1, 0)), paused.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3(0, 1, 0)));
                Near(.02f, paused.State.PreviousDeltaTime); state = paused.State;
            }
            var resumed = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, state, .01f);
            var uninterrupted = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, first.State, .01f);
            Equal(uninterrupted.Pose.ContentHash, resumed.Pose.ContentHash);
        });
        Test("Spring paused base-pose edits redraw without advancing physical history", () =>
        {
            var f = SpringFixture(4, new Vec3(1, 0, 0)); var chains = new[] { f.Chain };
            var state = SpringBoneSimulator.Step(f.Skeleton, f.Pose, chains, null, SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, chains), .02f).State;
            var moved = PoseSet.Create(f.Skeleton, new[] { f.Pose.ByBoneId[RootBone], new BonePose(ChildBone, PoseTransform.RotationZ(15, new Vec3(.2f, 1, 0))) });
            var paused = SpringBoneSimulator.Step(f.Skeleton, moved, chains, null, state, 0);
            True(ReferenceEquals(state, paused.State)); SpringPointNear(new Vec3(.2f, 1, 0), paused.Pose.ByBoneId[ChildBone].Transform.Translation);
            var resumed = SpringBoneSimulator.Step(f.Skeleton, moved, chains, null, paused.State, .01f);
            SpringPointNear(resumed.State.CurrentTails[ChildBone], resumed.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3(0, 1, 0)));
        });
    }
}
