using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunPaintPathSimplifierTests()
    {
        Test("paint simplification keeps endpoints, retracing turns and disconnected sections",()=>
        {
            var line=Enumerable.Range(0,3000).Select(i=>new Vec2(.1f+i*.8f/2999,.5f)).ToArray();
            var reduced=PaintPathSimplifier.Reduce(new[]{line});Equal(2,reduced.PointCount);
            Equal(line[0],reduced.Sections[0][0]);Equal(line.Last(),reduced.Sections[0][1]);
            var path=PaintPathSimplifier.Reduce(new[]{new[]{new Vec2(.1f,.5f),new Vec2(.9f,.5f),new Vec2(.5f,.5f)},new[]{new Vec2(.1f,.1f)}});
            Equal(2,path.Sections.Count);Equal(4,path.PointCount);
            var pixels=PaintStroke.ApplyPaths(new PaintImage(128,128,new Rgba32(0,0,0,0)),path,1,new Rgba32(255,0,0));
            True(pixels.GetPixel(114,64).A>0);Equal((byte)0,pixels.GetPixel(12,32).A);
        });
        Test("simplified curved strokes stay within the UV error contract and closely match raster coverage",()=>
        {
            var source=Enumerable.Range(0,600).Select(i=> { double angle=i*Math.PI/599;return new Vec2((float)(.5+.35*Math.Cos(angle)),(float)(.3+.35*Math.Sin(angle))); }).ToArray();
            var reduced=PaintPathSimplifier.Reduce(new[]{source});True(reduced.PointCount<source.Length/2);
            var vertices=reduced.Sections[0];
            foreach(var point in source)
            {
                double best=double.MaxValue;
                for(int i=1;i<vertices.Count;i++)
                {
                    var a=vertices[i-1];var b=vertices[i];double dx=(double)b.X-a.X,dy=(double)b.Y-a.Y;
                    double t=Math.Max(0,Math.Min(1,(((double)point.X-a.X)*dx+((double)point.Y-a.Y)*dy)/(dx*dx+dy*dy)));
                    best=Math.Min(best,Math.Sqrt(Math.Pow(point.X-a.X-t*dx,2)+Math.Pow(point.Y-a.Y-t*dy,2)));
                }
                True(best<=PaintPathSimplifier.UvTolerance+1e-9);
            }
            var image=new PaintImage(256,256,new Rgba32(0,0,0,0));var color=new Rgba32(200,20,80);
            var before=PaintStroke.ApplyPaths(image,new PaintStrokePath(new[]{source}),2,color).CopyRgba();
            var after=PaintStroke.ApplyPaths(image,reduced,2,color).CopyRgba();
            for(int p=3;p<before.Length;p+=4) True(Math.Abs(before[p]-after[p])<=10);
        });
        Test("simplification retains raw and final budgets rather than raising command capacity",()=>
        {
            var complex=Enumerable.Range(0,1100).Select(i=>new Vec2(.05f+i*.0006f,i%2==0 ? .05f : .2f)).ToArray();
            Expect("STROKE_BUDGET_EXCEEDED",()=>PaintPathSimplifier.Reduce(new[]{complex}));
            Expect("STROKE_BUDGET_EXCEEDED",()=>PaintPathSimplifier.Reduce(new[]{Enumerable.Repeat(new Vec2(.5f,.5f),4097)}));
        });
        Test("an irreducible final path invalidates its sampler and preserves earlier snapshots",()=>
        {
            var surface=new SurfacePaintMesh(RayTriangle(),new RestTransform(1,new Vec3()));
            var sampler=new SurfaceStrokeSampler(surface,p=>surface.Raycast(new Vec3(.05f+p.X*.0006f,((int)p.X)%2==0 ? .05f : .2f,1),new Vec3(0,0,-1)));
            sampler.Append(new Vec2(0,0));var frozen=sampler.Snapshot();
            for(int i=1;i<1100;i++) sampler.Append(new Vec2(i,0));
            Expect("STROKE_BUDGET_EXCEEDED",()=>sampler.Snapshot());Expect("STROKE_INVALIDATED",()=>sampler.Append(new Vec2(1100,0)));Equal(1,frozen.PointCount);
        });
    }
}
