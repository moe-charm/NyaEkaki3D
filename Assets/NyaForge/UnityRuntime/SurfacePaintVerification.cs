using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

namespace NyaForge.UnityRuntime
{
    internal static class SurfacePaintVerification
    {
        internal static void Verify(List<string> checks)
        {
            var mesh=new MeshData(new[]{new Vec3(0,0,0),new Vec3(1,0,0),new Vec3(0,1,0)},Array.Empty<Vec3>(),Array.Empty<Vec4>(),
                new[]{new Vec2(0,0),new Vec2(1,0),new Vec2(0,1)},new[]{new[]{0,1,2}});
            foreach(float scale in new[]{1f,100f})
            {
                var surface=new SurfacePaintMesh(mesh,new RestTransform(scale,new Vec3(0,0,2)));
                var hit=surface.Raycast(new Vec3(.2f*scale,.3f*scale,5),new Vec3(0,0,-5));
                if(hit==null || !hit.IsUvInRange || Math.Abs(hit.Uv.X-.2f)>1e-6 || Math.Abs(hit.Uv.Y-.3f)>1e-6 || Math.Abs(hit.Distance-3)>1e-8)
                    throw new InvalidOperationException("Player surface UV ray differs at scale "+scale);
            }
            checks.Add("Surface paint ray API: world distance and barycentric UV at scale 1/100");
            var path=new PaintStrokePath(new[]{new[]{new Vec2(.2f,.5f)},new[]{new Vec2(.8f,.5f)}});
            var painted=PaintStroke.ApplyPaths(new PaintImage(64,64,new Rgba32(0,0,0)),path,3,new Rgba32(255,0,0,128));
            var mask=PaintMaskStroke.ApplyPaths(new PaintMask(64,64,Enumerable.Repeat((byte)255,4096).ToArray()),path,3,0,.5f);
            if(painted.GetPixel(32,32).R!=0 || painted.GetPixel(12,32).R==0 || mask.GetCoverage(32,32)!=255 || mask.GetCoverage(12,32)!=128)
                throw new InvalidOperationException("Player disconnected UV gesture differs");
            checks.Add("Disconnected UV gesture API: no bridge between sections; color and linear mask coverage");
        }
    }
}
