using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static MeshData RayTriangle(float size=1)=>new MeshData(new[]{new Vec3(0,0,0),new Vec3(size,0,0),new Vec3(0,size,0)},
        Array.Empty<Vec3>(),Array.Empty<Vec4>(),new[]{new Vec2(0,0),new Vec2(1,0),new Vec2(0,1)},new[]{new[]{0,1,2}});
    static void RunSurfacePaintMeshTests()
    {
        Test("surface paint ray returns barycentric UV and world distance at different source scales",()=>
        {
            foreach(float scale in new[]{1f,100f,.000001f})
            {
                var transform=new RestTransform(scale,new Vec3(0,0,2));var mesh=RayTriangle();var surface=new SurfacePaintMesh(mesh,transform);
                var hit=surface.Raycast(new Vec3(.2f*scale,.3f*scale,5),new Vec3(0,0,-12));
                True(hit!=null && hit.IsFrontFace && hit.IsUvInRange);Near(.2f,hit.Uv.X);Near(.3f,hit.Uv.Y);True(Math.Abs(hit.Distance-3)<1e-8);
                Equal(0,hit.TriangleIndex);Equal(mesh.ContentHash,surface.MeshHash);
                True(surface.Raycast(new Vec3(.2f*scale,.3f*scale,5),new Vec3(0,0,-1),false,2.99)==null);
            }
            var tiny=new SurfacePaintMesh(RayTriangle(.00001f),new RestTransform(.000001f,new Vec3()));
            var tinyHit=tiny.Raycast(new Vec3(2e-12f,3e-12f,1e-8f),new Vec3(0,0,-1));True(tinyHit!=null);Near(.2f,tinyHit.Uv.X);Near(.3f,tinyHit.Uv.Y);
        });
        Test("surface ray double-sided visibility, missed rays and validation are explicit",()=>
        {
            var mesh=RayTriangle();var surface=new SurfacePaintMesh(mesh,new RestTransform(1,new Vec3()));
            var hit=surface.Raycast(new Vec3(.2f,.3f,-1),new Vec3(0,0,1));True(hit!=null && !hit.IsFrontFace);
            True(surface.Raycast(new Vec3(.2f,.3f,-1),new Vec3(0,0,1),true)==null);
            True(surface.Raycast(new Vec3(2,2,1),new Vec3(0,0,-1))==null);
            True(surface.Raycast(new Vec3(.2f,.3f,1),new Vec3(1,0,0))==null);
            True(surface.Raycast(new Vec3(.2f,.3f,1),new Vec3(0,0,1))==null);
            True(surface.Raycast(new Vec3(0,0,1),new Vec3(0,0,-1))!=null);
            Expect("INVALID_PAINT_RAY",()=>surface.Raycast(new Vec3(),new Vec3()));
            Expect("INVALID_PAINT_RAY",()=>surface.Raycast(new Vec3(),new Vec3(0,0,1),false,-1));
            var noUv=new MeshData(mesh.Positions.ToArray(),Array.Empty<Vec3>(),Array.Empty<Vec4>(),Array.Empty<Vec2>(),mesh.Submeshes.ToArray());
            Expect("UV_MISSING",()=>new SurfacePaintMesh(noUv,new RestTransform(1,new Vec3())));
        });
        Test("BVH picks nearest submesh and never skips an occluder with out-of-range UV",()=>
        {
            var positions=new List<Vec3>();var uv=new List<Vec2>();var indices=new List<int>();
            for(int i=0;i<32;i++)
            {
                positions.AddRange(new[]{new Vec3(0,0,i),new Vec3(1,0,i),new Vec3(0,1,i)});
                uv.AddRange(i==31 ? new[]{new Vec2(2,2),new Vec2(3,2),new Vec2(2,3)} : new[]{new Vec2(0,0),new Vec2(1,0),new Vec2(0,1)});
                indices.AddRange(new[]{i*3,i*3+1,i*3+2});
            }
            var mesh=new MeshData(positions.ToArray(),Array.Empty<Vec3>(),Array.Empty<Vec4>(),uv.ToArray(),new[]{indices.Take(48).ToArray(),indices.Skip(48).ToArray()});
            var surface=new SurfacePaintMesh(mesh,new RestTransform(1,new Vec3()));
            var hit=surface.Raycast(new Vec3(.25f,.25f,40),new Vec3(0,0,-1));
            Equal(31,hit.TriangleIndex);Equal(1,hit.SubmeshIndex);True(!hit.IsUvInRange);True(Math.Abs(hit.Distance-9)<1e-8);Near(2.25f,hit.Uv.X);
            var back=surface.Raycast(new Vec3(.25f,.25f,-2),new Vec3(0,0,1));Equal(0,back.TriangleIndex);True(back.IsUvInRange);
        });
        Test("split render vertices preserve each triangle UV across a seam",()=>
        {
            var mesh=new MeshData(new[]{new Vec3(0,0,0),new Vec3(1,0,0),new Vec3(0,1,0),new Vec3(1,0,0),new Vec3(1,1,0),new Vec3(0,1,0)},
                Array.Empty<Vec3>(),Array.Empty<Vec4>(),new[]{new Vec2(0,0),new Vec2(.4f,0),new Vec2(0,.4f),new Vec2(.6f,.6f),new Vec2(1,1),new Vec2(.6f,1)},new[]{new[]{0,1,2,3,4,5}});
            var surface=new SurfacePaintMesh(mesh,new RestTransform(1,new Vec3()));
            var left=surface.Raycast(new Vec3(.1f,.1f,1),new Vec3(0,0,-1));var right=surface.Raycast(new Vec3(.9f,.9f,1),new Vec3(0,0,-1));
            Equal(0,left.TriangleIndex);Equal(1,right.TriangleIndex);Near(.04f,left.Uv.X);Near(.92f,right.Uv.X);
        });
    }
}
