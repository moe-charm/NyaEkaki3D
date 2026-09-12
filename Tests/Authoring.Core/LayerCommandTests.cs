using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunLayerCommandTests()
    {
        Test("explicit migration preserves legacy RGBA and Undo restores the old node type", () =>
        {
            var f=PaintFixture();var bind=PaintEditing.Context(f.graph,f.paint);
            var image=new PaintImage(64,64,new Rgba32(130,70,20,0));
            var graph=f.graph.ReplaceNode(GraphNode.Paint(f.paint,64,64,image,bind.UvHash,bind.MeshDomain));
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            string layer=GraphId();var migrate=w.NewCommand(AuthoringOperation.MigratePaintLayers(PaintEditing.Context(graph,f.paint),layer));
            Ok(commands.Execute(migrate));string after=w.Document.StateHash;
            True(image.CopyRgba().SequenceEqual(w.Preview.Output.BaseColor.Image.CopyRgba()));
            Equal(layer,w.Document.Objects[0].Graph.Nodes[f.paint].LayerStack.Layers[0].Id);
            Ok(commands.Execute(migrate));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Equal(BuiltinNodes.Paint,w.Document.Objects[0].Graph.Nodes[f.paint].TypeId);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
        });
        Test("layer commands add move rename mask hide remove and restore through Undo and save", () =>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);string baseId=GraphId();
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.MigratePaintLayers(PaintEditing.Context(f.graph,f.paint),baseId))));
            LayerEditContext Context()=>LayerEditing.Context(w.Document.Objects[0].Graph,f.paint);
            void Apply(PaintLayerChange change)=>Ok(commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(Context(),change))));
            var top=new PaintLayer(GraphId(),"top",new PaintImage(64,64,new Rgba32(255,0,0)));
            Apply(PaintLayerChange.Add(top,1));Apply(PaintLayerChange.Move(top.Id,0));
            Equal(top.Id,w.Document.Objects[0].Graph.Nodes[f.paint].LayerStack.Layers[0].Id);
            Apply(PaintLayerChange.Rename(top.Id,"模様"));Apply(PaintLayerChange.Mask(top.Id,new PaintMask(64,64,new byte[4096])));
            Apply(PaintLayerChange.Appearance(top.Id,.4f,false));
            var stale=Context();Apply(PaintLayerChange.Rename(top.Id,"隠した模様"));
            string before=w.Document.StateHash;
            Code("PAINT_CONTEXT_STALE",commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(stale,PaintLayerChange.Remove(baseId)))));Equal(before,w.Document.StateHash);
            Apply(PaintLayerChange.Remove(top.Id));Equal(1,w.Document.Objects[0].Graph.Nodes[f.paint].LayerStack.Layers.Count);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            string dir=Dir("layer-command-save");ProjectStore.Save(dir,w,0);var read=ProjectStore.Open(dir);
            Equal(before,read.Document.StateHash);var layer=read.Document.Objects[0].Graph.Nodes[f.paint].LayerStack.Layers[0];
            Equal("隠した模様",layer.Name);Equal(.4f,layer.Opacity);True(!layer.Visible && layer.Mask!=null);
            Code("INVALID_LAYER_INDEX",commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(Context(),PaintLayerChange.Move(baseId,99)))));Equal(before,w.Document.StateHash);
            Code("INVALID_PAINT_LAYERS",commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(Context(),PaintLayerChange.Add(top,0)))));Equal(before,w.Document.StateHash);
        });
        Test("layer stroke owns points and commits once with retry and Undo", () =>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);string id=GraphId();
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.MigratePaintLayers(PaintEditing.Context(f.graph,f.paint),id))));
            var context=LayerEditing.Context(w.Document.Objects[0].Graph,f.paint);string before=w.Document.StateHash;
            var points=new[]{new Vec2(.2f,.5f),new Vec2(.8f,.5f)};
            var change=PaintLayerChange.Stroke(id,points,4,new Rgba32(255,0,0,128));points[0]=new Vec2(2,2);
            long revision=w.Document.DocumentRevision;
            var command=w.NewCommand(AuthoringOperation.EditPaintLayers(context,change));Ok(commands.Execute(command));
            Equal(revision+1,w.Document.DocumentRevision);string after=w.Document.StateHash;True(after!=before);
            Ok(commands.Execute(command));Equal(after,w.Document.StateHash);Equal(revision+1,w.Document.DocumentRevision);
            Code("PAINT_CONTEXT_STALE",commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(context,change))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            var current=LayerEditing.Context(w.Document.Objects[0].Graph,f.paint);
            Code("INVALID_STROKE",commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(current,PaintLayerChange.Stroke(id,new[]{new Vec2(2,2)},4,new Rgba32(0,0,0))))));Equal(after,w.Document.StateHash);
        });
    }
}
