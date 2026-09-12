using System;

namespace NyaForge.Authoring.Topology
{
    internal static class CornerInterpolation
    {
        internal static Vec3 Position(Vec3 a,Vec3 b,float t)=>new Vec3(Mix(a.X,b.X,t),Mix(a.Y,b.Y,t),Mix(a.Z,b.Z,t));
        internal static CageCorner Between(ulong id,ulong vertex,CageCorner a,CageCorner b,float t)
        {
            Vec2? uv=a.Uv0.HasValue ? new Vec2(Mix(a.Uv0.Value.X,b.Uv0.Value.X,t),Mix(a.Uv0.Value.Y,b.Uv0.Value.Y,t)) : (Vec2?)null;
            Vec3? normal=a.Normal.HasValue ? Unit(Position(a.Normal.Value,b.Normal.Value,t)) : (Vec3?)null;
            Vec4? tangent=null;
            if(a.Tangent.HasValue)
            {
                var x=a.Tangent.Value;var y=b.Tangent.Value;
                Checks.Require(x.W==y.W,"TANGENT_SEAM","Cannot interpolate opposite tangent handedness within an edge.");
                var direction=Position(new Vec3(x.X,x.Y,x.Z),new Vec3(y.X,y.Y,y.Z),t);
                if(normal.HasValue) { var n=normal.Value;direction=direction-n*(float)Dot(direction,n); }
                direction=Unit(direction);tangent=new Vec4(direction.X,direction.Y,direction.Z,x.W);
            }
            return new CageCorner(id,vertex,uv,normal,tangent);
        }
        static float Mix(float a,float b,float t)=>(float)((double)a*(1.0-t)+(double)b*t);
        static double Dot(Vec3 a,Vec3 b)=>(double)a.X*b.X+(double)a.Y*b.Y+(double)a.Z*b.Z;
        static Vec3 Unit(Vec3 value)
        {
            double length=Math.Sqrt(Dot(value,value));
            Checks.Require(length>1e-12 && double.IsFinite(length),"INVALID_ATTRIBUTE","Interpolated shading direction is degenerate.");
            return value*(float)(1/length);
        }
    }
}
