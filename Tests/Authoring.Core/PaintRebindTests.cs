using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPaintRebindTests()
    {
        Test("explicit paint rebind preserves pixels, Undo and native state", () =>
        {
            var f = PaintFixture(); var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.PaintImageStroke(PaintEditing.Context(f.graph,f.paint),new[]{new Vec2(.3f,.5f)},4,new Rgba32(250,20,100)))));
            var graph = w.Document.Objects[0].Graph; var source = graph.Nodes[f.source];
            var pixels = graph.Nodes[f.paint].PaintImage.CopyRgba();
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Polygon(f.source,PolygonUvProjection.Apply(source.SourcePolygon),source.Transform)))));
            True(!w.Preview.IsComplete); string unresolved = w.Document.StateHash;
            var context = PaintRebinding.Context(w.Document.Objects[0].Graph,f.paint);
            True(context.OldUvHash != context.NewUvHash);
            var command = w.NewCommand(AuthoringOperation.RebindPaintImage(context));
            Ok(commands.Execute(command)); True(w.Preview.IsComplete); string bound = w.Document.StateHash;
            True(pixels.SequenceEqual(w.Preview.Output.BaseColor.Image.CopyRgba()));
            Ok(commands.Execute(command)); Equal(bound,w.Document.StateHash);
            Code("PAINT_CONTEXT_STALE",commands.Execute(w.NewCommand(AuthoringOperation.RebindPaintImage(context))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo()))); Equal(unresolved,w.Document.StateHash); True(!w.Preview.IsComplete);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo()))); Equal(bound,w.Document.StateHash);
            string dir = Dir("paint-rebind"); ProjectStore.Save(dir,w,0); var reopened = ProjectStore.Open(dir);
            Equal(bound,reopened.Document.StateHash); True(reopened.Preview.IsComplete);
            True(pixels.SequenceEqual(reopened.Preview.Output.BaseColor.Image.CopyRgba()));
        });
        Test("paint rebind rejects missing image, disconnected input and changed target", () =>
        {
            var f = PaintFixture();
            Expect("PAINT_IMAGE_REQUIRED",()=>PaintRebinding.Context(f.graph,f.paint));
            var painted = PaintEditing.Stroke(f.graph,PaintEditing.Context(f.graph,f.paint),new[]{new Vec2(.5f,.5f)},4,new Rgba32(10,20,30));
            var context = PaintRebinding.Context(painted,f.paint); var source = painted.Nodes[f.source];
            var moved = painted.ReplaceNode(GraphNode.Polygon(f.source,source.SourcePolygon.MoveVertices(new ulong[]{1},new Vec3(.01f,0,0)),source.Transform));
            Expect("PAINT_CONTEXT_STALE",()=>PaintRebinding.Apply(moved,context));
            var disconnected = painted.WithEdges(painted.Edges.Where(e=>e.ToNode!=f.paint));
            Expect("PAINT_INPUT_UNRESOLVED",()=>PaintRebinding.Context(disconnected,f.paint));
            var changed = PaintEditing.Stroke(painted,PaintEditing.Context(painted,f.paint),new[]{new Vec2(.1f,.1f)},4,new Rgba32(255,0,0));
            Expect("PAINT_CONTEXT_STALE",()=>PaintRebinding.Apply(changed,context));
        });
    }
}
