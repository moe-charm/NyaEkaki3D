using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSpringConstraintTests()
    {
        Test("Spring solver validates all colliders after projecting and is deterministic", () =>
        {
            var colliders = new[] { new SpringBoneCollider(new Vec3(0, 1.2f, 0), .4f), new SpringBoneCollider(new Vec3(.4f, .95f, 0), .25f) };
            var a = SpringConstraintSolver.Solve(new Vec3(), new Vec3(0, 1, 0), 1, new Vec3(0, 1, 0), .03f, colliders);
            var b = SpringConstraintSolver.Solve(new Vec3(), new Vec3(0, 1, 0), 1, new Vec3(0, 1, 0), .03f, colliders);
            SpringPointNear(a, b); Near(1, Distance(a, new Vec3()));
            foreach (var collider in colliders) True(Distance(a, collider.Center) >= collider.Radius + .03f - SpringConstraintSolver.Tolerance);
        });

        Test("Spring solver handles candidate and collider center coincidences", () =>
        {
            foreach (var candidate in new[] { new Vec3(), new Vec3(0, 1, 0) })
            {
                var colliders = new[] { new SpringBoneCollider(new Vec3(0, 1, 0), .4f) };
                var value = SpringConstraintSolver.Solve(new Vec3(), candidate, 1, new Vec3(0, 1, 0), 0, colliders);
                SpringPointNear(value, value); Near(1, Distance(value, new Vec3()));
                True(Distance(value, colliders[0].Center) >= .4f - SpringConstraintSolver.Tolerance);
            }
        });

        Test("Spring conflicting spheres report bounded nonconvergence", () =>
        {
            var colliders = new[] { new SpringBoneCollider(new Vec3(0, 1, 0), 1.5f), new SpringBoneCollider(new Vec3(0, -1, 0), 1.5f) };
            // Neither sphere encloses all possible tails; together their permitted caps are disjoint.
            try { SpringConstraintSolver.Solve(new Vec3(), new Vec3(0, 1, 0), 1, new Vec3(0, 1, 0), 0, colliders); }
            catch (AuthoringException error)
            {
                True(error.Message.Contains("32 passes")); return;
            }
            throw new Exception("Expected bounded constraint failure.");
        });
        foreach (float hitRadius in new[] { 0f, .1f })
            Test("Spring coaxial collision retains length and separation: " + hitRadius, () =>
            {
                var f = SpringFixture(hitRadius: hitRadius);
                var chain = new SpringBoneChain("collision", new[] { f.Joint }, new[] { 0 });
                var groups = new[] { new SpringBoneColliderGroup("sphere", new[] { new SpringBoneCollider(new Vec3(0, 2.2f, 0), .4f) }) };
                var initial = SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, new[] { chain }, groups);
                var noCollision = SpringBoneSimulator.Step(f.Skeleton, f.Pose, new[] { f.Chain }, null, SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, new[] { f.Chain }), .01f);
                True(Distance(noCollision.State.CurrentTails[ChildBone], groups[0].Colliders[0].Center) < .4f + hitRadius);
                var result = SpringBoneSimulator.Step(f.Skeleton, f.Pose, new[] { chain }, groups, initial, .01f);
                var tail = result.State.CurrentTails[ChildBone];
                True(float.IsFinite(tail.X) && float.IsFinite(tail.Y) && float.IsFinite(tail.Z));
                Near(1, Distance(tail, new Vec3(0, 1, 0)));
                True(Distance(tail, groups[0].Colliders[0].Center) >= .4f + hitRadius - .00001f);
                SpringPointNear(tail, result.Pose.ByBoneId[ChildBone].Transform.TransformPoint(new Vec3(0, 1, 0)));
            });

        Test("Spring impossible concentric collision rejects step without changing state", () =>
        {
            var f = SpringFixture(); var chain = new SpringBoneChain("collision", new[] { f.Joint }, new[] { 0 });
            var groups = new[] { new SpringBoneColliderGroup("enclosing", new[] { new SpringBoneCollider(new Vec3(0, 1, 0), 2) }) };
            var state = SpringBoneSimulator.CreateInitialState(f.Skeleton, f.Pose, new[] { chain }, groups);
            var old = state.CurrentTails[ChildBone];
            Expect("SPRING_CONSTRAINT_UNRESOLVED", () => SpringBoneSimulator.Step(f.Skeleton, f.Pose, new[] { chain }, groups, state, .02f));
            SpringPointNear(old, state.CurrentTails[ChildBone]);
        });
    }
}
