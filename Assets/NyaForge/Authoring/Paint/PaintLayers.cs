using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Immutable bottom-to-top stack. Composite is derived, never a second editable source.</summary>
    public sealed class PaintLayers
    {
        public const int MaxLayers=16;
        public const long MaxLayerBytes=32L*1024*1024;
        public int Width { get; }
        public int Height { get; }
        public IReadOnlyList<PaintLayer> Layers { get; }
        public PaintLayers(int width,int height,IEnumerable<PaintLayer> layers)
        {
            PaintImage.ValidateDimensions(width,height);
            Checks.Require(layers!=null,"INVALID_PAINT_LAYERS","Layer collection is required.");
            var copy=layers.Take(MaxLayers+1).ToArray();
            Checks.Require(copy.Length<=MaxLayers,"PAINT_LAYER_BUDGET","Too many paint layers.");
            var ids=new HashSet<string>(StringComparer.Ordinal); int masks=0;
            foreach(var layer in copy)
            {
                Checks.Require(layer!=null && ids.Add(layer.Id),"INVALID_PAINT_LAYERS","Layer IDs must be unique.");
                Checks.Require(layer.Image.Width==width && layer.Image.Height==height,"INVALID_IMAGE_SIZE","All layer dimensions must match the canvas.");
                if(layer.Mask!=null) masks++;
            }
            ValidateBudget(width,height,copy.Length,masks);
            Width=width; Height=height; Layers=Array.AsReadOnly(copy);
        }
        internal static void ValidateBudget(int width,int height,int count,int masks)
        {
            PaintImage.ValidateDimensions(width,height);
            Checks.Require(count>=0 && count<=MaxLayers && masks>=0 && masks<=count,"PAINT_LAYER_BUDGET","Invalid layer count.");
            long tile=PaintImage.TileSize;
            long bytes=((width+tile-1)/tile)*((height+tile-1)/tile)*tile*tile*4*count+(long)width*height*masks;
            Checks.Require(bytes<=MaxLayerBytes,"PAINT_LAYER_BUDGET","Layer images and masks exceed memory budget.");
        }
        public PaintLayers Replace(PaintLayer layer)
        {
            Checks.Require(layer!=null && Layers.Any(l=>l.Id==layer.Id),"PAINT_LAYER_NOT_FOUND","Layer no longer exists.");
            return new PaintLayers(Width,Height,Layers.Select(l=>l.Id==layer.Id ? layer : l));
        }
        public PaintImage Composite()
        {
            var result=new PaintImage(Width,Height,new Rgba32());
            bool applied=false;
            foreach(var layer in Layers)
            {
                if(!layer.Visible || layer.Opacity==0) continue;
                // A legacy image promoted to one full-opacity layer must retain all RGBA bytes,
                // including RGB under zero alpha. Share that immutable first layer directly.
                if(!applied && layer.Opacity==1 && layer.Mask==null) { result=layer.Image;applied=true;continue; }
                var edit=new PaintImage.Editor(result);
                for(int y=0;y<Height;y++) for(int x=0;x<Width;x++)
                {
                    var over=layer.Image.GetPixel(x,y);
                    float coverage=layer.Opacity*(layer.Mask==null ? 1 : layer.Mask.GetCoverage(x,y)/255f);
                    if(coverage>0 && over.A>0) edit.Set(x,y,PaintBlend.Over(result.GetPixel(x,y),over,coverage));
                }
                result=edit.Finish();
                applied=true;
            }
            return result;
        }
    }
}
