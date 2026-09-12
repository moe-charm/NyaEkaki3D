using System;

namespace NyaForge.Authoring.Geometry
{
    internal readonly struct RayVector
    {
        internal readonly double X,Y,Z;
        internal RayVector(double x,double y,double z) { X=x;Y=y;Z=z; }
        internal RayVector(Vec3 p):this(p.X,p.Y,p.Z) { }
        internal double Axis(int axis)=>axis==0 ? X : axis==1 ? Y : Z;
        public static RayVector operator +(RayVector a,RayVector b)=>new RayVector(a.X+b.X,a.Y+b.Y,a.Z+b.Z);
        public static RayVector operator -(RayVector a,RayVector b)=>new RayVector(a.X-b.X,a.Y-b.Y,a.Z-b.Z);
        public static RayVector operator *(RayVector a,double scale)=>new RayVector(a.X*scale,a.Y*scale,a.Z*scale);
        internal static double Dot(RayVector a,RayVector b)=>a.X*b.X+a.Y*b.Y+a.Z*b.Z;
        internal static RayVector Cross(RayVector a,RayVector b)=>new RayVector(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        internal static RayVector Min(RayVector a,RayVector b)=>new RayVector(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Min(a.Z,b.Z));
        internal static RayVector Max(RayVector a,RayVector b)=>new RayVector(Math.Max(a.X,b.X),Math.Max(a.Y,b.Y),Math.Max(a.Z,b.Z));
    }
    internal readonly struct RayBounds
    {
        internal readonly RayVector Min,Max;
        internal RayBounds(RayVector min,RayVector max) { Min=min;Max=max; }
        internal RayBounds Merge(RayBounds other)=>new RayBounds(RayVector.Min(Min,other.Min),RayVector.Max(Max,other.Max));
        internal bool Intersects(RayVector origin,RayVector direction,double limit,out double enter)
        {
            enter=0;double exit=limit;
            for(int axis=0;axis<3;axis++)
            {
                double o=origin.Axis(axis),d=direction.Axis(axis),a=Min.Axis(axis),b=Max.Axis(axis);
                if(d==0) { if(o<a || o>b) return false;continue; }
                double near=(a-o)/d,far=(b-o)/d;if(near>far) { double swap=near;near=far;far=swap; }
                enter=Math.Max(enter,near);exit=Math.Min(exit,far);if(enter>exit) return false;
            }
            return true;
        }
    }
}

