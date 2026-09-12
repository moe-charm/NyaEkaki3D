using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunStrokePathTests()
    {
        Test("disconnected gesture paints endpoints without a bridge and unions opacity across sections",()=>
        {
            var a=new Vec2(.2f,.5f);var b=new Vec2(.8f,.5f);var color=new Rgba32(255,0,0,128);
            var image=new PaintImage(64,64,new Rgba32(0,0,0));
            var path=new PaintStrokePath(new[]{new[]{a},new[]{b}});
            var result=PaintStroke.ApplyPaths(image,path,3,color);
            Equal((byte)0,result.GetPixel(32,32).R);True(result.GetPixel(12,32).R>0 && result.GetPixel(51,32).R>0);
            var line=new[]{a,b};var repeated=new PaintStrokePath(new[]{line,line});
            var once=PaintStroke.Apply(image,line,3,color);True(once.CopyRgba().SequenceEqual(PaintStroke.ApplyPaths(image,repeated,3,color).CopyRgba()));
            var joined=PaintStroke.ApplyPaths(image,new PaintStrokePath(new[]{line}),3,color);True(joined.GetPixel(32,32).R>0);
            var white=new PaintMask(64,64,Enumerable.Repeat((byte)255,4096).ToArray());
            var mask=PaintMaskStroke.ApplyPaths(white,path,3,0,.5f);Equal((byte)255,mask.GetCoverage(32,32));Equal((byte)128,mask.GetCoverage(12,32));
            True(PaintMaskStroke.Apply(white,line,3,0,.5f).CopyCoverage().SequenceEqual(PaintMaskStroke.ApplyPaths(white,repeated,3,0,.5f).CopyCoverage()));
        });
        Test("gesture sections freeze input and share point and pixel budgets",()=>
        {
            var points=new[]{new Vec2(.2f,.5f),new Vec2(.8f,.5f)};var sections=new[]{points};var path=new PaintStrokePath(sections);
            points[0]=new Vec2(2,2);sections[0]=Array.Empty<Vec2>();Equal(2,path.PointCount);Near(.2f,path.Sections[0][0].X);
            Expect("STROKE_BUDGET_EXCEEDED",()=>new PaintStrokePath(new[]{Array.Empty<Vec2>()}));
            Expect("INVALID_STROKE",()=>new PaintStrokePath(new[]{new[]{new Vec2(-1,0)}}));
            var single=new[]{new Vec2(.5f,.5f)};
            Expect("STROKE_BUDGET_EXCEEDED",()=>new PaintStrokePath(Enumerable.Repeat(single,1025)));
            var many=new PaintStrokePath(Enumerable.Repeat(single,20));var image=new PaintImage(1024,1024,new Rgba32());
            Expect("STROKE_BUDGET_EXCEEDED",()=>PaintStroke.ApplyPaths(image,many,512,new Rgba32(255,0,0)));
            var mask=new PaintMask(1024,1024,new byte[1024*1024]);
            Expect("STROKE_BUDGET_EXCEEDED",()=>PaintMaskStroke.ApplyPaths(mask,many,512,255,1));
            Equal((byte)0,image.GetPixel(512,512).A);Equal((byte)0,mask.GetCoverage(512,512));
        });
        Test("section boundaries participate in retry identity and all sections undo as one command",()=>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();var service=new AuthoringCommandService(w);string id=GraphId();
            Ok(service.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            Ok(service.Execute(w.NewCommand(AuthoringOperation.MigratePaintLayers(PaintEditing.Context(f.graph,f.paint),id))));
            var context=LayerEditing.Context(w.Document.Objects[0].Graph,f.paint);string before=w.Document.StateHash;long revision=w.Document.DocumentRevision;
            var a=new Vec2(.2f,.5f);var b=new Vec2(.8f,.5f);var color=new Rgba32(255,0,0,128);
            var split=new PaintStrokePath(new[]{new[]{a},new[]{b}});var joined=new PaintStrokePath(new[]{new[]{a,b}});
            var command=w.NewCommand(AuthoringOperation.EditPaintLayers(context,PaintLayerChange.PathStroke(id,split,3,color)));
            Ok(service.Execute(command));string after=w.Document.StateHash;Equal(revision+1,w.Document.DocumentRevision);
            Ok(service.Execute(command));Equal(revision+1,w.Document.DocumentRevision);
            command.Operations=new[]{AuthoringOperation.EditPaintLayers(context,PaintLayerChange.PathStroke(id,joined,3,color))};
            Code("COMMAND_ID_REUSED",service.Execute(command));Equal(after,w.Document.StateHash);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("section-gesture-native");ProjectStore.Save(dir,w,0);Equal(after,ProjectStore.Open(dir).Document.StateHash);
        });
        Test("mask sections preserve color and verify strength identity, Undo and stale context",()=>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();var service=new AuthoringCommandService(w);string id=GraphId();
            Ok(service.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            Ok(service.Execute(w.NewCommand(AuthoringOperation.MigratePaintLayers(PaintEditing.Context(f.graph,f.paint),id))));
            LayerEditContext Context()=>LayerEditing.Context(w.Document.Objects[0].Graph,f.paint);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(Context(),PaintLayerChange.Mask(id,new PaintMask(64,64,Enumerable.Repeat((byte)255,4096).ToArray()))))));
            var context=Context();string before=w.Document.StateHash;var pixels=w.Preview.Output.BaseColor.Image.CopyRgba();
            var path=new PaintStrokePath(new[]{new[]{new Vec2(.2f,.5f)},new[]{new Vec2(.8f,.5f)}});
            var command=w.NewCommand(AuthoringOperation.EditPaintLayers(context,PaintLayerChange.MaskPathStroke(id,path,3,0,.5f)));Ok(service.Execute(command));
            var layer=w.Document.Objects[0].Graph.Nodes[f.paint].LayerStack.Layers[0];string after=w.Document.StateHash;
            True(pixels.SequenceEqual(layer.Image.CopyRgba()));Equal((byte)128,layer.Mask.GetCoverage(12,32));Equal((byte)255,layer.Mask.GetCoverage(32,32));
            Ok(service.Execute(command));Equal(after,w.Document.StateHash);
            command.Operations=new[]{AuthoringOperation.EditPaintLayers(context,PaintLayerChange.MaskPathStroke(id,path,3,0,1))};Code("COMMAND_ID_REUSED",service.Execute(command));
            Code("PAINT_CONTEXT_STALE",service.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(context,PaintLayerChange.MaskPathStroke(id,path,3,0,.5f)))));
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
        });
    }
}
