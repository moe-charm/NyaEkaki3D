using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSpringPreviewTests()
    {
        var centers = new Dictionary<string, PoseTransform> { [ChildBone] = PoseTransform.Identity };
        Test("Spring preview fixed steps are independent of render frame partition", () =>
        {
            var f = SpringFixture();
            var chains = new[] { new SpringBoneChain("VRM", new[] { new SpringBoneJointSettings(ChildBone, 0, 2, .2f, new Vec3(1, 0, 0), .5f, integrationMode: SpringIntegrationMode.VrmReference) }, Array.Empty<int>()) };
            var whole = new SpringPreviewController(f.Skeleton, chains, f.Pose, null, centers);
            var split = new SpringPreviewController(f.Skeleton, chains, f.Pose, null, centers);
            whole.Play(); split.Play(); whole.Advance(f.Pose, null, centers, .2f);
            for (int i = 0; i < 20; i++) split.Advance(f.Pose, null, centers, .01f);
            Equal(12L, whole.CompletedSteps); Equal(12L, split.CompletedSteps);
            Equal(whole.Pose.ContentHash, split.Pose.ContentHash);
            SpringPointNear(whole.State.PreviousTails[ChildBone], split.State.PreviousTails[ChildBone]);
        });
        Test("Paused preview redraws center motion without committing history and resumes once", () =>
        {
            var f = SpringFixture(8, new Vec3(1, 0, 0)); var chains = new[] { f.Chain };
            var preview = new SpringPreviewController(f.Skeleton, chains, f.Pose, null, centers);
            preview.Play(); preview.Advance(f.Pose, null, centers, .02f); preview.Pause();
            var state = preview.State; var frame = preview.Centers; double remainder = preview.PendingSeconds;
            var offset = new Vec3(1, 2, 3);
            var movedCenters = new Dictionary<string, PoseTransform> { [ChildBone] = PoseTransform.FromTranslation(offset) };
            var movedPose = PoseSet.Create(f.Skeleton, f.Pose.Poses.Select(b => new BonePose(b.BoneId, new PoseTransform(b.Transform.XAxis, b.Transform.YAxis, b.Transform.ZAxis, b.Transform.Translation + offset))));
            for (int i = 0; i < 3; i++) preview.Advance(movedPose, null, movedCenters, .2f);
            True(ReferenceEquals(state, preview.State) && ReferenceEquals(frame, preview.Centers));
            True(remainder == preview.PendingSeconds); Equal(1L, preview.CompletedSteps);
            var expectedState = SpringCenterMotion.Transfer(state, frame, new SpringCenterFrame(state, movedCenters));
            var expected = SpringBoneSimulator.Step(f.Skeleton, movedPose, chains, null, expectedState, 1f / 60f);
            preview.Play(); preview.Advance(movedPose, null, movedCenters, .02f);
            Equal(expected.Pose.ContentHash, preview.Pose.ContentHash); Equal(2L, preview.CompletedSteps);
            preview.Reset(movedPose, null, movedCenters);
            False(preview.IsPlaying); Equal(0L, preview.CompletedSteps); True(preview.PendingSeconds == 0);
            Near(0, preview.State.PreviousDeltaTime);
        });
        Test("Preview failure preserves state clock centers and displayed pose for retry", () =>
        {
            var f = SpringFixture(8, new Vec3(1, 0, 0));
            var chains = new[] { new SpringBoneChain("collision", f.Chain.Joints, new[] { 0 }) };
            var empty = new[] { new SpringBoneColliderGroup("empty", Array.Empty<SpringBoneCollider>()) };
            var bad = new[] { new SpringBoneColliderGroup("enclosed", new[] { new SpringBoneCollider(new Vec3(0, 1, 0), 3) }) };
            var preview = new SpringPreviewController(f.Skeleton, chains, f.Pose, empty, centers);
            preview.Play(); preview.Advance(f.Pose, empty, centers, .01f);
            var state = preview.State; var pose = preview.Pose; var frame = preview.Centers; double pending = preview.PendingSeconds;
            Expect("SPRING_CONSTRAINT_UNRESOLVED", () => preview.Advance(f.Pose, bad, centers, .04f));
            True(ReferenceEquals(state, preview.State) && ReferenceEquals(pose, preview.Pose) && ReferenceEquals(frame, preview.Centers));
            True(pending == preview.PendingSeconds); Equal(0L, preview.CompletedSteps);
            Expect("INVALID_DELTA_TIME", () => preview.Advance(f.Pose, empty, centers, .3f));
            Expect("INVALID_SPRING_CENTER", () => preview.Reset(f.Pose, empty, new Dictionary<string, PoseTransform>()));
            True(ReferenceEquals(state, preview.State)); True(preview.IsPlaying);
            preview.Advance(f.Pose, empty, centers, .04f); Equal(3L, preview.CompletedSteps);
        });
    }
}
