using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Paints linear coverage toward a byte target, once per stroke rather than once per sample.</summary>
    public static class PaintMaskStroke
    {
        public static PaintMask Apply(PaintMask mask,IEnumerable<Vec2> path,float radiusPixels,byte target,float strength)
        {
            Checks.Require(mask!=null,"PAINT_MASK_REQUIRED","Add a mask before painting it.");
            Checks.Finite(strength);
            Checks.Require(strength>=0 && strength<=1,"INVALID_MASK_STRENGTH","Mask strength must be 0..1.");
            var coverage=BrushCoverage.Rasterize(mask.Width,mask.Height,path,radiusPixels);
            return ApplyCoverage(mask,coverage,target,strength);
        }
        public static PaintMask ApplyPaths(PaintMask mask,PaintStrokePath path,float radiusPixels,byte target,float strength)
        {
            Checks.Require(mask!=null,"PAINT_MASK_REQUIRED","Add a mask before painting it.");
            Checks.Finite(strength);Checks.Require(strength>=0 && strength<=1,"INVALID_MASK_STRENGTH","Mask strength must be 0..1.");
            return ApplyCoverage(mask,BrushCoverage.RasterizePaths(mask.Width,mask.Height,path,radiusPixels),target,strength);
        }
        static PaintMask ApplyCoverage(PaintMask mask,float[] coverage,byte target,float strength)
        {
            if(strength==0) return mask;
            var values=mask.CopyCoverage();bool changed=false;
            for(int i=0;i<values.Length;i++)
            {
                byte value=(byte)Math.Max(0,Math.Min(255,Math.Floor(values[i]+(target-values[i])*(double)coverage[i]*strength+.5)));
                changed|=value!=values[i];values[i]=value;
            }
            return changed ? new PaintMask(mask.Width,mask.Height,values) : mask;
        }
    }
}
