using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Import
{
    /// <summary>Immutable column-major affine transform; point/vector/normal semantics are explicit.</summary>
    public sealed class SourceAffine
    {
        readonly double[] m;
        public bool IsMirrored => Determinant(m) < 0;
        public static SourceAffine Identity { get; } = FromTrs(new Vec3(), new Vec4(0, 0, 0, 1), new Vec3(1, 1, 1));

        public SourceAffine(IReadOnlyList<double> columnMajor)
        {
            Require(columnMajor != null && columnMajor.Count == 16, "Affine matrix requires 16 column-major numbers.");
            m = new double[16];
            for (int i = 0; i < 16; i++) { Require(Finite(columnMajor[i]) && Math.Abs(columnMajor[i]) <= float.MaxValue, "Affine component exceeds finite float range."); m[i] = columnMajor[i]; }
            Require(m[3] == 0 && m[7] == 0 && m[11] == 0 && m[15] == 1, "Perspective matrices are not affine.");
            double magnitude = Length(m[0], m[1], m[2]) * Length(m[4], m[5], m[6]) * Length(m[8], m[9], m[10]);
            Require(magnitude > 0 && Math.Abs(Determinant(m)) / magnitude > 1e-12, "Affine basis is singular or nearly collinear.");
        }

        public IReadOnlyList<double> ToColumnMajor() => Array.AsReadOnly((double[])m.Clone());

        public static SourceAffine FromTrs(Vec3 translation, Vec4 rotation, Vec3 scale)
        {
            double x = rotation.X, y = rotation.Y, z = rotation.Z, w = rotation.W;
            double norm = Math.Sqrt(x * x + y * y + z * z + w * w);
            Require(Finite(norm) && Math.Abs(norm - 1) <= 1e-5, "Source rotation quaternion must be unit length.");
            x /= norm; y /= norm; z /= norm; w /= norm;
            return new SourceAffine(new[] {
                (1-2*(y*y+z*z))*scale.X, 2*(x*y+z*w)*scale.X, 2*(x*z-y*w)*scale.X, 0,
                2*(x*y-z*w)*scale.Y, (1-2*(x*x+z*z))*scale.Y, 2*(y*z+x*w)*scale.Y, 0,
                2*(x*z+y*w)*scale.Z, 2*(y*z-x*w)*scale.Z, (1-2*(x*x+y*y))*scale.Z, 0,
                (double)translation.X, translation.Y, translation.Z, 1 });
        }

        /// <summary>parent * local: apply local first, then this transform.</summary>
        public SourceAffine Compose(SourceAffine local)
        {
            Require(local != null, "Local affine transform is required.");
            var result = new double[16];
            for (int column = 0; column < 4; column++) for (int row = 0; row < 4; row++)
                for (int k = 0; k < 4; k++) result[column * 4 + row] += m[k * 4 + row] * local.m[column * 4 + k];
            return new SourceAffine(result);
        }

        public SourceAffine Inverse()
        {
            double d = Determinant(m);
            var r = new double[16];
            r[0]=(m[5]*m[10]-m[9]*m[6])/d; r[4]=(m[8]*m[6]-m[4]*m[10])/d; r[8]=(m[4]*m[9]-m[8]*m[5])/d;
            r[1]=(m[9]*m[2]-m[1]*m[10])/d; r[5]=(m[0]*m[10]-m[8]*m[2])/d; r[9]=(m[8]*m[1]-m[0]*m[9])/d;
            r[2]=(m[1]*m[6]-m[5]*m[2])/d; r[6]=(m[4]*m[2]-m[0]*m[6])/d; r[10]=(m[0]*m[5]-m[4]*m[1])/d;
            for (int row = 0; row < 3; row++) r[12+row]=-(r[row]*m[12]+r[4+row]*m[13]+r[8+row]*m[14]);
            r[15]=1; return new SourceAffine(r);
        }

        public Vec3 TransformPoint(Vec3 p) => Apply(p, true);
        public Vec3 TransformVector(Vec3 v) => Apply(v, false);

        public Vec3 TransformNormal(Vec3 n)
        {
            var inverse = Inverse().m;
            return Unit(inverse[0]*n.X+inverse[1]*n.Y+inverse[2]*n.Z,
                inverse[4]*n.X+inverse[5]*n.Y+inverse[6]*n.Z, inverse[8]*n.X+inverse[9]*n.Y+inverse[10]*n.Z);
        }

        public Vec4 TransformTangent(Vec4 tangent, Vec3 sourceNormal)
        {
            Require(tangent.W == -1 || tangent.W == 1, "Tangent handedness must be -1 or 1.");
            var n = TransformNormal(sourceNormal); var t = TransformVector(new Vec3(tangent.X, tangent.Y, tangent.Z));
            double dot = (double)t.X*n.X+(double)t.Y*n.Y+(double)t.Z*n.Z;
            var direction = Unit(t.X-dot*n.X, t.Y-dot*n.Y, t.Z-dot*n.Z);
            return new Vec4(direction.X, direction.Y, direction.Z, IsMirrored ? -tangent.W : tangent.W);
        }

        Vec3 Apply(Vec3 p, bool point) => Vector(m[0]*p.X+m[4]*p.Y+m[8]*p.Z+(point?m[12]:0),
            m[1]*p.X+m[5]*p.Y+m[9]*p.Z+(point?m[13]:0), m[2]*p.X+m[6]*p.Y+m[10]*p.Z+(point?m[14]:0));
        static Vec3 Unit(double x, double y, double z)
        {
            double length=Length(x,y,z); Require(Finite(length) && length > 0, "Direction must be finite and nonzero.");
            return Vector(x/length,y/length,z/length);
        }
        static Vec3 Vector(double x, double y, double z)
        {
            Require(Finite(x)&&Finite(y)&&Finite(z)&&Math.Abs(x)<=float.MaxValue&&Math.Abs(y)<=float.MaxValue&&Math.Abs(z)<=float.MaxValue, "Transformed vector exceeds finite float range.");
            return new Vec3((float)x,(float)y,(float)z);
        }
        static double Determinant(double[] a) => a[0]*(a[5]*a[10]-a[9]*a[6])-a[4]*(a[1]*a[10]-a[9]*a[2])+a[8]*(a[1]*a[6]-a[5]*a[2]);
        static double Length(double x,double y,double z) => Math.Sqrt(x*x+y*y+z*z);
        static bool Finite(double value) => !double.IsNaN(value)&&!double.IsInfinity(value);
        static void Require(bool condition,string message) => Checks.Require(condition,"INVALID_AFFINE",message);
    }
}
