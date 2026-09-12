using System;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Small immutable affine transform used by the rig core. Axes map bone-local rest coordinates.</summary>
    public readonly struct PoseTransform
    {
        public readonly Vec3 XAxis, YAxis, ZAxis, Translation;

        public PoseTransform(Vec3 xAxis, Vec3 yAxis, Vec3 zAxis, Vec3 translation)
        {
            Checks.Finite(xAxis); Checks.Finite(yAxis); Checks.Finite(zAxis); Checks.Finite(translation);
            double determinant = Dot(xAxis, Cross(yAxis, zAxis));
            Checks.Require(Math.Abs(determinant) > 1e-9, "INVALID_POSE", "Pose transform basis must be invertible.");
            XAxis = xAxis; YAxis = yAxis; ZAxis = zAxis; Translation = translation;
        }

        public static PoseTransform Identity { get { return new PoseTransform(new Vec3(1, 0, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1), new Vec3()); } }
        public static PoseTransform FromTranslation(Vec3 translation) { return new PoseTransform(new Vec3(1, 0, 0), new Vec3(0, 1, 0), new Vec3(0, 0, 1), translation); }
        public static PoseTransform RotationZ(float degrees, Vec3 translation)
        {
            Checks.Finite(degrees); double radians = degrees * Math.PI / 180.0; float c = (float)Math.Cos(radians), s = (float)Math.Sin(radians);
            return new PoseTransform(new Vec3(c, s, 0), new Vec3(-s, c, 0), new Vec3(0, 0, 1), translation);
        }
        /// <summary>Creates a rigid transform from XYZ Euler degrees using the Rz*Ry*Rx convention.</summary>
        public static PoseTransform RotationEuler(float xDegrees, float yDegrees, float zDegrees, Vec3 translation)
        {
            Checks.Finite(xDegrees); Checks.Finite(yDegrees); Checks.Finite(zDegrees);
            double rx = xDegrees * Math.PI / 180.0, ry = yDegrees * Math.PI / 180.0, rz = zDegrees * Math.PI / 180.0;
            float sx = (float)Math.Sin(rx), cx = (float)Math.Cos(rx);
            float sy = (float)Math.Sin(ry), cy = (float)Math.Cos(ry);
            float sz = (float)Math.Sin(rz), cz = (float)Math.Cos(rz);
            // Columns of Rz * Ry * Rx, matching TransformPoint's basis convention.
            return new PoseTransform(
                new Vec3(cz * cy, sz * cy, -sy),
                new Vec3(cz * sy * sx - sz * cx, sz * sy * sx + cz * cx, cy * sx),
                new Vec3(cz * sy * cx + sz * sx, sz * sy * cx - cz * sx, cy * cx),
                translation);
        }

        public Vec3 TransformPoint(Vec3 point) { return Translation + XAxis * point.X + YAxis * point.Y + ZAxis * point.Z; }

        public Vec3 InverseTransformPoint(Vec3 point)
        {
            var d = point - Translation; var c1 = Cross(YAxis, ZAxis); var c2 = Cross(ZAxis, XAxis); var c3 = Cross(XAxis, YAxis); double determinant = Dot(XAxis, c1);
            return new Vec3((float)(Dot(d, c1) / determinant), (float)(Dot(d, c2) / determinant), (float)(Dot(d, c3) / determinant));
        }

        static float Dot(Vec3 a, Vec3 b) { return a.X * b.X + a.Y * b.Y + a.Z * b.Z; }
        static Vec3 Cross(Vec3 a, Vec3 b) { return new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X); }
    }

    public sealed class BonePose
    {
        public string BoneId { get; }
        public PoseTransform Transform { get; }
        public BonePose(string boneId, PoseTransform transform) { Checks.Id(boneId); BoneId = boneId; Transform = transform; }
    }
}
