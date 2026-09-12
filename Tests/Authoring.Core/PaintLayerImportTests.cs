using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunPaintLayerImportTests()
    {
        Test("shared layer import fits pixels and pins source bytes",()=>
        {
            var source=new PaintImage(8,4,new Rgba32(255,0,0));
            var layer=PaintLayerImport.Prepare(source,8,8,true,GraphId(),"import",0).Layer;
            Equal((byte)0,layer.Image.GetPixel(0,0).A);Equal((byte)255,layer.Image.GetPixel(4,4).R);
            Expect("INVALID_IMAGE_SIZE",()=>PaintLayerImport.Prepare(source,8,8,false,GraphId(),"import",0));
            var bytes=PaintPng.Encode(source);string hash=Checks.Hash(bytes);PaintPngInput.RequireSourceHash(bytes,hash);
            bytes[bytes.Length-1]^=1;Expect("IMPORT_SOURCE_CHANGED",()=>PaintPngInput.RequireSourceHash(bytes,hash));
            Equal(4,source.Height);
        });
    }
}
