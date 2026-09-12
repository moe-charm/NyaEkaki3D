using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Projects onto the bone-length sphere while excluding collider interiors.</summary>
    internal static class SpringConstraintSolver
    {
        internal const int MaxPasses = 32;
        internal const float Tolerance = .00001f;

        internal static Vec3 Solve(Vec3 head, Vec3 candidate, float length, Vec3 fallback, float hitRadius, IReadOnlyList<SpringBoneCollider> colliders)
        {
            var preferred = Unit(candidate - head, Unit(fallback, new Vec3(0, 1, 0)));
            // A single projection path can cycle even when another part of the sphere is feasible.
            // Try the requested direction first, then six deterministic axis seeds.
            var seeds = new[] { preferred, new Vec3(1, 0, 0), new Vec3(-1, 0, 0), new Vec3(0, 1, 0), new Vec3(0, -1, 0), new Vec3(0, 0, 1), new Vec3(0, 0, -1) };
            foreach (var seed in seeds)
            {
                var direction = seed;
                var result = head + direction * length;
                for (int pass = 0; pass < MaxPasses; pass++)
                {
                    foreach (var collider in colliders)
                    {
                        double radius = collider.Radius + hitRadius;
                        var center = SpringColliderGeometry.ClosestCenter(collider, result);
                        if (Distance(result, center) + Tolerance >= radius) continue;
                        var offset = center - head;
                        double d = Length(offset);
                        // An enclosing sphere has no feasible point on the bone-length sphere.
                        if (d + length + Tolerance < radius || d < 1e-12)
                            throw Unresolved("A collider encloses the bone-length sphere.");
                        var axis = offset * (float)(1 / d);
                        double boundary = ((double)length * length + d * d - radius * radius) / (2 * length * d);
                        boundary = Math.Max(-1, Math.Min(1, boundary));
                        var tangent = direction - axis * (float)Dot(direction, axis);
                        tangent = Unit(tangent, Perpendicular(axis));
                        // Move to the intersection with the current closest sphere on the collider axis.
                        // Capsule closest points are recomputed every pass and during final validation.
                        direction = Unit(axis * (float)boundary + tangent * (float)Math.Sqrt(Math.Max(0, 1 - boundary * boundary)), axis * -1f);
                        result = head + direction * length;
                    }
                    Checks.Finite(result);
                    bool satisfied = Math.Abs(Distance(head, result) - length) <= Tolerance;
                    foreach (var collider in colliders)
                        satisfied &= SpringColliderGeometry.DistanceToAxis(collider, result) + Tolerance >= collider.Radius + hitRadius;
                    if (satisfied) return result;
                }
            }
            throw Unresolved("Collision constraints did not converge within " + MaxPasses + " passes per seed (7 seeds).");
        }

        static AuthoringException Unresolved(string reason) => new AuthoringException("SPRING_CONSTRAINT_UNRESOLVED", reason);
        static double Dot(Vec3 a, Vec3 b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
        static double Length(Vec3 value) => Math.Sqrt(Dot(value, value));
        static double Distance(Vec3 a, Vec3 b) => Length(a - b);
        static Vec3 Unit(Vec3 value, Vec3 fallback)
        {
            double length = Length(value);
            return length < 1e-7 ? fallback : value * (float)(1 / length);
        }
        static Vec3 Perpendicular(Vec3 axis)
        {
            var reference = Math.Abs(axis.X) <= Math.Abs(axis.Y) && Math.Abs(axis.X) <= Math.Abs(axis.Z)
                ? new Vec3(1, 0, 0) : Math.Abs(axis.Y) <= Math.Abs(axis.Z) ? new Vec3(0, 1, 0) : new Vec3(0, 0, 1);
            return Unit(reference - axis * (float)Dot(reference, axis), new Vec3(1, 0, 0));
        }
    }
}
