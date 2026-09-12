using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using Newtonsoft.Json;

internal static class SurfacePaintProfile
{
    // Opt-in measurement, not a wall-clock assertion or a replacement for Player acceptance.
    internal static int Run(string directory)
    {
        var rows=new List<object>();
        foreach(int cells in new[]{32,64,128,256})
        {
            var positions=new List<Vec3>();var uv=new List<Vec2>();var indices=new List<int>();
            for(int y=0;y<=cells;y++) for(int x=0;x<=cells;x++)
            { positions.Add(new Vec3((float)x/cells,(float)y/cells,0));uv.Add(new Vec2((float)x/cells,(float)y/cells)); }
            for(int y=0;y<cells;y++) for(int x=0;x<cells;x++)
            { int a=y*(cells+1)+x,b=a+1,c=a+cells+1,d=c+1;indices.AddRange(new[]{a,b,c,b,d,c}); }
            var mesh=new MeshData(positions.ToArray(),Array.Empty<Vec3>(),Array.Empty<Vec4>(),uv.ToArray(),new[]{indices.ToArray()});
            var transform=new RestTransform(1,new Vec3());var watch=Stopwatch.StartNew();
            var surface=new SurfacePaintMesh(mesh,transform);double rayBuildMs=watch.Elapsed.TotalMilliseconds;
            watch.Restart();var coverage=SurfaceScreenProjection.Build(mesh,transform,p=>1-p.Z,p=>new Vec2(p.X*1000,p.Y*1000),.01f,10);
            double coverageBuildMs=watch.Elapsed.TotalMilliseconds;
            foreach(float length in new[]{50f,800f})
            {
                var sampler=new SurfaceStrokeSampler(surface,p=>surface.Raycast(new Vec3(p.X/1000,p.Y/1000,1),new Vec3(0,0,-1)),2,coverage);
                string failure=null;int sections=0,retainedPoints=0;watch.Restart();
                try { sampler.Append(new Vec2(100,431));sampler.Append(new Vec2(100+length,431));var stroke=sampler.Snapshot();sections=stroke.Sections.Count;retainedPoints=stroke.PointCount; }
                catch(Exception e) { failure=e.Message; }
                rows.Add(new { cells,triangles=mesh.TriangleCount,length,rayBuildMs,coverageBuildMs,strokeMs=watch.Elapsed.TotalMilliseconds,
                    samples=sampler.RaySampleCount,sourcePoints=sampler.PointCount,retainedPoints,sections,failure });
            }
        }
        string path=Path.Combine(directory,"surface-profile.json");File.WriteAllText(path,JsonConvert.SerializeObject(rows,Formatting.Indented));
        Console.WriteLine(File.ReadAllText(path));Console.WriteLine("Profile: "+path);return 0;
    }
}
