using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void SpringPointNear(Vec3 expected, Vec3 actual)
    {
        True(float.IsFinite(actual.X) && float.IsFinite(actual.Y) && float.IsFinite(actual.Z));
        if (Distance(expected, actual) >= .0001f) throw new Exception("Spring pose distance: " + Distance(expected, actual));
    }

    static void RunSpringPoseTests()
    {
        RunSpringEndpointTests();
        Test("Spring three joints follow moving affine base poses without mutating input", () =>
        {
            string tip = "00000000-0000-0000-0000-000000000003";
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(RootBone, "root", "", new Vec3(), new Vec3(0, 1, 0)),
                new BoneDefinition(ChildBone, "child", RootBone, new Vec3(.2f, 1, 0), new Vec3(.2f, 2, 0)),
                new BoneDefinition(tip, "tip", ChildBone, new Vec3(.2f, 2, 0), new Vec3(.2f, 3, 0)) });
            var chains = new[] { new SpringBoneChain("three", skeleton.Bones.Reverse().Select(b => new SpringBoneJointSettings(b.BoneId, 0, .2f, 4, new Vec3(1, 0, 0), .1f)), Array.Empty<int>()) };
            SpringBoneState state = null;
            for (int i = 0; i < 20; i++)
            {
                var world = PoseTransform.RotationZ(i * 2, new Vec3(i * .01f, 0, 0));
                var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(b => new BonePose(b.BoneId, new PoseTransform(world.XAxis * 2, world.YAxis * 1.5f, world.ZAxis, world.TransformPoint(new Vec3(b.Head.X * 2, b.Head.Y * 1.5f, b.Head.Z))))));
                string hash = pose.ContentHash;
                if (state == null) state = SpringBoneSimulator.CreateInitialState(skeleton, pose, chains);
                var result = SpringBoneSimulator.Step(skeleton, pose, chains, null, state, .02f);
                foreach (var bone in skeleton.Bones)
                {
                    var output = result.Pose.ByBoneId[bone.BoneId].Transform;
                    SpringPointNear(result.State.CurrentTails[bone.BoneId], output.TransformPoint(bone.Tail - bone.Head));
                    Near(1.5f, Distance(output.Translation, result.State.CurrentTails[bone.BoneId]));
                    if (bone.ParentBoneId != "")
                    {
                        var inputParent = pose.ByBoneId[bone.ParentBoneId].Transform;
                        var outputParent = result.Pose.ByBoneId[bone.ParentBoneId].Transform;
                        SpringPointNear(outputParent.TransformPoint(inputParent.InverseTransformPoint(pose.ByBoneId[bone.BoneId].Transform.Translation)), output.Translation);
                    }
                }
                Equal(hash, pose.ContentHash); state = result.State;
            }
        });
        foreach (bool moving in new[] { false, true })
            Test("Spring continuous pose agrees with state, moving base: " + moving, () =>
            {
                var f = SpringFixture(8, new Vec3(1, 0, 0)); var chains = new[] { f.Chain };
                var state = SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, chains);
                string originalHash = f.Pose.ContentHash;
                for (int i = 0; i < 25; i++)
                {
                    var pose = moving ? PoseSet.Create(f.Skeleton, new[] { f.Pose.ByBoneId[RootBone], new BonePose(ChildBone, PoseTransform.RotationZ(i * 3, new Vec3(i * .01f, 1, 0))) }) : f.Pose;
                    var oldTail = state.CurrentTails[ChildBone];
                    var result = SpringBoneSimulator.Step(f.Skeleton, pose, chains, Array.Empty<SpringBoneColliderGroup>(), state, .02f);
                    SpringPointNear(result.State.CurrentTails[ChildBone], result.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3(0, 1, 0)));
                    SpringPointNear(oldTail, state.CurrentTails[ChildBone]); state = result.State;
                }
                Equal(originalHash, f.Pose.ContentHash);
            });

        foreach (bool offset in new[] { false, true })
            Test("Spring hierarchy preserves parent offsets and unregistered descendants: " + offset, () =>
            {
                string tip = "00000000-0000-0000-0000-000000000003";
                var childHead = new Vec3(offset ? .3f : 0, 1, 0); var tipHead = childHead + new Vec3(0, 1, 0);
                var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(tip, "tip", ChildBone, tipHead, tipHead + new Vec3(0, 1, 0)), new BoneDefinition(ChildBone, "child", RootBone, childHead, tipHead), new BoneDefinition(RootBone, "root", "", new Vec3(), new Vec3(0, 1, 0)) });
                var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
                var root = new SpringBoneJointSettings(RootBone, 0, 0, 8, new Vec3(1, 0, 0), 0);
                var child = new SpringBoneJointSettings(ChildBone, 0, 0, 0, new Vec3(), 0);
                var chains = new[] { new SpringBoneChain("reverse", new[] { child, root }, Array.Empty<int>()) };
                var state = SpringBoneSimulator.CreateInitialState(skeleton, pose, chains);
                for (int i = 0; i < 10; i++)
                {
                    var result = SpringBoneSimulator.Step(skeleton, pose, chains, Array.Empty<SpringBoneColliderGroup>(), state, .04f);
                    var parent = result.Pose.ByBoneId[RootBone].Transform; var c = result.Pose.ByBoneId[ChildBone].Transform;
                    SpringPointNear(parent.TransformPoint(childHead), c.Translation);
                    SpringPointNear(c.TransformPoint(new Vec3(0, 1, 0)), result.Pose.ByBoneId[tip].Transform.Translation);
                    foreach (var id in new[] { RootBone, ChildBone }) SpringPointNear(result.State.CurrentTails[id], result.Pose.ByBoneId[id].Transform.TransformPoint(new Vec3(0, 1, 0)));
                    state = result.State;
                }
                var sorted = new[] { new SpringBoneChain("sorted", new[] { root, child }, Array.Empty<int>()) };
                var a = SpringBoneSimulator.Step(skeleton, pose, chains, null, SpringBoneSimulator.CreateInitialState(skeleton, pose, chains), .1f);
                var b = SpringBoneSimulator.Step(skeleton, pose, sorted, null, SpringBoneSimulator.CreateInitialState(skeleton, pose, sorted), .1f);
                Equal(a.Pose.ContentHash, b.Pose.ContentHash);
            });
    }
}
