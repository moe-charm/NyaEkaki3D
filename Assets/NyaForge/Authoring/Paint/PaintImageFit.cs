using System;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Aspect-preserving centered fit, with transparent padding and linear premultiplied filtering.</summary>
    public static class PaintImageFit
    {
        public static PaintImage Apply(PaintImage source,int width,int height)
        {
            Checks.Require(source!=null,"INVALID_IMAGE_SIZE","Source image is required.");PaintImage.ValidateDimensions(width,height);
            if(source.Width==width && source.Height==height) return source;
            double scale=Math.Min((double)width/source.Width,(double)height/source.Height);
            int fittedWidth=Math.Max(1,Math.Min(width,(int)Math.Round(source.Width*scale)));
            int fittedHeight=Math.Max(1,Math.Min(height,(int)Math.Round(source.Height*scale)));
            int offsetX=(width-fittedWidth)/2,offsetY=(height-fittedHeight)/2;
            var edit=new PaintImage.Editor(new PaintImage(width,height,new Rgba32()));
            for(int y=0;y<fittedHeight;y++) for(int x=0;x<fittedWidth;x++)
            {
                // Area filtering when shrinking; bilinear interpolation when enlarging.
                double left=(double)x*source.Width/fittedWidth,right=(double)(x+1)*source.Width/fittedWidth;
                double bottom=(double)y*source.Height/fittedHeight,top=(double)(y+1)*source.Height/fittedHeight;
                bool shrink=source.Width>fittedWidth || source.Height>fittedHeight;
                double cx=(left+right)/2-.5,cy=(bottom+top)/2-.5;
                int x0=shrink ? (int)Math.Floor(left) : (int)Math.Floor(cx),x1=shrink ? (int)Math.Ceiling(right)-1 : x0+1;
                int y0=shrink ? (int)Math.Floor(bottom) : (int)Math.Floor(cy),y1=shrink ? (int)Math.Ceiling(top)-1 : y0+1;
                double weight=0,alpha=0,r=0,g=0,b=0;
                for(int sy=y0;sy<=y1;sy++) for(int sx=x0;sx<=x1;sx++)
                {
                    double wx=shrink ? Math.Min(right,sx+1)-Math.Max(left,sx) : sx==x0 ? 1-(cx-x0) : cx-x0;
                    double wy=shrink ? Math.Min(top,sy+1)-Math.Max(bottom,sy) : sy==y0 ? 1-(cy-y0) : cy-y0;
                    double w=wx*wy;var p=source.GetPixel(Math.Max(0,Math.Min(source.Width-1,sx)),Math.Max(0,Math.Min(source.Height-1,sy)));
                    double a=p.A/255.0*w;weight+=w;alpha+=a;r+=PaintBlend.Decode(p.R)*a;g+=PaintBlend.Decode(p.G)*a;b+=PaintBlend.Decode(p.B)*a;
                }
                var color=alpha==0 ? new Rgba32() : new Rgba32(PaintBlend.Encode(r/alpha),PaintBlend.Encode(g/alpha),PaintBlend.Encode(b/alpha),(byte)Math.Max(0,Math.Min(255,Math.Round(alpha/weight*255))));
                edit.Set(x+offsetX,y+offsetY,color);
            }
            return edit.Finish();
        }
    }
}
