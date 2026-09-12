using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSpringCapsuleTests()
    {
        Test("Spring capsule excludes its middle while preserving the joint length", () =>
        {
            var f = SpringFixture(hitRadius: .05f);
            var collider = new SpringBoneCollider(new Vec3(0, 1.8f, 0), .25f, new Vec3(0, 2.2f, 0));
            var groups = new[] { new SpringBoneColliderGroup("capsule", new[] { collider }) };
            var chain = new SpringBoneChain("tail", new[] { f.Joint }, new[] { 0 });
            var state = SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, new[] { chain }, groups);
            var result = SpringBoneSimulator.Step(f.Skeleton, f.Pose, new[] { chain }, groups, state, .01f);
            var tail = result.State.CurrentTails[ChildBone];
            True(tail.Y >= 1.8f && tail.Y <= 2.2f);
            True(Math.Sqrt(tail.X * tail.X + tail.Z * tail.Z) >= .3f - .00001f);
            Near(1, Distance(tail, new Vec3(0, 1, 0)));
            SpringPointNear(tail, result.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3(0, 1, 0)));
        });
        Test("Spring capsule end caps and zero-length capsules match sphere geometry", () =>
        {
            var center = new Vec3(0, 1.2f, 0);
            var sphere = new SpringBoneCollider(center, .4f);
            var capsule = new SpringBoneCollider(center, .4f, new Vec3(0, 1.6f, 0));
            var point = new Vec3(0, 1, 0);
            var expected = SpringConstraintSolver.Solve(new Vec3(), point, 1, point, 0, new[] { sphere });
            var endpoint = SpringConstraintSolver.Solve(new Vec3(), point, 1, point, 0, new[] { capsule });
            SpringPointNear(expected, endpoint);
            var degenerate = SpringConstraintSolver.Solve(new Vec3(), point, 1, point, 0, new[] { new SpringBoneCollider(center, .4f, center) });
            SpringPointNear(expected, degenerate);
            SpringPointNear(new Vec3(0, 1.6f, 0), SpringColliderGeometry.ClosestCenter(capsule, new Vec3(0, 2, 0)));
        });
        Test("Spring mixed capsule and sphere constraints validate every shape", () =>
        {
            var cap = new SpringBoneCollider(new Vec3(0, .8f, 0), .25f, new Vec3(0, 1.2f, 0));
            var sphere = new SpringBoneCollider(new Vec3(.4f, .95f, 0), .25f);
            var result = SpringConstraintSolver.Solve(new Vec3(), new Vec3(0, 1, 0), 1, new Vec3(0, 1, 0), .02f, new[] { cap, sphere });
            SpringPointNear(result, result); Near(1, Distance(result, new Vec3()));
            var y = Math.Max(.8f, Math.Min(1.2f, result.Y));
            True(Distance(result, new Vec3(0, y, 0)) >= .27f - .00001f);
            True(Distance(result, sphere.Center) >= .27f - .00001f);
            var repeat = SpringConstraintSolver.Solve(new Vec3(), new Vec3(0, 1, 0), 1, new Vec3(0, 1, 0), .02f, new[] { cap, sphere });
            SpringPointNear(result, repeat);
        });
    }
}
