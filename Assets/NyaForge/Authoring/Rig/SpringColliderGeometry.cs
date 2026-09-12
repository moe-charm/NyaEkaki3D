using System;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Queries sphere/capsule axes without approximating a capsule by a fixed set of spheres.</summary>
    internal static class SpringColliderGeometry
    {
        internal static Vec3 ClosestCenter(SpringBoneCollider collider, Vec3 point)
        {
            if (!collider.Tail.HasValue) return collider.Center;
            var segment = collider.Tail.Value - collider.Center;
            double squaredLength = Dot(segment, segment);
            if (squaredLength == 0) return collider.Center;
            double t = Math.Max(0, Math.Min(1, Dot(point - collider.Center, segment) / squaredLength));
            return collider.Center + segment * (float)t;
        }

        internal static double DistanceToAxis(SpringBoneCollider collider, Vec3 point)
        {
            var delta = point - ClosestCenter(collider, point);
            return Math.Sqrt(Dot(delta, delta));
        }

        static double Dot(Vec3 a, Vec3 b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    }
}
