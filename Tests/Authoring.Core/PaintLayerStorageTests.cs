using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunPaintLayerStorageTests()
    {
        Test("layer assets retain hidden images masks names order and composite pixels", () =>
        {
            var image=new PaintImage(65,3,new Rgba32(230,40,90,120));
            var mask=new PaintMask(65,3,Enumerable.Range(0,195).Select(i=>(byte)i).ToArray());
            var stack=new PaintLayers(65,3,new[]{new PaintLayer(GraphId(),"背景",image,.5f,true,mask),new PaintLayer(GraphId(),"非表示",image,.8f,false)});
            string dir=Dir("paint-layer-assets");string hash=PaintLayersStore.Write(dir,stack);var read=PaintLayersStore.Read(dir,hash);
            Equal(hash,PaintLayersStore.Write(dir,read));Equal(2,read.Layers.Count);
            Equal(stack.Layers[1].Id,read.Layers[1].Id);Equal("非表示",read.Layers[1].Name);True(!read.Layers[1].Visible);
            Equal(.8f,read.Layers[1].Opacity);True(mask.CopyCoverage().SequenceEqual(read.Layers[0].Mask.CopyCoverage()));
            True(stack.Composite().CopyRgba().SequenceEqual(read.Composite().CopyRgba()));
            True(read.Layers[1].Image.CopyRgba().SequenceEqual(image.CopyRgba()));
            string imageHash=Checks.Hash(PaintImageCodec.Write(image));File.WriteAllBytes(Path.Combine(dir,"blobs",imageHash+".bin"),new byte[]{1});
            Expect("HASH_MISMATCH",()=>PaintLayersStore.Read(dir,hash));
        });
        Test("layer metadata rejects corrupt size version flags and budgets before dependency reads", () =>
        {
            var image=new PaintImage(1,1,new Rgba32(1,2,3));
            var layers=Enumerable.Range(0,8).Select(i=>new PaintLayer(GraphId(),"a",image,1,true,i==0 ? new PaintMask(1,1,new byte[]{255}) : null)).ToArray();
            var blobs=new Dictionary<string,byte[]>();
            byte[] data=PaintLayersCodec.Write(new PaintLayers(1,1,layers),bytes=> { string h=Checks.Hash(bytes);blobs[h]=bytes;return h; });
            int reads=0;byte[] Read(string hash){reads++;return blobs[hash];}
            var tooLarge=(byte[])data.Clone();Buffer.BlockCopy(BitConverter.GetBytes(1024),0,tooLarge,8,4);Buffer.BlockCopy(BitConverter.GetBytes(1024),0,tooLarge,12,4);
            Expect("PAINT_LAYER_BUDGET",()=>PaintLayersCodec.Read(tooLarge,Read));Equal(0,reads);
            Expect("INVALID_LAYER_BLOB",()=>PaintLayersCodec.Read(data.Take(data.Length-1).ToArray(),Read));Equal(0,reads);
            Expect("INVALID_LAYER_BLOB",()=>PaintLayersCodec.Read(data.Concat(new byte[]{0}).ToArray(),Read));Equal(0,reads);
            var future=(byte[])data.Clone();future[4]=2;Expect("UNSUPPORTED_FORMAT",()=>PaintLayersCodec.Read(future,Read));Equal(0,reads);
            var flag=(byte[])data.Clone();flag[20+4+36+4+1+4]=2;
            Expect("INVALID_LAYER_BLOB",()=>PaintLayersCodec.Read(flag,Read));Equal(0,reads);
            var wrongSize=(byte[])data.Clone();Buffer.BlockCopy(BitConverter.GetBytes(2),0,wrongSize,8,4);
            Expect("INVALID_IMAGE_SIZE",()=>PaintLayersCodec.Read(wrongSize,Read));Equal(1,reads);
        });
        Test("mask codec preserves coverage and rejects profile or dimensions", () =>
        {
            var mask=new PaintMask(3,1,new byte[]{0,128,255});var bytes=PaintMaskCodec.Write(mask);
            True(mask.CopyCoverage().SequenceEqual(PaintMaskCodec.Read(bytes,3,1).CopyCoverage()));
            Expect("INVALID_MASK_SIZE",()=>PaintMaskCodec.Read(bytes,1,3));
            Expect("INVALID_MASK_BLOB",()=>PaintMaskCodec.Read(bytes.Take(bytes.Length-1).ToArray(),3,1));
            bytes[16]=2;Expect("UNSUPPORTED_FORMAT",()=>PaintMaskCodec.Read(bytes,3,1));
        });
    }
}
