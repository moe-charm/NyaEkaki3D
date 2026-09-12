using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSpringEndpointTests()
    {
        Test("Spring rotation preserves an explicit source pivot independently of bind head", () =>
        {
            var pivot = new Vec3(.3f, 0, 0); var tail = new Vec3(.3f, 2, 0);
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(RootBone, "head", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(ChildBone, "tail", RootBone, tail, tail + new Vec3(0, .1f, 0)) });
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(b => new BonePose(b.BoneId, new PoseTransform(new Vec3(2, 0, 0), new Vec3(0, 2, 0), new Vec3(0, 0, 2), b.Head * 2))));
            SpringBoneChain Chain(Vec3 head) => new SpringBoneChain("source pair", new[] { new SpringBoneJointSettings(RootBone, 0, .2f, 8, new Vec3(1, 0, 0), .1f, tail, head) }, Array.Empty<int>());
            var chains = new[] { Chain(pivot) };
            var state = SpringBoneSimulator.CreateInitialState(skeleton, pose, chains);
            for (int i = 0; i < 12; i++)
            {
                var result = SpringBoneSimulator.Step(skeleton, pose, chains, null, state, .02f); state = result.State;
                var transform = result.Pose.ByBoneId[RootBone].Transform;
                SpringPointNear(pivot * 2, transform.TransformPoint(pivot));
                SpringPointNear(state.CurrentTails[RootBone], transform.TransformPoint(tail));
                SpringPointNear(state.CurrentTails[RootBone], result.Pose.ByBoneId[ChildBone].Transform.Translation);
                Near(4, Distance(pivot * 2, state.CurrentTails[RootBone]));
            }
            Expect("SPRING_CHAIN_CHANGED", () => SpringBoneSimulator.Step(skeleton, pose, new[] { Chain(new Vec3()) }, null, state, .02f));
            Expect("INVALID_SPRING", () => Chain(tail));
            var invalid = new SpringBoneChain("implicit coincident tail", new[] { new SpringBoneJointSettings(RootBone, 0, 0, 0, new Vec3(), 0, restHeadOffset: new Vec3(0, .1f, 0)) }, Array.Empty<int>());
            Expect("INVALID_SPRING", () => SpringBoneSimulator.CreateInitialState(skeleton, pose, new[] { invalid }));
        });
        Test("Explicit spring endpoint controls length rotation collision and unsimulated descendants", () =>
        {
            string tip = "00000000-0000-0000-0000-000000000003";
            var endpoint = new Vec3(.2f, 2, .3f);
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(RootBone, "root", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(ChildBone, "middle", RootBone, new Vec3(0, 1, 0), new Vec3(0, 1.1f, 0)),
                new BoneDefinition(tip, "endpoint", ChildBone, endpoint, endpoint + new Vec3(0, .1f, 0)) });
            var joint = new SpringBoneJointSettings(RootBone, .02f, .2f, 4, new Vec3(1, 0, 0), .1f, endpoint);
            var chains = new[] { new SpringBoneChain("pair", new[] { joint }, new[] { 0 }) };
            var collider = new SpringBoneCollider(endpoint * 2, .05f);
            var groups = new[] { new SpringBoneColliderGroup("tip obstacle", new[] { collider }) };
            var pose = PoseSet.Create(skeleton, skeleton.Bones.Select(b => new BonePose(b.BoneId, new PoseTransform(new Vec3(2, 0, 0), new Vec3(0, 2, 0), new Vec3(0, 0, 2), b.Head * 2))));
            var state = SpringBoneSimulator.CreateInitialState(skeleton, pose, chains, groups);
            SpringPointNear(endpoint * 2, state.CurrentTails[RootBone]);
            string original = skeleton.ContentHash;
            for (int i = 0; i < 12; i++)
            {
                var result = SpringBoneSimulator.Step(skeleton, pose, chains, groups, state, .02f); state = result.State;
                SpringPointNear(state.CurrentTails[RootBone], result.Pose.ByBoneId[RootBone].Transform.TransformPoint(endpoint));
                SpringPointNear(state.CurrentTails[RootBone], result.Pose.ByBoneId[tip].Transform.Translation);
                Near(Distance(new Vec3(), endpoint * 2), Distance(result.Pose.ByBoneId[RootBone].Transform.Translation, state.CurrentTails[RootBone]));
                True(Distance(collider.Center, state.CurrentTails[RootBone]) >= .07f - .00001f);
            }
            Equal(original, skeleton.ContentHash);
            var changed = new[] { new SpringBoneChain("pair", new[] { new SpringBoneJointSettings(RootBone, .02f, .2f, 4, new Vec3(1, 0, 0), .1f, endpoint * 2) }, new[] { 0 }) };
            Expect("SPRING_CHAIN_CHANGED", () => SpringBoneSimulator.Step(skeleton, pose, changed, groups, state, .02f));
        });
        Test("Spring explicit endpoints reject zero and nonfinite offsets", () =>
        {
            Expect("INVALID_SPRING", () => new SpringBoneJointSettings(RootBone, 0, 0, 0, new Vec3(), 0, new Vec3()));
            bool rejected = false;
            try { _ = new SpringBoneJointSettings(RootBone, 0, 0, 0, new Vec3(), 0, new Vec3(float.NaN, 1, 0)); }
            catch (AuthoringException) { rejected = true; }
            True(rejected);
        });
    }
}
