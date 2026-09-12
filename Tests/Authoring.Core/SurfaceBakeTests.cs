using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunSurfaceBakeTests()
    {
        foreach (float scale in new[]{1f,100f})
            Test("surface Bake preserves mesh, image and metadata scale " + scale, () =>
            {
                var f = PaintFixture(); var source = f.graph.Nodes[f.source];
                var graph = f.graph.ReplaceNode(GraphNode.Polygon(f.source,source.SourcePolygon,new RestTransform(scale,new Vec3())));
                graph = PaintEditing.Stroke(graph,PaintEditing.Context(graph,f.paint),new[]{new Vec2(.3f,.4f)},5,new Rgba32(50,150,240,120));
                var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w);
                Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
                string state = w.Document.StateHash, dir = Dir("surface-"+scale);
                string manifest = SurfaceBakeStore.Export(dir,w); var read = SurfaceBakeStore.Read(manifest);
                Equal(state,w.Document.StateHash); Equal(w.Document.DocumentRevision,read.Geometry.DocumentRevision);
                Equal(w.Evaluate().ContentHash,read.Geometry.Mesh.ContentHash);
                True(read.BaseColor.CopyRgba().SequenceEqual(w.Preview.Output.BaseColor.Image.CopyRgba()));
                Equal(w.Preview.Output.BaseColor.UvHash,read.UvHash); Equal(w.Preview.Output.BaseColor.MeshDomain,read.MeshDomain);
                True(read.CopyPng().SequenceEqual(PaintPng.Encode(read.BaseColor)));
                var copy = read.CopyPng(); copy[0] = 0; Equal((byte)137,read.CopyPng()[0]);
                Expect("INVALID_MANIFEST",()=>BakeStore.Read(manifest));
                string old = Path.Combine(dir,"mesh-only"); Expect("EXPORT_UNSUPPORTED_FEATURE",()=>BakeStore.Export(old,w)); True(!Directory.Exists(old));
                File.WriteAllBytes(Path.Combine(dir,read.PngHash+".png"),new byte[]{1,2,3});
                Expect("HASH_MISMATCH",()=>SurfaceBakeStore.Read(manifest));
                var beforeManifest = File.ReadAllBytes(manifest);
                Expect("HASH_MISMATCH",()=>SurfaceBakeStore.Export(dir,w)); True(beforeManifest.SequenceEqual(File.ReadAllBytes(manifest)));
            });
        Test("surface Bake refuses missing image and unresolved graph before publication", () =>
        {
            var f = PaintFixture(); var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w);
            var graph = PaintEditing.Stroke(f.graph,PaintEditing.Context(f.graph,f.paint),new[]{new Vec2(.5f,.5f)},4,new Rgba32(255,0,0));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            var source = graph.Nodes[f.source];
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Polygon(f.source,PolygonUvProjection.Apply(source.SourcePolygon),source.Transform)))));
            string dir = Path.Combine(Dir("surface-reject"),"unresolved");
            Expect("GRAPH_INCOMPLETE",()=>SurfaceBakeStore.Export(dir,w)); True(!Directory.Exists(dir));
            var unpainted = f.graph.WithEdges(f.graph.Edges.Where(e=>e.ToPort!="baseColor"));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.ReplaceGraph(unpainted))));
            Expect("PAINT_IMAGE_REQUIRED",()=>SurfaceBakeStore.Export(dir,w)); True(!Directory.Exists(dir));
        });
    }
}
