namespace NyaForge.Authoring.Rig
{
    public enum SpringIntegrationMode { Authoring = 0, VrmReference = 1 }

    /// <summary>VRMC_springBone reference inertia/force step; drag is applied per simulation step.</summary>
    internal static class VrmSpringIntegration
    {
        internal static Vec3 Predict(SpringBoneJointSettings joint, Vec3 current, Vec3 previous, Vec3 restDirection, float deltaTime)
        {
            var direction = SpringBoneJointSettings.NormalizeOrZero(restDirection);
            var next = current + (current - previous) * (1 - joint.DragForce)
                + direction * (joint.Stiffness * deltaTime)
                + joint.GravityDirection * (joint.GravityPower * deltaTime);
            Checks.Finite(next);
            return next;
        }
    }
}
