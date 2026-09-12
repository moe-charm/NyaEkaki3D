using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Paint
{
    public sealed class SurfacePaintHit
    {
        internal SurfacePaintMesh Owner { get; }
        public int TriangleIndex { get; }
        public int SubmeshIndex { get; }
        public Vec2 Uv { get; }
        public double Distance { get; }
        public bool IsFrontFace { get; }
        public bool IsUvInRange=>Uv.X>=0 && Uv.X<=1 && Uv.Y>=0 && Uv.Y<=1;
        internal SurfacePaintHit(SurfacePaintMesh owner,int triangle,int submesh,Vec2 uv,double distance,bool front)
        { Owner=owner;TriangleIndex=triangle;SubmeshIndex=submesh;Uv=uv;Distance=distance;IsFrontFace=front; }
    }

    /// <summary>Paint attributes and continuity over a reusable geometry BVH.</summary>
    public sealed class SurfacePaintMesh
    {
        readonly MeshData mesh;
        readonly NyaForge.Authoring.Geometry.MeshRaycast geometry;
        readonly SurfaceUvContinuity continuity;
        public string MeshHash=>mesh.ContentHash;
        public RestTransform Transform { get; }
        public SurfacePaintMesh(MeshData source,RestTransform placement,IReadOnlyList<ulong> logicalVertexIds=null)
        {
            Checks.Require(source!=null && source.Uv0.Count==source.VertexCount,"UV_MISSING","A surface paint mesh needs UV0 on every vertex.");
            mesh=source;Transform=placement;geometry=new NyaForge.Authoring.Geometry.MeshRaycast(source,placement);
            continuity=new SurfaceUvContinuity(source,logicalVertexIds);
        }
        public SurfacePaintHit Raycast(Vec3 origin,Vec3 direction,bool cullBackFaces=false,double maxDistance=double.PositiveInfinity)
        {
            var hit=geometry.Raycast(origin,direction,cullBackFaces,maxDistance);if(hit==null) return null;
            var a=mesh.Uv0[hit.A];var b=mesh.Uv0[hit.B];var c=mesh.Uv0[hit.C];
            var uv=new Vec2((float)((1-hit.U-hit.V)*a.X+hit.U*b.X+hit.V*c.X),(float)((1-hit.U-hit.V)*a.Y+hit.U*b.Y+hit.V*c.Y));
            return new SurfacePaintHit(this,hit.TriangleIndex,hit.SubmeshIndex,uv,hit.Distance,hit.IsFrontFace);
        }
        public bool Owns(SurfacePaintHit hit)=>hit!=null && ReferenceEquals(hit.Owner,this);
        public bool CanJoin(SurfacePaintHit a,SurfacePaintHit b)=>Owns(a) && Owns(b) && a.IsUvInRange && b.IsUvInRange && continuity.CanJoin(a.TriangleIndex,b.TriangleIndex);
    }
}
