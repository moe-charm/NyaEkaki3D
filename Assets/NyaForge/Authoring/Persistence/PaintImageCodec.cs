using System.IO;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring
{
    internal static class PaintImageCodec
    {
        const int Magic = 0x4946594e; // NYFI
        internal static byte[] Write(PaintImage image)
        {
            using(var stream=new MemoryStream()) using(var writer=new BinaryWriter(stream))
            {
                writer.Write(Magic);writer.Write(1);writer.Write(image.Width);writer.Write(image.Height);
                writer.Write(1); // Base color, sRGB RGB, straight alpha, bottom-left rows.
                writer.Write(image.CopyRgba());return stream.ToArray();
            }
        }
        internal static PaintImage Read(byte[] bytes,int expectedWidth=0,int expectedHeight=0)
        {
            Checks.Require(bytes != null && bytes.Length >= 20 && bytes.Length <= 20+PaintImage.MaxDimension*PaintImage.MaxDimension*4,"INVALID_IMAGE_BLOB","Invalid image blob length.");
            using(var stream=new MemoryStream(bytes,false)) using(var reader=new BinaryReader(stream))
            {
                Checks.Require(reader.ReadInt32()==Magic,"INVALID_IMAGE_BLOB","Not a paint image.");
                Checks.Require(reader.ReadInt32()==1,"UNSUPPORTED_FORMAT","Unsupported image version.");
                int width=reader.ReadInt32(),height=reader.ReadInt32();PaintImage.ValidateDimensions(width,height);
                Checks.Require(expectedWidth==0 || (width==expectedWidth && height==expectedHeight),"INVALID_IMAGE_SIZE","Image dimensions differ from the owning canvas.");
                Checks.Require(reader.ReadInt32()==1,"UNSUPPORTED_FORMAT","Unsupported image channel/color profile.");
                Checks.Require(bytes.Length==20+(long)width*height*4,"INVALID_IMAGE_BLOB","Image payload length differs from dimensions.");
                var image=new PaintImage(width,height,new Rgba32());var edit=new PaintImage.Editor(image);
                for(int y=0;y<height;y++) for(int x=0;x<width;x++) edit.Set(x,y,new Rgba32(reader.ReadByte(),reader.ReadByte(),reader.ReadByte(),reader.ReadByte()));
                return edit.Finish();
            }
        }
    }
    public static class PaintImageStore
    {
        public static string Write(string directory,PaintImage image)
        {
            byte[] bytes=PaintImageCodec.Write(image);string hash=Checks.Hash(bytes);directory=Storage.DirectoryPath(directory);
            using(Storage.Lock(directory)) Storage.WriteBlob(directory,hash,bytes);return hash;
        }
        public static PaintImage Read(string directory,string hash)
        { Checks.HashText(hash);return PaintImageCodec.Read(Storage.ReadBlob(Storage.DirectoryPath(directory),hash)); }
    }
}
