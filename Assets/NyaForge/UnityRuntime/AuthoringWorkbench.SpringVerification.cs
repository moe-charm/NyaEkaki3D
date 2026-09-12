using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifySpringCore(List<string> checks)
        {
            const string root = "00000000-0000-0000-0000-000000000001";
            const string child = "00000000-0000-0000-0000-000000000002";
            var up = new Vec3(0, 1, 0);
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "root", "", new Vec3(), up), new BoneDefinition(child, "child", root, up, up * 2) });
            var pose = PoseSet.Create(skeleton, new[] { new BonePose(root, PoseTransform.Identity), new BonePose(child, PoseTransform.FromTranslation(up)) });
            var joints = new[] { new SpringBoneJointSettings(root, 0, .1f, 4, new Vec3(1, 0, 0), .1f), new SpringBoneJointSettings(child, 0, .1f, 1, new Vec3(1, 0, 0), .1f) };
            var chains = new[] { new SpringBoneChain("tail", joints, new[] { 0 }) };
            var center = new Vec3(0, 1.2f, 0);
            var groups = new[] { new SpringBoneColliderGroup("sphere", new[] { new SpringBoneCollider(center, .4f) }) };
            var state = SpringBoneSimulator.CreateInitialState(skeleton, pose, chains, groups);
            for (int frame = 0; frame < 12; frame++)
            {
                var result = SpringBoneSimulator.Step(skeleton, pose, chains, groups, state, frame % 2 == 0 ? .01f : .02f);
                foreach (var joint in joints)
                {
                    var transform = result.Pose.ByBoneId[joint.BoneId].Transform;
                    SpringCheckNear(transform.TransformPoint(up), result.State.CurrentTails[joint.BoneId]);
                    var delta = result.State.CurrentTails[joint.BoneId] - center;
                    Check(delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z >= .16f - .00001f, "Spring tail penetrated collider in Player");
                }
                SpringCheckNear(result.Pose.ByBoneId[root].Transform.TransformPoint(up), result.Pose.ByBoneId[child].Transform.Translation);
                var paused = SpringBoneSimulator.Step(skeleton, pose, chains, groups, result.State, 0);
                Check(ReferenceEquals(result.State, paused.State), "Spring pause changed state in Player");
                state = paused.State;
            }
            checks.Add("Spring Core in Player: continuous Pose/State, parent propagation, sphere separation, alternating dt and paused history");
        }

        void SpringCheckNear(Vec3 expected, Vec3 actual)
        {
            var delta = actual - expected;
            float distanceSquared = delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
            Check(!float.IsNaN(distanceSquared) && !float.IsInfinity(distanceSquared) && distanceSquared < .00000001f, "Spring pose/state mismatch in Player");
        }
    }
}
