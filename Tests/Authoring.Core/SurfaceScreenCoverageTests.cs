using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunSurfaceScreenCoverageTests()
    {
        Test("projected coverage finds a thin unpaintable occluder hidden between equal coarse hits",()=>
        {
            var positions=new[]{new Vec3(0,0,0),new Vec3(1,0,0),new Vec3(0,1,0),new Vec3(1,1,0),
                new Vec3(.5000f,0,.1f),new Vec3(.5002f,0,.1f),new Vec3(.5000f,1,.1f),new Vec3(.5002f,1,.1f)};
            var uv=new[]{new Vec2(0,0),new Vec2(1,0),new Vec2(0,1),new Vec2(1,1),new Vec2(2,0),new Vec2(3,0),new Vec2(2,1),new Vec2(3,1)};
            var mesh=new MeshData(positions,Array.Empty<Vec3>(),Array.Empty<Vec4>(),uv,new[]{new[]{0,1,2,1,3,2,4,5,6,5,7,6}});
            var transform=new RestTransform(1,new Vec3());var surface=new SurfacePaintMesh(mesh,transform);
            var coverage=SurfaceScreenProjection.Build(mesh,transform,p=>1-p.Z,p=>new Vec2(p.X*100,p.Y*100),.01f,10);
            var coarse=new SurfaceStrokeSampler(surface,p=>ScreenPaintHit(surface,p),16);
            coarse.Append(new Vec2(10,20));coarse.Append(new Vec2(90,20));Equal(1,coarse.Snapshot().Sections.Count);
            var guarded=new SurfaceStrokeSampler(surface,p=>ScreenPaintHit(surface,p),16,coverage);
            guarded.Append(new Vec2(10,20));guarded.Append(new Vec2(90,20));Equal(2,guarded.Snapshot().Sections.Count);
            True(guarded.Snapshot().Sections[0].Last().X<=.50001f);True(guarded.Snapshot().Sections[1][0].X>=.50019f);
        });
        Test("screen projection clips near and far planes before perspective division",()=>
        {
            var mesh=new MeshData(new[]{new Vec3(-1,0,-1),new Vec3(1,0,2),new Vec3(0,1,2)},Array.Empty<Vec3>(),Array.Empty<Vec4>(),Array.Empty<Vec2>(),new[]{new[]{0,1,2}});
            int projected=0;
            var coverage=SurfaceScreenProjection.Build(mesh,new RestTransform(1,new Vec3()),p=>p.Z,p=>
            { True(p.Z>=.25f && p.Z<=1.5f);projected++;return new Vec2(p.X/p.Z,p.Y/p.Z); },.25f,1.5f);
            True(projected>=3);True(coverage.SampleParameters(new Vec2(-10,.1f),new Vec2(10,.1f),4096).Count>2);
            var hidden=SurfaceScreenProjection.Build(mesh,new RestTransform(1,new Vec3(0,0,-10)),p=>p.Z,p=>throw new Exception("Behind-camera triangle was projected"),.25f,1.5f);
            Equal(0,hidden.SampleParameters(new Vec2(-1,0),new Vec2(1,0),4096).Count);
        });
        Test("projected boundaries obey budgets and fail the gesture without partial commit",()=>
        {
            var triangles=new List<Vec2>();
            for(int i=0;i<20;i++) triangles.AddRange(new[]{new Vec2(i,0),new Vec2(i+.1f,0),new Vec2(i,.1f)});
            var coverage=new SurfaceScreenCoverage(triangles);
            Expect("STROKE_BUDGET_EXCEEDED",()=>coverage.SampleParameters(new Vec2(-1,.02f),new Vec2(21,.02f),8));
            var surface=new SurfacePaintMesh(RayTriangle(),new RestTransform(1,new Vec3()));
            var sampler=new SurfaceStrokeSampler(surface,_=>null,2,coverage);
            sampler.Append(new Vec2(-8151,.02f));sampler.Append(new Vec2(-1,.02f));
            Expect("STROKE_BUDGET_EXCEEDED",()=>sampler.Append(new Vec2(21,.02f)));Expect("STROKE_INVALIDATED",()=>sampler.Snapshot());
            Expect("SCREEN_MESH_BUDGET_EXCEEDED",()=>new SurfaceScreenCoverage(new[]{new Vec2()}));
        });
    }
}
