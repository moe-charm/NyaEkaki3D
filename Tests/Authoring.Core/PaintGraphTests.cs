using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static (AuthoringGraph graph,string source,string paint,string output) PaintFixture()
    {
        string source=GraphId(),paint=GraphId(),output=GraphId();
        var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,PolygonPrimitives.Plane(GraphId()),new RestTransform(1,new Vec3())),GraphNode.Paint(paint,64,64),GraphNode.Output(output)},
            new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(source,"mesh",output,"mesh"),new GraphEdge(paint,"image",output,"baseColor")},output);
        return(graph,source,paint,output);
    }
    static void RunPaintGraphTests()
    {
        Test("typed image port binds paint and rejects scalar or mesh confusion", () =>
        {
            var f=PaintFixture();var result=GraphEvaluator.Evaluate(f.graph);
            True(result.IsComplete);Equal(1,result.ImageOutputs.Count);Equal((byte)255,result.Output.BaseColor.Image.GetPixel(0,0).R);
            True(result.Output.SnapshotHash!=result.MeshOutputs[f.source].SnapshotHash);
            Expect("PORT_TYPE_MISMATCH",()=>f.graph.WithEdges(f.graph.Edges.Where(e=>e.ToPort!="baseColor").Append(new GraphEdge(f.source,"mesh",f.output,"baseColor"))));
            var unrelated=GraphNode.Polygon(GraphId(),PolygonPrimitives.Plane(GraphId()),new RestTransform(1,new Vec3()));
            var wrong=new AuthoringGraph(f.graph.GraphId,f.graph.Nodes.Values.Append(unrelated),f.graph.Edges.Where(e=>!(e.ToNode==f.output && e.ToPort=="mesh")).Append(new GraphEdge(unrelated.NodeId,"mesh",f.output,"mesh")),f.output);
            True(GraphEvaluator.Evaluate(wrong).Diagnostics.Any(d=>d.Code=="PAINT_UV_CHANGED"));
        });
        Test("paint stroke is one Undo and retries do not paint twice", () =>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));string before=w.Document.StateHash;
            var points=new[]{new Vec2(.2f,.5f),new Vec2(.8f,.5f)};
            var context=PaintEditing.Context(f.graph,f.paint);
            var operation=AuthoringOperation.PaintImageStroke(context,points,4,new Rgba32(255,0,0,128));
            points[0]=new Vec2(0,0); // operation owns its input copy
            var command=w.NewCommand(operation);Ok(commands.Execute(command));string after=w.Document.StateHash;
            string image=w.Preview.Output.BaseColor.ImageHash;True(before!=after);Ok(commands.Execute(command));Equal(after,w.Document.StateHash);
            Code("PAINT_CONTEXT_STALE",commands.Execute(w.NewCommand(operation)));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);Equal(image,w.Preview.Output.BaseColor.ImageHash);
            string dir=Dir("paint-project");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);
            Equal(after,reopened.Document.StateHash);Equal(image,reopened.Preview.Output.BaseColor.ImageHash);
            string bake=Path.Combine(dir,"unsupported-bake");Expect("EXPORT_UNSUPPORTED_FEATURE",()=>BakeStore.Export(bake,reopened));True(!Directory.Exists(bake));
        });
        Test("UV change retains paint as unresolved through native save and Undo", () =>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.PaintImageStroke(PaintEditing.Context(f.graph,f.paint),new[]{new Vec2(.5f,.5f)},4,new Rgba32(0,0,255)))));
            var painted=w.Document.Objects[0].Graph;var oldImage=painted.Nodes[f.paint].PaintImage.CopyRgba();
            var source=painted.Nodes[f.source];var moved=source.SourcePolygon.MoveVertices(new ulong[]{1},new Vec3(.01f,0,0));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Polygon(f.source,moved,source.Transform)))));True(w.Preview.IsComplete);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Polygon(f.source,PolygonUvProjection.Apply(moved),source.Transform)))));
            True(!w.Preview.IsComplete);True(w.Preview.Evaluation.Diagnostics.Any(d=>d.Code=="PAINT_UV_CHANGED"));
            True(oldImage.SequenceEqual(w.Document.Objects[0].Graph.Nodes[f.paint].PaintImage.CopyRgba()));
            string dir=Dir("unresolved-paint");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);
            True(!reopened.Preview.IsComplete);True(oldImage.SequenceEqual(reopened.Document.Objects[0].Graph.Nodes[f.paint].PaintImage.CopyRgba()));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));True(w.Preview.IsComplete);
        });
        Test("invalid paint stroke preserves document and paint image", () =>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            string before=w.Document.StateHash;long revision=w.Document.DocumentRevision;
            Code("INVALID_STROKE",commands.Execute(w.NewCommand(AuthoringOperation.PaintImageStroke(PaintEditing.Context(f.graph,f.paint),new[]{new Vec2(2,0)},4,new Rgba32(0,0,0)))));
            Equal(before,w.Document.StateHash);Equal(revision,w.Document.DocumentRevision);True(w.Document.Objects[0].Graph.Nodes[f.paint].PaintImage==null);
        });
    }
}
