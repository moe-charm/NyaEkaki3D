using System.Collections.Generic;

namespace NyaForge.Authoring.Paint
{
    public static class PaintStroke
    {
        public const int MaxPoints = BrushCoverage.MaxPoints;
        public const long MaxPixelVisits = BrushCoverage.MaxPixelVisits;
        public static PaintImage Apply(PaintImage image, IEnumerable<Vec2> path, float radiusPixels, Rgba32 color)
        {
            Checks.Require(image != null,"INVALID_STROKE","Image is required.");
            var coverage=BrushCoverage.Rasterize(image.Width,image.Height,path,radiusPixels);
            return ApplyCoverage(image,coverage,color);
        }
        public static PaintImage ApplyPaths(PaintImage image,PaintStrokePath path,float radiusPixels,Rgba32 color)
        {
            Checks.Require(image!=null,"INVALID_STROKE","Image is required.");
            return ApplyCoverage(image,BrushCoverage.RasterizePaths(image.Width,image.Height,path,radiusPixels),color);
        }
        static PaintImage ApplyCoverage(PaintImage image,float[] coverage,Rgba32 color)
        {
            if(color.A==0) return image;
            var edit=new PaintImage.Editor(image);
            for(int y=0;y<image.Height;y++) for(int x=0;x<image.Width;x++)
            {
                float amount=coverage[y*image.Width+x];
                if(amount>0) edit.Set(x,y,PaintBlend.Over(image.GetPixel(x,y),color,amount));
            }
            return edit.Finish();
        }
    }
}
