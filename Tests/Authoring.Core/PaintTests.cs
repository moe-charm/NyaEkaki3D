using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPaintTests()
    {
        Test("paint tiles preserve source and span tile boundaries", () =>
        {
            var source = new PaintImage(128,128,new Rgba32(0,0,0,0));
            var image = PaintStroke.Apply(source,new[]{new Vec2(.5f,.5f)},8,new Rgba32(255,0,0));
            Equal((byte)0,source.GetPixel(63,63).A);Equal((byte)255,image.GetPixel(63,63).R);Equal((byte)255,image.GetPixel(64,64).A);
            Equal((byte)0,image.GetPixel(0,0).A);
            var bytes=image.CopyRgba();bytes[(63*128+63)*4]=0;Equal((byte)255,image.GetPixel(63,63).R);
            var moved=PaintStroke.Apply(image,new[]{new Vec2(.1f,.1f)},4,new Rgba32(0,0,255));
            Equal((byte)0,image.GetPixel(12,12).A);Equal((byte)255,moved.GetPixel(12,12).B);
        });
        Test("paint alpha blends in linear light and stroke sampling does not accumulate opacity", () =>
        {
            var black=new PaintImage(64,64,new Rgba32(0,0,0));
            var gray=PaintStroke.Apply(black,new[]{new Vec2(.5f,.5f)},4,new Rgba32(255,255,255,128));
            Equal((byte)188,gray.GetPixel(31,31).R);Equal((byte)255,gray.GetPixel(31,31).A);
            var source=new PaintImage(128,128,new Rgba32(0,0,0,0));
            var sparse=PaintStroke.Apply(source,new[]{new Vec2(.25f,.5f),new Vec2(.75f,.5f)},5,new Rgba32(0,255,0,128));
            var dense=PaintStroke.Apply(source,new[]{new Vec2(.25f,.5f),new Vec2(.5f,.5f),new Vec2(.5f,.5f),new Vec2(.75f,.5f)},5,new Rgba32(0,255,0,128));
            True(sparse.CopyRgba().SequenceEqual(dense.CopyRgba()));Equal((byte)128,dense.GetPixel(64,64).A);
            True(ReferenceEquals(source,PaintStroke.Apply(source,new[]{new Vec2(.5f,.5f)},4,new Rgba32(255,0,0,0))));
        });
        Test("paint rejects oversized work before changing source", () =>
        {
            Expect("IMAGE_BUDGET_EXCEEDED",()=>new PaintImage(1025,2,new Rgba32()));
            var source=new PaintImage(1024,1024,new Rgba32());
            Expect("INVALID_STROKE",()=>PaintStroke.Apply(source,new[]{new Vec2(-.1f,0)},4,new Rgba32(255,0,0)));
            Expect("STROKE_BUDGET_EXCEEDED",()=>PaintStroke.Apply(source,Enumerable.Repeat(new Vec2(.5f,.5f),PaintStroke.MaxPoints+1),4,new Rgba32()));
            Expect("STROKE_BUDGET_EXCEEDED",()=>PaintStroke.Apply(source,Enumerable.Repeat(new Vec2(.5f,.5f),40),512,new Rgba32(255,0,0)));
            Equal((byte)0,source.GetPixel(512,512).A);
        });
        Test("paint image codec and hash blob preserve odd dimensions and bottom-left rows", () =>
        {
            var source=new PaintImage(65,67,new Rgba32(25,40,80,100));
            var image=PaintStroke.Apply(source,new[]{new Vec2(0,0)},3,new Rgba32(255,0,0));
            string dir=Dir("paint-blob");string hash=PaintImageStore.Write(dir,image);var loaded=PaintImageStore.Read(dir,hash);
            Equal(65,loaded.Width);Equal(67,loaded.Height);True(image.CopyRgba().SequenceEqual(loaded.CopyRgba()));
            Equal(hash,PaintImageStore.Write(dir,loaded));True(loaded.GetPixel(0,0).R>loaded.GetPixel(0,66).R);
            var bytes=PaintImageCodec.Write(image);
            Expect("INVALID_IMAGE_BLOB",()=>PaintImageCodec.Read(bytes.Take(bytes.Length-1).ToArray()));
            Expect("INVALID_IMAGE_BLOB",()=>PaintImageCodec.Read(bytes.Concat(new byte[]{0}).ToArray()));
            var forged=(byte[])bytes.Clone();Array.Copy(BitConverter.GetBytes(int.MaxValue),0,forged,8,4);
            Expect("IMAGE_BUDGET_EXCEEDED",()=>PaintImageCodec.Read(forged));
            forged=(byte[])bytes.Clone();forged[16]=2;Expect("UNSUPPORTED_FORMAT",()=>PaintImageCodec.Read(forged));
        });
        Test("paint UV binding survives position edits and refuses different UV topology or domain", () =>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());string hash=PaintUvBinding.Hash(mesh);
            PaintUvBinding.RequireMatch(hash,mesh.MoveVertices(new ulong[]{1},new Vec3(.01f,0,0)));
            Expect("PAINT_UV_CHANGED",()=>PaintUvBinding.RequireMatch(hash,PolygonUvProjection.Apply(mesh)));
            Expect("PAINT_UV_CHANGED",()=>PaintUvBinding.RequireMatch(hash,PolygonSolidify.Apply(mesh,.01f)));
            Expect("PAINT_UV_CHANGED",()=>PaintUvBinding.RequireMatch(hash,new PolygonMesh(GraphId(),mesh.Vertices.Values,mesh.Faces)));
        });
    }
}
