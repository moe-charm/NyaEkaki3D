using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPaintPngTests()
    {
        Test("portable PNG decompresses to exact top-first RGBA across deflate blocks", () =>
        {
            var image = PaintStroke.Apply(new PaintImage(257,131,new Rgba32(10,20,30,40)),new[]{new Vec2(.2f,.1f),new Vec2(.7f,.3f)},12,new Rgba32(240,50,150,120));
            var png = PaintPng.Encode(image); True(png.SequenceEqual(PaintPng.Encode(image)));
            True(png.Take(8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}));
            int at = 8; byte[] compressed = null; bool srgb = false;
            while (at < png.Length)
            {
                int length = (png[at]<<24)|(png[at+1]<<16)|(png[at+2]<<8)|png[at+3];
                string type = Encoding.ASCII.GetString(png,at+4,4);
                if (type == "sRGB") srgb = length == 1 && png[at+8] == 0;
                if (type == "IDAT") compressed = png.Skip(at+8).Take(length).ToArray();
                at += length+12;
            }
            True(srgb && compressed != null); Equal(png.Length,at);
            using var input = new MemoryStream(compressed);
            using var zlib = new ZLibStream(input,CompressionMode.Decompress);
            using var output = new MemoryStream(); zlib.CopyTo(output); var raw = output.ToArray();
            Equal(image.Height*(1+image.Width*4),raw.Length); at = 0;
            for (int y = image.Height-1; y >= 0; y--)
            {
                Equal((byte)0,raw[at++]);
                for (int x = 0; x < image.Width; x++)
                {
                    var p = image.GetPixel(x,y);
                    Equal(p.R,raw[at++]); Equal(p.G,raw[at++]); Equal(p.B,raw[at++]); Equal(p.A,raw[at++]);
                }
            }
        });
        Test("PNG export pins committed image and refuses stale UV before creating output", () =>
        {
            var f = PaintFixture(); var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.PaintImageStroke(PaintEditing.Context(f.graph,f.paint),new[]{new Vec2(.5f,.5f)},5,new Rgba32(255,0,0)))));
            string state = w.Document.StateHash; string dir = Dir("png-export");
            string path = PaintPngExport.Write(dir,w,f.paint); Equal(state,w.Document.StateHash);
            True(File.ReadAllBytes(path).SequenceEqual(PaintPng.Encode(w.Preview.Output.BaseColor.Image)));
            var manifest = Storage.ReadJson<PaintPngManifest>(Path.Combine(dir,PaintPngExport.ManifestName));
            Equal(w.Document.DocumentRevision,manifest.DocumentRevision); Equal(w.Preview.Output.BaseColor.ImageHash,manifest.ImageHash);
            Equal(Checks.Hash(File.ReadAllBytes(path)),manifest.PngHash); Equal(path,PaintPngExport.Write(dir,w,f.paint));
            File.WriteAllBytes(path,new byte[]{1}); Expect("HASH_MISMATCH",()=>PaintPngExport.Write(dir,w,f.paint));
            var source = w.Document.Objects[0].Graph.Nodes[f.source];
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Polygon(f.source,PolygonUvProjection.Apply(source.SourcePolygon),source.Transform)))));
            string rejected = Path.Combine(dir,"unresolved");
            Expect("PAINT_UNRESOLVED",()=>PaintPngExport.Write(rejected,w,f.paint)); True(!Directory.Exists(rejected));
        });
    }
}
