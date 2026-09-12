namespace NyaForge.Authoring.Paint
{
    /// <summary>Opaque grayscale visualization only; never use these sRGB display bytes as editable mask data.</summary>
    public static class PaintMaskDisplay
    {
        public static PaintImage Create(PaintMask mask)
        {
            Checks.Require(mask!=null,"PAINT_MASK_REQUIRED","A mask is required for its display image.");
            var edit=new PaintImage.Editor(new PaintImage(mask.Width,mask.Height,new Rgba32(0,0,0)));
            for(int y=0;y<mask.Height;y++) for(int x=0;x<mask.Width;x++)
            {
                byte value=mask.GetCoverage(x,y);edit.Set(x,y,new Rgba32(value,value,value));
            }
            return edit.Finish();
        }
    }
}
