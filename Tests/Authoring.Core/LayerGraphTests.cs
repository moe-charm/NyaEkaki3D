using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunLayerGraphTests()
    {
        Test("layered graph native save owns all images masks and hidden payloads", () =>
        {
            var f=PaintFixture();var binding=PaintEditing.Context(f.graph,f.paint);
            var image=new PaintImage(64,64,new Rgba32(255,30,100));
            var mask=new PaintMask(64,64,Enumerable.Repeat((byte)128,4096).ToArray());
            var bottom=new PaintLayer(GraphId(),"下",image,.8f,true,mask);
            var hidden=new PaintLayer(GraphId(),"隠した層",new PaintImage(64,64,new Rgba32(0,100,255)),1,false);
            var stack=new PaintLayers(64,64,new[]{bottom,hidden});
            var graph=f.graph.ReplaceNode(GraphNode.LayeredPaint(f.paint,stack,binding.UvHash,binding.MeshDomain));
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            True(w.Preview.IsComplete);True(stack.Composite().CopyRgba().SequenceEqual(w.Preview.Output.BaseColor.Image.CopyRgba()));
            string before=w.Document.StateHash; string composite=w.Preview.Output.BaseColor.ImageHash;
            var review=PaintRebinding.Context(graph,f.paint);
            var changedStack=stack.Replace(hidden.WithImage(new PaintImage(64,64,new Rgba32(30,40,50))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.LayeredPaint(f.paint,changedStack,binding.UvHash,binding.MeshDomain)))));
            True(w.Document.StateHash!=before);Equal(composite,w.Preview.Output.BaseColor.ImageHash);
            Expect("PAINT_CONTEXT_STALE",()=>PaintRebinding.Apply(w.Document.Objects[0].Graph,review));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            string dir=Dir("layer-graph");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);
            Equal(before,reopened.Document.StateHash);var read=reopened.Document.Objects[0].Graph.Nodes[f.paint].LayerStack;
            Equal(2,read.Layers.Count);Equal(hidden.Id,read.Layers[1].Id);True(!read.Layers[1].Visible);
            True(mask.CopyCoverage().SequenceEqual(read.Layers[0].Mask.CopyCoverage()));
            Equal(composite,reopened.Preview.Output.BaseColor.ImageHash);
            var surface=SurfaceBakeStore.Read(SurfaceBakeStore.Export(Dir("layer-surface"),reopened));
            True(surface.BaseColor.CopyRgba().SequenceEqual(stack.Composite().CopyRgba()));
            Expect("PAINT_NODE_REQUIRED",()=>PaintEditing.Context(graph,f.paint)); // legacy stroke cannot flatten a layer stack
        });
        Test("layered graph UV invalidation and rebind preserve layers through save", () =>
        {
            var f=PaintFixture();var context=PaintEditing.Context(f.graph,f.paint);
            var layer=new PaintLayer(GraphId(),"one",new PaintImage(64,64,new Rgba32(255,210,50)));
            var graph=f.graph.ReplaceNode(GraphNode.LayeredPaint(f.paint,new PaintLayers(64,64,new[]{layer}),context.UvHash,context.MeshDomain));
            var source=graph.Nodes[f.source];graph=graph.ReplaceNode(GraphNode.Polygon(f.source,PolygonUvProjection.Apply(source.SourcePolygon),source.Transform));
            True(!GraphEvaluator.Evaluate(graph).IsComplete);True(PaintDependencies.DownstreamImages(graph,f.source).Contains(f.paint));
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            string dir=Dir("layer-unresolved");ProjectStore.Save(dir,w,0);var read=ProjectStore.Open(dir);
            True(!read.Preview.IsComplete);Equal(layer.Id,read.Document.Objects[0].Graph.Nodes[f.paint].LayerStack.Layers[0].Id);
            var rebound=PaintRebinding.Apply(graph,PaintRebinding.Context(graph,f.paint));
            True(GraphEvaluator.Evaluate(rebound).IsComplete);Equal(layer.Id,rebound.Nodes[f.paint].LayerStack.Layers[0].Id);
            var empty=graph.ReplaceNode(GraphNode.LayeredPaint(f.paint,new PaintLayers(64,64,Array.Empty<PaintLayer>()),PaintRebinding.Context(graph,f.paint).NewUvHash,context.MeshDomain));
            Equal((byte)0,GraphEvaluator.Evaluate(empty).Output.BaseColor.Image.GetPixel(0,0).A);
        });
    }
}
