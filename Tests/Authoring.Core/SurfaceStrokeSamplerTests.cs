using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static MeshData SplitPaintQuad(bool seam=false)=>new MeshData(
        new[]{new Vec3(0,0,0),new Vec3(1,0,0),new Vec3(0,1,0),new Vec3(1,0,0),new Vec3(1,1,0),new Vec3(0,1,0)},
        Array.Empty<Vec3>(),Array.Empty<Vec4>(),seam ?
            new[]{new Vec2(0,0),new Vec2(.4f,0),new Vec2(0,.4f),new Vec2(.6f,.6f),new Vec2(1,1),new Vec2(.6f,1)} :
            new[]{new Vec2(0,0),new Vec2(1,0),new Vec2(0,1),new Vec2(1,0),new Vec2(1,1),new Vec2(0,1)},new[]{new[]{0,1,2,3,4,5}});
    static readonly ulong[] PaintQuadIds={1,2,3,2,4,3};
    static SurfacePaintHit ScreenPaintHit(SurfacePaintMesh surface,Vec2 p)=>surface.Raycast(new Vec3(p.X/100,p.Y/100,1),new Vec3(0,0,-1));
    static void RunSurfaceStrokeSamplerTests()
    {
        Test("adaptive boundary samples approach both sides of a gap without joining across it",()=>
        {
            var surface=new SurfacePaintMesh(SplitPaintQuad(),new RestTransform(1,new Vec3()),PaintQuadIds);
            var sampler=new SurfaceStrokeSampler(surface,p=>p.X>43.3f && p.X<60.7f ? null : ScreenPaintHit(surface,p),16);
            sampler.Append(new Vec2(10,20));sampler.Append(new Vec2(90,20));
            var sections=sampler.Snapshot().Sections;Equal(2,sections.Count);
            float left=sections[0].Last().X*100,right=sections[1][0].X*100;
            True(left<=43.3f && left>43.28f);True(right>=60.7f && right<60.72f);
            True(sampler.RaySampleCount>6);
        });
        Test("adaptive sampling preserves a connected stroke across subpixel triangulated strips",()=>
        {
            var positions=new List<Vec3>();var uv=new List<Vec2>();var indices=new List<int>();
            for(int x=0;x<=32;x++)
            {
                positions.Add(new Vec3(x/32f,0,0));positions.Add(new Vec3(x/32f,1,0));
                uv.Add(new Vec2(x/32f,0));uv.Add(new Vec2(x/32f,1));
                if(x<32) { int a=x*2;indices.AddRange(new[]{a,a+2,a+1,a+2,a+3,a+1}); }
            }
            var mesh=new MeshData(positions.ToArray(),Array.Empty<Vec3>(),Array.Empty<Vec4>(),uv.ToArray(),new[]{indices.ToArray()});
            var surface=new SurfacePaintMesh(mesh,new RestTransform(1,new Vec3()));
            var sampler=new SurfaceStrokeSampler(surface,p=>surface.Raycast(new Vec3(p.X/16,.3f,1),new Vec3(0,0,-1)),16);
            sampler.Append(new Vec2(.1f,0));sampler.Append(new Vec2(15.9f,0));
            Equal(1,sampler.Snapshot().Sections.Count);True(sampler.PointCount>64);
            var painted=PaintStroke.ApplyPaths(new PaintImage(256,256,new Rgba32(0,0,0,0)),sampler.Snapshot(),1,new Rgba32(255,0,0));
            for(int x=2;x<254;x++) True(painted.GetPixel(x,76).A>0);
        });
        Test("a foreign hit discovered only by boundary refinement invalidates the entire gesture",()=>
        {
            var surface=new SurfacePaintMesh(SplitPaintQuad(),new RestTransform(1,new Vec3()),PaintQuadIds);
            var foreign=new SurfacePaintMesh(SplitPaintQuad(),new RestTransform(1,new Vec3()),PaintQuadIds);
            var sampler=new SurfaceStrokeSampler(surface,p=>ScreenPaintHit(p.X==82 ? foreign : surface,p),16);
            sampler.Append(new Vec2(10,20));var frozen=sampler.Snapshot();
            Expect("SURFACE_CONTEXT_STALE",()=>sampler.Append(new Vec2(90,20)));
            Expect("STROKE_INVALIDATED",()=>sampler.Snapshot());Equal(1,frozen.PointCount);
        });
        Test("surface continuity joins split normals by logical edge and rejects UV seam or coincident unrelated vertices",()=>
        {
            var mesh=SplitPaintQuad();var surface=new SurfacePaintMesh(mesh,new RestTransform(1,new Vec3()),PaintQuadIds);
            var a=ScreenPaintHit(surface,new Vec2(10,10));var b=ScreenPaintHit(surface,new Vec2(90,90));True(surface.CanJoin(a,b));
            var unrelated=new SurfacePaintMesh(mesh,new RestTransform(1,new Vec3()));
            False(unrelated.CanJoin(ScreenPaintHit(unrelated,new Vec2(10,10)),ScreenPaintHit(unrelated,new Vec2(90,90))));
            False(surface.CanJoin(a,ScreenPaintHit(unrelated,new Vec2(90,90))));
            var seam=new SurfacePaintMesh(SplitPaintQuad(true),new RestTransform(1,new Vec3()),PaintQuadIds);
            False(seam.CanJoin(ScreenPaintHit(seam,new Vec2(10,10)),ScreenPaintHit(seam,new Vec2(90,90))));
            Expect("INVALID_VERTEX_MAP",()=>new SurfacePaintMesh(mesh,new RestTransform(1,new Vec3()),new ulong[]{1}));
            Expect("INVALID_VERTEX_MAP",()=>new SurfacePaintMesh(mesh,new RestTransform(1,new Vec3()),new ulong[]{1,1,3,2,4,3}));
        });
        Test("screen sampler interpolates sparse events, freezes snapshots and breaks at background or UV seams",()=>
        {
            var surface=new SurfacePaintMesh(SplitPaintQuad(),new RestTransform(1,new Vec3()),PaintQuadIds);
            var continuous=new SurfaceStrokeSampler(surface,p=>ScreenPaintHit(surface,p));continuous.Append(new Vec2(10,10));
            var frozen=continuous.Snapshot();continuous.Append(new Vec2(90,90));Equal(1,frozen.PointCount);
            True(continuous.PointCount>40);Equal(1,continuous.Snapshot().Sections.Count);
            var gap=new SurfaceStrokeSampler(surface,p=>p.X>40 && p.X<60 ? null : ScreenPaintHit(surface,p));
            gap.Append(new Vec2(10,10));gap.Append(new Vec2(90,90));Equal(2,gap.Snapshot().Sections.Count);
            var image=PaintStroke.ApplyPaths(new PaintImage(64,64,new Rgba32(0,0,0)),gap.Snapshot(),2,new Rgba32(255,0,0));
            Equal((byte)0,image.GetPixel(32,32).R);True(image.GetPixel(12,12).R>0);
            var seam=new SurfacePaintMesh(SplitPaintQuad(true),new RestTransform(1,new Vec3()),PaintQuadIds);
            var split=new SurfaceStrokeSampler(seam,p=>ScreenPaintHit(seam,p));split.Append(new Vec2(10,10));split.Append(new Vec2(90,90));
            Equal(2,split.Snapshot().Sections.Count);
            var painted=PaintStroke.ApplyPaths(new PaintImage(64,64,new Rgba32(0,0,0)),split.Snapshot(),2,new Rgba32(255,0,0));Equal((byte)0,painted.GetPixel(32,32).R);
        });
        Test("surface sampler rejects foreign hits and invalidates a partial gesture on UV or ray budget failure",()=>
        {
            var surface=new SurfacePaintMesh(RayTriangle(),new RestTransform(1,new Vec3()));
            var foreign=new SurfacePaintMesh(RayTriangle(),new RestTransform(1,new Vec3()));
            var stale=new SurfaceStrokeSampler(surface,p=>ScreenPaintHit(foreign,p));
            Expect("SURFACE_CONTEXT_STALE",()=>stale.Append(new Vec2(10,10)));Expect("STROKE_INVALIDATED",()=>stale.Snapshot());
            var tooMany=new SurfaceStrokeSampler(surface,p=>surface.Raycast(new Vec3(p.X/2048,.1f,1),new Vec3(0,0,-1)),.5f);
            tooMany.Append(new Vec2(0,0));var before=tooMany.Snapshot();
            tooMany.Append(new Vec2(1024,0));True(tooMany.PointCount>1024);Equal(2,tooMany.Snapshot().PointCount);Equal(1,before.PointCount);
            Expect("STROKE_BUDGET_EXCEEDED",()=>tooMany.Append(new Vec2(3072,0)));Expect("STROKE_INVALIDATED",()=>tooMany.Snapshot());
            var background=new SurfaceStrokeSampler(surface,_=>null);background.Append(new Vec2(0,0));background.Append(new Vec2(8190,0));
            Equal(4096,background.RaySampleCount);True(background.Snapshot()==null);
            Expect("STROKE_BUDGET_EXCEEDED",()=>background.Append(new Vec2(8192,0)));Expect("STROKE_INVALIDATED",()=>background.Snapshot());
        });
    }
}
