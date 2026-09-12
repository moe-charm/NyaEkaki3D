using System;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Immutable linear coverage mask, one byte per pixel, bottom-left rows.</summary>
    public sealed class PaintMask
    {
        readonly byte[] values;
        public int Width { get; }
        public int Height { get; }
        public PaintMask(int width,int height,byte[] coverage)
        {
            PaintImage.ValidateDimensions(width,height);
            Checks.Require(coverage != null && coverage.Length == width*height,"INVALID_MASK_SIZE","Mask coverage must match its dimensions.");
            Width=width; Height=height; values=(byte[])coverage.Clone();
        }
        public byte GetCoverage(int x,int y)
        {
            Checks.Require(x>=0 && x<Width && y>=0 && y<Height,"PIXEL_OUT_OF_RANGE","Pixel outside mask.");
            return values[y*Width+x];
        }
        public byte[] CopyCoverage() => (byte[])values.Clone();
    }
}
