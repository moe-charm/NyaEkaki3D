namespace NyaForge.Authoring.Rig
{
    /// <summary>Resolves a simulation endpoint independently of the skeleton's display tail.</summary>
    internal static class SpringJointTarget
    {
        internal static void Validate(Vec3 offset)
        {
            Checks.Finite(offset);
            double lengthSquared = (double)offset.X * offset.X + (double)offset.Y * offset.Y + (double)offset.Z * offset.Z;
            Checks.Require(lengthSquared > 1e-12, "INVALID_SPRING", "Spring endpoint must differ from its bone head.");
        }

        internal static Vec3 Position(BoneDefinition bone, SpringBoneJointSettings joint, PoseTransform pose)
        {
            var result = pose.TransformPoint(joint.RestTailOffset ?? (bone.Tail - bone.Head));
            Checks.Finite(result);
            return result;
        }
    }
}
