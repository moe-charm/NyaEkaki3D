using System.IO;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    internal static class PaintMaskCodec
    {
        const int Magic=0x4d46594e; // NYFM
        internal static byte[] Write(PaintMask mask)
        {
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream))
            {
                writer.Write(Magic);writer.Write(1);writer.Write(mask.Width);writer.Write(mask.Height);
                writer.Write(1); // linear coverage8, bottom-left rows
                writer.Write(mask.CopyCoverage());return stream.ToArray();
            }
        }
        internal static PaintMask Read(byte[] bytes,int expectedWidth,int expectedHeight)
        {
            Checks.Require(bytes!=null && bytes.Length>=20 && bytes.Length<=20+PaintImage.MaxDimension*PaintImage.MaxDimension,"INVALID_MASK_BLOB","Invalid mask size.");
            using(var stream=new MemoryStream(bytes,false)) using(var reader=new BinaryReader(stream))
            {
                Checks.Require(reader.ReadInt32()==Magic,"INVALID_MASK_BLOB","Not a paint mask.");
                Checks.Require(reader.ReadInt32()==1,"UNSUPPORTED_FORMAT","Unsupported mask version.");
                int width=reader.ReadInt32(),height=reader.ReadInt32();PaintImage.ValidateDimensions(width,height);
                Checks.Require(width==expectedWidth && height==expectedHeight,"INVALID_MASK_SIZE","Mask dimensions differ from canvas.");
                Checks.Require(reader.ReadInt32()==1,"UNSUPPORTED_FORMAT","Unsupported mask profile.");
                Checks.Require(bytes.Length==20+(long)width*height,"INVALID_MASK_BLOB","Mask length differs from dimensions.");
                return new PaintMask(width,height,reader.ReadBytes(width*height));
            }
        }
    }
}
