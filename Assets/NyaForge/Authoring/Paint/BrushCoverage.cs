using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Paint
{
    internal static class BrushCoverage
    {
        public const int MaxPoints = 1024;
        public const long MaxPixelVisits = 16L * 1024 * 1024;
        public static float[] Rasterize(int width, int height, IEnumerable<Vec2> path, float radiusPixels)
            => RasterizePaths(width,height,new PaintStrokePath(new[]{path}),radiusPixels);
        public static float[] RasterizePaths(int width,int height,PaintStrokePath path,float radiusPixels)
        {
            PaintImage.ValidateDimensions(width,height);
            Checks.Require(path != null, "INVALID_STROKE", "UV stroke path is required.");
            Checks.Finite(radiusPixels);
            Checks.Require(radiusPixels >= .5f && radiusPixels <= 512, "INVALID_BRUSH", "Brush radius must be 0.5..512 pixels.");
            var segments = new List<(double ax,double ay,double bx,double by,int x0,int y0,int x1,int y1)>();
            long visits = 0;
            foreach(var points in path.Sections)
            for (int i = 0; i < Math.Max(1, points.Count - 1); i++)
            {
                var a = points[i]; var b = points[Math.Min(i+1,points.Count-1)];
                double ax = a.X * width, ay = a.Y * height, bx = b.X * width, by = b.Y * height;
                int x0 = Math.Max(0,(int)Math.Floor(Math.Min(ax,bx)-radiusPixels-1)), y0 = Math.Max(0,(int)Math.Floor(Math.Min(ay,by)-radiusPixels-1));
                int x1 = Math.Min(width-1,(int)Math.Ceiling(Math.Max(ax,bx)+radiusPixels+1)), y1 = Math.Min(height-1,(int)Math.Ceiling(Math.Max(ay,by)+radiusPixels+1));
                visits += (long)(x1-x0+1)*(y1-y0+1);
                Checks.Require(visits <= MaxPixelVisits, "STROKE_BUDGET_EXCEEDED", "Stroke exceeds pixel work budget.");
                segments.Add((ax,ay,bx,by,x0,y0,x1,y1));
            }
            // Union coverage makes opacity independent of how frequently the input path is sampled.
            var coverage = new float[width * height];
            foreach (var s in segments)
            {
                double dx=s.bx-s.ax,dy=s.by-s.ay,length=dx*dx+dy*dy;
                for(int y=s.y0;y<=s.y1;y++) for(int x=s.x0;x<=s.x1;x++)
                {
                    double px=x+.5-s.ax,py=y+.5-s.ay;
                    double t=length==0 ? 0 : Math.Max(0,Math.Min(1,(px*dx+py*dy)/length));
                    double ex=px-t*dx,ey=py-t*dy;
                    float c=(float)Math.Max(0,Math.Min(1,radiusPixels+.5-Math.Sqrt(ex*ex+ey*ey)));
                    int index=y*width+x; if(c>coverage[index]) coverage[index]=c;
                }
            }
            return coverage;
        }
    }
}
