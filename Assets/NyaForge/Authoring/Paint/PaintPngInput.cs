using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace NyaForge.Authoring.Paint
{
    /// <summary>Bounded PNG framing/profile preflight. Pixel decompression is supplied by the host decoder.</summary>
    public sealed class PaintPngInput
    {
        public const int MaxFileBytes=16*1024*1024;
        public static void RequireSourceHash(byte[] bytes,string expectedHash)
        {
            Checks.Require(bytes!=null && bytes.Length<=MaxFileBytes,"INVALID_PNG","Invalid PNG byte count.");
            Checks.HashText(expectedHash);
            Checks.Require(Checks.Hash(bytes)==expectedHash,"IMPORT_SOURCE_CHANGED","PNG content differs from the requested hash.");
        }
        readonly byte[] bytes;
        public int Width { get; }
        public int Height { get; }
        public bool DeclaredSrgb { get; }
        PaintPngInput(byte[] data,int width,int height,bool srgb) { bytes=data;Width=width;Height=height;DeclaredSrgb=srgb; }
        public byte[] CopyBytes()=>(byte[])bytes.Clone();
        static readonly uint[] CrcTable=CreateCrcTable();
        static uint[] CreateCrcTable()
        {
            var table=new uint[256];for(uint i=0;i<256;i++) { uint c=i;for(int bit=0;bit<8;bit++) c=(c>>1)^((c&1)==0 ? 0 : 0xedb88320u);table[i]=c; }return table;
        }
        static uint U32(byte[] data,int at)=>(uint)data[at]<<24 | (uint)data[at+1]<<16 | (uint)data[at+2]<<8 | data[at+3];
        public static PaintPngInput Read(byte[] input)
        {
            Checks.Require(input!=null && input.Length>=45 && input.Length<=MaxFileBytes,"INVALID_PNG","PNG file must fit the 16 MiB import budget.");
            var data=(byte[])input.Clone();var signature=new byte[]{137,80,78,71,13,10,26,10};
            for(int i=0;i<8;i++) Checks.Require(data[i]==signature[i],"INVALID_PNG","Not a PNG file.");
            int width=0,height=0,count=0,depth=0,color=0,paletteEntries=0;bool idat=false,endedIdat=false,srgb=false,end=false;uint? gamma=null;uint[] chroma=null;
            var singletons=new HashSet<string>();
            using var decoderData=new MemoryStream();decoderData.Write(signature,0,signature.Length);
            for(int at=8;at<data.Length;)
            {
                Checks.Require(++count<=4096 && data.Length-at>=12,"INVALID_PNG","Invalid PNG chunk framing.");
                uint size=U32(data,at);Checks.Require(size<=data.Length-at-12,"INVALID_PNG","PNG chunk extends beyond file.");
                int length=(int)size,p=at+8,next=at+length+12;
                for(int i=at+4;i<at+8;i++) Checks.Require(data[i]>=65 && data[i]<=90 || data[i]>=97 && data[i]<=122,"INVALID_PNG","Invalid PNG chunk type.");
                Checks.Require((data[at+6]&32)==0,"INVALID_PNG","Invalid PNG reserved chunk bit.");
                uint crc=0xffffffff;for(int i=at+4;i<p+length;i++) crc=CrcTable[(crc^data[i])&255]^(crc>>8);
                Checks.Require((crc^0xffffffff)==U32(data,p+length),"INVALID_PNG","PNG chunk CRC mismatch.");
                string type=Encoding.ASCII.GetString(data,at+4,4);
                Checks.Require(count!=1 || type=="IHDR","INVALID_PNG","PNG must begin with IHDR.");
                if(type!="IDAT" && idat) endedIdat=true;
                switch(type)
                {
                    case "IHDR":
                        Checks.Require(count==1 && length==13,"INVALID_PNG","Invalid PNG header.");
                        Checks.Require(U32(data,p)<=PaintImage.MaxDimension && U32(data,p+4)<=PaintImage.MaxDimension,"IMAGE_BUDGET_EXCEEDED","PNG dimensions exceed 1024 pixels.");
                        width=(int)U32(data,p);height=(int)U32(data,p+4);PaintImage.ValidateDimensions(width,height);
                        depth=data[p+8];color=data[p+9];
                        bool valid=color==0 ? depth==1 || depth==2 || depth==4 || depth==8 || depth==16 : color==3 ? depth==1 || depth==2 || depth==4 || depth==8 : (color==2 || color==4 || color==6) && (depth==8 || depth==16);
                        Checks.Require(valid && data[p+10]==0 && data[p+11]==0 && data[p+12]<=1,"INVALID_PNG","Unsupported PNG header values.");break;
                    case "IDAT":
                        Checks.Require(!endedIdat,"INVALID_PNG","PNG image chunks must be consecutive.");
                        Checks.Require(color!=3 || paletteEntries>0,"INVALID_PNG","Indexed PNG requires a palette before image data.");idat=true;break;
                    case "IEND": Checks.Require(length==0 && idat && next==data.Length,"INVALID_PNG","PNG end is missing, premature or followed by trailing data.");end=true;break;
                    case "PLTE":
                        Checks.Require(!idat && !singletons.Contains("tRNS") && color!=0 && color!=4 && length>0 && length<=768 && length%3==0 && singletons.Add(type),"INVALID_PNG","Invalid PNG palette.");
                        paletteEntries=length/3;
                        Checks.Require(color!=3 || paletteEntries<=(1<<depth),"INVALID_PNG","PNG palette exceeds indexed bit depth.");break;
                    case "tRNS":
                        Checks.Require(!idat && singletons.Add(type) && (color==0 ? length==2 : color==2 ? length==6 : color==3 && paletteEntries>0 && length<=paletteEntries),
                            "INVALID_PNG","Invalid PNG transparency table or ordering.");break;
                    case "sRGB": Checks.Require(!idat && length==1 && data[p]<=3 && singletons.Add(type),"INVALID_PNG","Invalid sRGB declaration.");srgb=true;break;
                    case "gAMA": Checks.Require(!idat && length==4 && singletons.Add(type),"INVALID_PNG","Invalid PNG gamma.");gamma=U32(data,p);break;
                    case "cHRM":
                        Checks.Require(!idat && length==32 && singletons.Add(type),"INVALID_PNG","Invalid PNG chromaticities.");
                        chroma=new uint[8];for(int i=0;i<8;i++) chroma[i]=U32(data,p+i*4);break;
                    case "iCCP": case "cICP": case "mDCV": case "cLLI":
                        throw new AuthoringException("UNSUPPORTED_IMAGE_PROFILE","Convert the image to standard sRGB PNG without an embedded ICC/HDR profile before importing.");
                    case "acTL": case "fcTL": case "fdAT": throw new AuthoringException("UNSUPPORTED_IMAGE_ANIMATION","Import a single static PNG image.");
                    default: Checks.Require((data[at+4]&32)!=0,"INVALID_PNG","Unknown critical PNG chunk.");break;
                }
                // Do not send compressed text or unrelated metadata to the native pixel decoder.
                if(type=="IHDR" || type=="PLTE" || type=="tRNS" || type=="IDAT" || type=="IEND" || type=="sRGB") decoderData.Write(data,at,next-at);
                at=next;
            }
            Checks.Require(end,"INVALID_PNG","PNG IEND is missing.");
            if(!srgb)
            {
                Checks.Require(!gamma.HasValue || gamma.Value==45455,"UNSUPPORTED_IMAGE_PROFILE","Convert PNG gamma to sRGB before importing.");
                var expected=new uint[]{31270,32900,64000,33000,30000,60000,15000,6000};
                if(chroma!=null) for(int i=0;i<8;i++) Checks.Require(chroma[i]==expected[i],"UNSUPPORTED_IMAGE_PROFILE","Convert PNG chromaticities to sRGB before importing.");
            }
            return new PaintPngInput(decoderData.ToArray(),width,height,srgb);
        }
    }
}
