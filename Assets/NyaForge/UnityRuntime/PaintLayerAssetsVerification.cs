using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

namespace NyaForge.UnityRuntime
{
    internal static class PaintLayerAssetsVerification
    {
        internal static void Verify(string output,List<string> checks)
        {
            var background=new PaintLayer(Guid.NewGuid().ToString("D"),"背景",new PaintImage(256,128,new Rgba32(255,210,50)));
            var stroke=PaintStroke.Apply(new PaintImage(256,128,new Rgba32()),new[]{new Vec2(.1f,.2f),new Vec2(.9f,.8f)},18,new Rgba32(245,70,145));
            var coverage=new byte[256*128];
            for(int y=0;y<128;y++) for(int x=0;x<256;x++) coverage[y*256+x]=(byte)x;
            var masked=new PaintLayer(Guid.NewGuid().ToString("D"),"模様",stroke,.75f,true,new PaintMask(256,128,coverage));
            var hidden=new PaintLayer(Guid.NewGuid().ToString("D"),"非表示",new PaintImage(256,128,new Rgba32(0,0,0)),1,false);
            var stack=new PaintLayers(256,128,new[]{background,masked,hidden});
            string dir=Path.Combine(output,"layer-assets"); string hash=PaintLayersStore.Write(dir,stack);
            var read=PaintLayersStore.Read(dir,hash);
            if(read.Layers.Count!=3 || read.Layers[2].Visible || read.Layers[1].Name!="模様" ||
                !read.Layers[1].Mask.CopyCoverage().SequenceEqual(coverage) ||
                !read.Composite().CopyRgba().SequenceEqual(stack.Composite().CopyRgba()) || PaintLayersStore.Write(dir,read)!=hash)
                throw new InvalidOperationException("Layer asset roundtrip changed layer payloads or composite.");
            File.WriteAllBytes(Path.Combine(output,"layer-composite.png"),PaintPng.Encode(read.Composite()));
            checks.Add("Layer asset storage: order/names/opacity/mask/hidden image retained; composite pixels and metadata hash roundtrip. Graph ownership pending.");
        }
    }
}
