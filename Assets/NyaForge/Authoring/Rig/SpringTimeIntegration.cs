using System;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Variable-step Verlet prediction, separate from pose projection and collision constraints.</summary>
    internal static class SpringTimeIntegration
    {
        internal static Vec3 Predict(SpringBoneJointSettings joint, Vec3 current, Vec3 previous, Vec3 target, float deltaTime, float previousDeltaTime, Vec3? head = null)
        {
            if (joint.IntegrationMode == SpringIntegrationMode.VrmReference)
            {
                Checks.Require(head.HasValue, "INVALID_SPRING", "VRM integration requires a posed head.");
                return VrmSpringIntegration.Predict(joint, current, previous, target - head.Value, deltaTime);
            }
            // DragForce means velocity loss per 1/60 second, independent of call frequency.
            double retention = Math.Pow(1 - joint.DragForce, deltaTime * 60.0);
            double ratio = previousDeltaTime > 0 ? deltaTime / (double)previousDeltaTime : 0;
            var candidate = current + (current - previous) * (float)(ratio * retention);
            float stiffness = (float)(1 - Math.Exp(-joint.Stiffness * deltaTime));
            candidate = candidate + (target - candidate) * stiffness;
            // The first interval starts at rest; subsequent intervals use unequal-step Verlet.
            double accelerationTime = .5 * deltaTime * (deltaTime + previousDeltaTime);
            candidate = candidate + joint.GravityDirection * (float)(joint.GravityPower * accelerationTime);
            Checks.Finite(candidate);
            return candidate;
        }
    }
}
