using System;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Immutable world-to-panel projection with no engine objects or callbacks.</summary>
    public sealed class SurfaceCameraSnapshot : IEquatable<SurfaceCameraSnapshot>
    {
        readonly Vec4 clipX,clipY,clipW,depth;
        readonly Vec2 origin,size;
        public float Near { get; }
        public float Far { get; }
        public SurfaceCameraSnapshot(Vec4 clipX,Vec4 clipY,Vec4 clipW,Vec4 depth,Vec2 origin,Vec2 size,float near,float far)
        {
            foreach(var row in new[]{clipX,clipY,clipW,depth}) { Checks.Finite(row.X);Checks.Finite(row.Y);Checks.Finite(row.Z);Checks.Finite(row.W); }
            Checks.Finite(origin.X);Checks.Finite(origin.Y);Checks.Finite(size.X);Checks.Finite(size.Y);Checks.Finite(near);Checks.Finite(far);
            Checks.Require(size.X>0 && size.Y>0 && near>0 && far>near,"INVALID_PAINT_CAMERA","Positive viewport and camera depth range required.");
            this.clipX=clipX;this.clipY=clipY;this.clipW=clipW;this.depth=depth;this.origin=origin;this.size=size;Near=near;Far=far;
        }
        static double Dot(Vec4 row,Vec3 p)=>(double)row.X*p.X+(double)row.Y*p.Y+(double)row.Z*p.Z+row.W;
        public float Depth(Vec3 point) { Checks.Finite(point);float value=(float)Dot(depth,point);Checks.Finite(value);return value; }
        public double ProjectionWeight(Vec3 point) { Checks.Finite(point);return Dot(clipW,point); }
        public Vec2 Project(Vec3 point)
        {
            Checks.Finite(point);double w=Dot(clipW,point);
            Checks.Require(w>0,"INVALID_PAINT_CAMERA","Clip the triangle before projecting behind the camera.");
            float x=(float)(origin.X+(Dot(clipX,point)/w+1)*.5*size.X);
            float y=(float)(origin.Y+(1-Dot(clipY,point)/w)*.5*size.Y);
            Checks.Finite(x);Checks.Finite(y);return new Vec2(x,y);
        }
        public SurfaceScreenCoverage BuildCoverage(MeshData mesh,RestTransform transform)=>SurfaceScreenProjection.Build(mesh,transform,Depth,Project,Near,Far);
        public bool Equals(SurfaceCameraSnapshot other)=>other!=null && clipX.Equals(other.clipX) && clipY.Equals(other.clipY) && clipW.Equals(other.clipW) &&
            depth.Equals(other.depth) && origin.Equals(other.origin) && size.Equals(other.size) && Near==other.Near && Far==other.Far;
        public override bool Equals(object obj)=>Equals(obj as SurfaceCameraSnapshot);
        public override int GetHashCode()=>unchecked(clipX.GetHashCode()*397 ^ clipY.GetHashCode() ^ clipW.GetHashCode() ^ depth.GetHashCode() ^ origin.GetHashCode() ^ size.GetHashCode() ^ Near.GetHashCode() ^ Far.GetHashCode());
    }
}

