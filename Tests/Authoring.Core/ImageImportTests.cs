using System;
using System.IO;
using System.Linq;
using System.Text;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static byte[] ImportChunk(string type,byte[] payload)
    {
        using var stream=new MemoryStream();
        void U32(uint n) { stream.WriteByte((byte)(n>>24));stream.WriteByte((byte)(n>>16));stream.WriteByte((byte)(n>>8));stream.WriteByte((byte)n); }
        U32((uint)payload.Length);byte[] body=Encoding.ASCII.GetBytes(type).Concat(payload).ToArray();stream.Write(body);
        uint crc=0xffffffff;foreach(byte b in body) { crc^=b;for(int i=0;i<8;i++) crc=(crc>>1)^((crc&1)==0 ? 0 : 0xedb88320u); }
        U32(crc^0xffffffff);return stream.ToArray();
    }
    static void RunImageImportTests()
    {
        Test("PNG preflight validates indexed palette and transparency structure before native decoding",()=>
        {
            var png=PaintPng.Encode(new PaintImage(2,2,new Rgba32()));
            byte[] Variant(int color,int depth,params byte[][] chunks)
            {
                var header=png.Skip(16).Take(13).ToArray();header[8]=(byte)depth;header[9]=(byte)color;
                return png.Take(8).Concat(ImportChunk("IHDR",header)).Concat(chunks.SelectMany(c=>c)).Concat(png.Skip(46)).ToArray();
            }
            var palette=ImportChunk("PLTE",new byte[]{1,2,3,4,5,6});var transparency=ImportChunk("tRNS",new byte[]{0,128});
            // Pixel bytes remain RGBA in this structural-only test. Only the Player tests exercise decoding.
            Equal(2,PaintPngInput.Read(Variant(3,1,palette,transparency)).Width);
            Equal(2,PaintPngInput.Read(Variant(0,8,ImportChunk("tRNS",new byte[]{0,1}))).Width);
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(3,1)));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(3,1,transparency,palette)));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(3,1,ImportChunk("PLTE",new byte[9]))));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(3,1,palette,ImportChunk("tRNS",new byte[3]))));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(3,1,palette,transparency,transparency)));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(0,8,palette)));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(4,8,palette)));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(6,8,transparency)));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(0,8,ImportChunk("tRNS",new byte[1]))));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Variant(2,8,transparency)));
            var late=png.Take(png.Length-12).Concat(ImportChunk("tRNS",new byte[2])).Concat(png.Skip(png.Length-12)).ToArray();
            Expect("INVALID_PNG",()=>PaintPngInput.Read(late));
        });
        Test("image fit uses linear premultiplied filtering, area shrink and transparent aspect padding",()=>
        {
            var source=PaintImage.FromRgbaBottomLeft(2,1,new byte[]{255,0,0,255,0,0,255,0});
            var half=PaintImageFit.Apply(source,1,1).GetPixel(0,0);Equal((byte)255,half.R);Equal((byte)0,half.B);Equal((byte)128,half.A);
            var padded=PaintImageFit.Apply(source,4,4);Equal((byte)0,padded.GetPixel(0,0).A);Equal((byte)0,padded.GetPixel(0,3).A);Equal((byte)255,padded.GetPixel(0,1).A);
            var bw=PaintImage.FromRgbaBottomLeft(2,1,new byte[]{0,0,0,255,255,255,255,255});Equal((byte)188,PaintImageFit.Apply(bw,1,1).GetPixel(0,0).R);
            var checker=Enumerable.Range(0,16).SelectMany(i=>new byte[]{(byte)(i%2*255),(byte)(i%2*255),(byte)(i%2*255),255}).ToArray();
            Equal((byte)188,PaintImageFit.Apply(PaintImage.FromRgbaBottomLeft(4,4,checker),1,1).GetPixel(0,0).R);
            True(ReferenceEquals(source,PaintImageFit.Apply(source,2,1)));Equal((byte)255,source.GetPixel(1,0).B);
            var rgba=new byte[]{1,2,3,4};var image=PaintImage.FromRgbaBottomLeft(1,1,rgba);rgba[0]=255;Equal((byte)1,image.GetPixel(0,0).R);
            Expect("INVALID_IMAGE_SIZE",()=>PaintImage.FromRgbaBottomLeft(2,1,rgba));
        });
        Test("PNG import preflight owns bytes and rejects corruption, dimensions, profiles and animation",()=>
        {
            byte[] png=PaintPng.Encode(new PaintImage(2,3,new Rgba32(12,34,56,78)));var read=PaintPngInput.Read(png);
            Equal(2,read.Width);Equal(3,read.Height);True(read.DeclaredSrgb);byte first=read.CopyBytes()[0];png[0]=0;Equal(first,read.CopyBytes()[0]);
            png=PaintPng.Encode(new PaintImage(2,3,new Rgba32()));var broken=(byte[])png.Clone();broken[broken.Length-1]^=1;
            Expect("INVALID_PNG",()=>PaintPngInput.Read(broken));Expect("INVALID_PNG",()=>PaintPngInput.Read(png.Take(png.Length-1).ToArray()));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(png.Concat(new byte[]{0}).ToArray()));
            byte[] Insert(string type,byte[] value)=>png.Take(33).Concat(ImportChunk(type,value)).Concat(png.Skip(33)).ToArray();
            Expect("UNSUPPORTED_IMAGE_PROFILE",()=>PaintPngInput.Read(Insert("iCCP",new byte[]{0,0})));
            Expect("UNSUPPORTED_IMAGE_ANIMATION",()=>PaintPngInput.Read(Insert("acTL",new byte[8])));
            Expect("INVALID_PNG",()=>PaintPngInput.Read(Insert("ZZZZ",Array.Empty<byte>())));
            byte[] header=png.Skip(16).Take(13).ToArray();header[2]=8;header[3]=0;
            var huge=png.Take(8).Concat(ImportChunk("IHDR",header)).Concat(png.Skip(33)).ToArray();
            Expect("IMAGE_BUDGET_EXCEEDED",()=>PaintPngInput.Read(huge));
            // Remove the encoder's sRGB declaration before testing a non-sRGB gamma.
            var withoutSrgb=png.Take(33).Concat(png.Skip(46)).ToArray();
            var linear=withoutSrgb.Take(33).Concat(ImportChunk("gAMA",new byte[]{0,1,134,160})).Concat(withoutSrgb.Skip(33)).ToArray();
            Expect("UNSUPPORTED_IMAGE_PROFILE",()=>PaintPngInput.Read(linear));True(!PaintPngInput.Read(withoutSrgb).DeclaredSrgb);
        });
    }
}
