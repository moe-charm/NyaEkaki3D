using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
using NyaForge.Authoring.Graph;

namespace NyaForge.UnityRuntime
{
    /// <summary>Creates one deterministic front view for a transient secondary-motion graph value.</summary>
    internal static class SecondaryMotionModelCapture
    {
        internal static byte[] Capture(GraphMeshValue value, int width, int height, out EvidenceView view)
        {
            if (value == null || value.Mesh == null) throw new InvalidOperationException("Secondary-motion capture needs a renderable mesh.");
            var min = new Vec3(float.PositiveInfinity, float.PositiveInfinity, float.PositiveInfinity);
            var max = new Vec3(float.NegativeInfinity, float.NegativeInfinity, float.NegativeInfinity);
            foreach (var local in value.Mesh.Positions)
            {
                var point = value.Transform.ToAvatarPoint(local);
                min = new Vec3(Math.Min(min.X, point.X), Math.Min(min.Y, point.Y), Math.Min(min.Z, point.Z));
                max = new Vec3(Math.Max(max.X, point.X), Math.Max(max.Y, point.Y), Math.Max(max.Z, point.Z));
            }
            var center = new Vec3((min.X + max.X) * .5f, (min.Y + max.Y) * .5f, (min.Z + max.Z) * .5f);
            double dx = (double)max.X - min.X, dy = (double)max.Y - min.Y, dz = (double)max.Z - min.Z;
            float radius = (float)Math.Max(.001, Math.Sqrt(dx * dx + dy * dy + dz * dz) * .5);
            float distance = radius * 3f;
            view = new EvidenceView(width, height, center + new Vec3(0, 0, 1) * distance, center, new Vec3(0, 1, 0), radius * 1.15f, radius * .1f, radius * 6f);
            return EvidenceModelCapture.CapturePng(value, view);
        }
    }
}
