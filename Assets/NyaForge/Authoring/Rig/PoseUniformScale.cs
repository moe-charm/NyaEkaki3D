using System;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Checks whether an affine basis preserves spheres and returns its radius scale.</summary>
    internal static class PoseUniformScale
    {
        internal static float Require(PoseTransform transform)
        {
            double x = Dot(transform.XAxis, transform.XAxis), y = Dot(transform.YAxis, transform.YAxis), z = Dot(transform.ZAxis, transform.ZAxis);
            double tolerance = Math.Max(x, Math.Max(y, z)) * 1e-5;
            Checks.Require(x > 0 && y > 0 && z > 0 && Math.Abs(x - y) <= tolerance && Math.Abs(x - z) <= tolerance
                && Math.Abs(Dot(transform.XAxis, transform.YAxis)) <= tolerance
                && Math.Abs(Dot(transform.XAxis, transform.ZAxis)) <= tolerance
                && Math.Abs(Dot(transform.YAxis, transform.ZAxis)) <= tolerance,
                "IMPORT_COLLIDER_SCALE_UNSUPPORTED", "Sphere/capsule conversion requires uniform scale without shear.");
            float scale = (float)Math.Sqrt(x);
            Checks.Finite(scale);
            return scale;
        }

        static double Dot(Vec3 a, Vec3 b) { return (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z; }
    }
}
