using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunSourceAffineTests()
    {
        Test("Source affine composes TRS and inverse bind without translating vectors", () =>
        {
            float h=(float)Math.Sqrt(.5);
            var parent=SourceAffine.FromTrs(new Vec3(10,20,30),new Vec4(0,0,h,h),new Vec3(2,3,4));
            var local=SourceAffine.FromTrs(new Vec3(1,2,3),new Vec4(0,0,0,1),new Vec3(1,1,1));
            var world=parent.Compose(local);
            SpringPointNear(new Vec3(4,22,42),world.TransformPoint(new Vec3()));
            SpringPointNear(new Vec3(0,2,0),world.TransformVector(new Vec3(1,0,0)));
            var point=new Vec3(.1f,.2f,.3f);
            SpringPointNear(point,world.Inverse().TransformPoint(world.TransformPoint(point)));
            SpringPointNear(point,world.Compose(world.Inverse()).TransformPoint(point));
            // Joint global at rest * inverse bind cancels to identity.
            var posed=local.Compose(world).Compose(world.Inverse());
            SpringPointNear(point+new Vec3(1,2,3),posed.TransformPoint(point));
        });
        Test("Source affine inverse transpose and tangent handedness survive shear and reflection", () =>
        {
            var shear=new SourceAffine(new double[] { -2,0,0,0, 1,3,0,0, 0,0,4,0, 5,6,7,1 });
            True(shear.IsMirrored);
            var n=shear.TransformNormal(new Vec3(1,0,0));
            var tangent=shear.TransformTangent(new Vec4(0,1,0,1),new Vec3(1,0,0));
            Near(0,n.X*tangent.X+n.Y*tangent.Y+n.Z*tangent.Z); Equal(-1f,tangent.W);
            SpringPointNear(new Vec3(-.9486833f,.3162278f,0),n);
            var point=new Vec3(2,3,4); SpringPointNear(point,shear.Inverse().TransformPoint(shear.TransformPoint(point)));
            var copy=shear.ToColumnMajor().ToArray(); copy[0]=0; True(shear.IsMirrored);
        });
        Test("Source affine rejects invalid matrices and preserves small valid scales", () =>
        {
            Expect("INVALID_AFFINE",()=>new SourceAffine(new double[16]));
            Expect("INVALID_AFFINE",()=>SourceAffine.FromTrs(new Vec3(),new Vec4(),new Vec3(1,1,1)));
            Expect("INVALID_AFFINE",()=>SourceAffine.FromTrs(new Vec3(),new Vec4(0,0,0,1),new Vec3(0,1,1)));
            var perspective=SourceAffine.Identity.ToColumnMajor().ToArray(); perspective[3]=.1;
            Expect("INVALID_AFFINE",()=>new SourceAffine(perspective));
            var nonfinite=SourceAffine.Identity.ToColumnMajor().ToArray(); nonfinite[12]=double.NaN;
            Expect("INVALID_AFFINE",()=>new SourceAffine(nonfinite));
            Expect("INVALID_AFFINE",()=>SourceAffine.Identity.TransformNormal(new Vec3()));
            var tiny=SourceAffine.FromTrs(new Vec3(),new Vec4(0,0,0,1),new Vec3(1e-8f,1e-8f,1e-8f));
            SpringPointNear(new Vec3(1,2,3),tiny.Inverse().TransformPoint(tiny.TransformPoint(new Vec3(1,2,3))));
        });
    }
}
