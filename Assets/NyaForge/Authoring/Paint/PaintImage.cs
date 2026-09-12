using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Paint
{
    public readonly struct Rgba32
    {
        public readonly byte R, G, B, A;
        public Rgba32(byte r, byte g, byte b, byte a = 255) { R = r; G = g; B = b; A = a; }
    }

    /// <summary>Immutable base-color image: sRGB RGB, straight linear alpha, bottom-left row origin.</summary>
    public sealed class PaintImage
    {
        public const int TileSize = 64, MaxDimension = 1024;
        readonly byte[][] tiles;
        public int Width { get; }
        public int Height { get; }
        internal int Columns => (Width + TileSize - 1) / TileSize;

        public PaintImage(int width, int height, Rgba32 fill)
        {
            ValidateDimensions(width, height); Width = width; Height = height;
            tiles = new byte[Columns * ((height + TileSize - 1) / TileSize)][];
            for (int i = 0; i < tiles.Length; i++)
            {
                var tile = tiles[i] = new byte[TileSize * TileSize * 4];
                for (int p = 0; p < tile.Length; p += 4) { tile[p] = fill.R; tile[p+1] = fill.G; tile[p+2] = fill.B; tile[p+3] = fill.A; }
            }
        }
        PaintImage(PaintImage source, Dictionary<int, byte[]> changed)
        {
            Width = source.Width; Height = source.Height;
            tiles = (byte[][])source.tiles.Clone();
            foreach (var pair in changed) tiles[pair.Key] = pair.Value;
        }
        internal static void ValidateDimensions(int width, int height) => Checks.Require(width > 0 && height > 0 && width <= MaxDimension && height <= MaxDimension, "IMAGE_BUDGET_EXCEEDED", "Paint dimensions must be 1..1024.");
        int TileIndex(int x, int y) => y / TileSize * Columns + x / TileSize;
        static int Offset(int x, int y) => ((y % TileSize) * TileSize + x % TileSize) * 4;
        public Rgba32 GetPixel(int x, int y)
        {
            Checks.Require(x >= 0 && x < Width && y >= 0 && y < Height, "PIXEL_OUT_OF_RANGE", "Pixel outside image.");
            var tile = tiles[TileIndex(x,y)]; int p = Offset(x,y);
            return new Rgba32(tile[p],tile[p+1],tile[p+2],tile[p+3]);
        }
        public byte[] CopyRgba()
        {
            var bytes = new byte[Width * Height * 4];
            for (int y = 0; y < Height; y++) for (int x = 0; x < Width; x++)
            {
                int p = (y * Width + x) * 4; var c = GetPixel(x,y);
                bytes[p] = c.R; bytes[p+1] = c.G; bytes[p+2] = c.B; bytes[p+3] = c.A;
            }
            return bytes;
        }
        public static PaintImage FromRgbaBottomLeft(int width,int height,byte[] bytes)
        {
            ValidateDimensions(width,height);
            Checks.Require(bytes!=null && bytes.Length==width*height*4,"INVALID_IMAGE_SIZE","RGBA bytes must match image dimensions.");
            var edit=new Editor(new PaintImage(width,height,new Rgba32()));
            for(int y=0;y<height;y++) for(int x=0;x<width;x++)
            {
                int p=(y*width+x)*4;edit.Set(x,y,new Rgba32(bytes[p],bytes[p+1],bytes[p+2],bytes[p+3]));
            }
            return edit.Finish();
        }
        internal sealed class Editor
        {
            readonly PaintImage source;
            readonly Dictionary<int,byte[]> changed = new Dictionary<int,byte[]>();
            bool finished;
            internal Editor(PaintImage source) { this.source = source; }
            internal void Set(int x, int y, Rgba32 value)
            {
                Checks.Require(!finished, "IMAGE_EDIT_FINISHED", "Image edit has already committed.");
                int index = source.TileIndex(x,y), p = Offset(x,y);
                if (!changed.TryGetValue(index,out var tile)) changed.Add(index,tile = (byte[])source.tiles[index].Clone());
                tile[p] = value.R; tile[p+1] = value.G; tile[p+2] = value.B; tile[p+3] = value.A;
            }
            internal PaintImage Finish() { finished = true; return changed.Count == 0 ? source : new PaintImage(source,changed); }
        }
    }
}
