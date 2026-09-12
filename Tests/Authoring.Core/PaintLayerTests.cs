using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunPaintLayerTests()
    {
        Test("layers combine opacity and linear mask without changing source images", () =>
        {
            var black=new PaintImage(2,1,new Rgba32(0,0,0)); var white=new PaintImage(2,1,new Rgba32(255,255,255));
            var bytes=new byte[]{255,0}; var mask=new PaintMask(2,1,bytes); bytes[0]=0;
            var background=new PaintLayer(GraphId(),"背景",black);
            var top=new PaintLayer(GraphId(),"色",white,.5f,true,mask);
            var stack=new PaintLayers(2,1,new[]{background,top}); var result=stack.Composite();
            Equal((byte)188,result.GetPixel(0,0).R); Equal((byte)0,result.GetPixel(1,0).R);
            Equal((byte)255,result.GetPixel(0,0).A); Equal((byte)255,white.GetPixel(1,0).R);
            Equal((byte)255,mask.GetCoverage(0,0)); var copied=mask.CopyCoverage();copied[0]=0;Equal((byte)255,mask.GetCoverage(0,0));
            var hidden=stack.Replace(top.WithAppearance(.5f,false));
            Equal((byte)0,hidden.Composite().GetPixel(0,0).R); Equal((byte)188,stack.Composite().GetPixel(0,0).R);
            Equal((byte)188,stack.Replace(top.WithMask(null)).Composite().GetPixel(1,0).R);
            var halfMask=new PaintMask(2,1,new byte[]{128,128});
            Equal((byte)137,stack.Replace(top.WithMask(halfMask)).Composite().GetPixel(0,0).R);
        });
        Test("layer order and transparent canvas preserve straight alpha", () =>
        {
            var red=new PaintLayer(GraphId(),"red",new PaintImage(1,1,new Rgba32(255,0,0)),.5f);
            var blue=new PaintLayer(GraphId(),"blue",new PaintImage(1,1,new Rgba32(0,0,255)));
            var solo=new PaintLayers(1,1,new[]{red}).Composite().GetPixel(0,0);
            Equal((byte)255,solo.R);Equal((byte)128,solo.A);
            Equal((byte)255,new PaintLayers(1,1,new[]{red,blue}).Composite().GetPixel(0,0).B);
            var reversed=new PaintLayers(1,1,new[]{blue,red}).Composite().GetPixel(0,0);
            Equal((byte)188,reversed.R);Equal((byte)188,reversed.B);
            Equal((byte)0,new PaintLayers(1,1,Array.Empty<PaintLayer>()).Composite().GetPixel(0,0).A);
        });
        Test("layer identities dimensions opacity and memory budgets are validated", () =>
        {
            var image=new PaintImage(1,1,new Rgba32()); var layer=new PaintLayer(GraphId(),"a",image);
            Expect("INVALID_PAINT_LAYERS",()=>new PaintLayers(1,1,new[]{layer,layer}));
            Expect("INVALID_IMAGE_SIZE",()=>new PaintLayers(2,1,new[]{layer}));
            Expect("INVALID_LAYER_OPACITY",()=>layer.WithAppearance(2,true));
            Expect("NON_FINITE",()=>layer.WithAppearance(float.NaN,true));
            Expect("INVALID_MASK_SIZE",()=>layer.WithMask(new PaintMask(2,1,new byte[2])));
            Expect("PAINT_LAYER_BUDGET",()=>new PaintLayers(1,1,Enumerable.Range(0,17).Select(i=>new PaintLayer(GraphId(),"a",image))));
            var large=new PaintImage(1024,1024,new Rgba32());
            Expect("PAINT_LAYER_BUDGET",()=>new PaintLayers(1024,1024,Enumerable.Range(0,9).Select(i=>new PaintLayer(GraphId(),"a",large))));
        });
    }
}
