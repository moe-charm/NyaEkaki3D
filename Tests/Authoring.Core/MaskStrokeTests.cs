using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunMaskStrokeTests()
    {
        Test("mask stroke is linear, immutable and independent of repeated path sampling",()=>
        {
            var source=new PaintMask(16,16,Enumerable.Repeat((byte)255,256).ToArray());
            var path=new[]{new Vec2(.2f,.5f),new Vec2(.8f,.5f)};
            var result=PaintMaskStroke.Apply(source,path,2,0,.5f);
            Equal((byte)128,result.GetCoverage(8,8));Equal((byte)255,result.GetCoverage(0,0));
            Equal((byte)255,source.GetCoverage(8,8));
            var repeated=PaintMaskStroke.Apply(source,new[]{path[0],new Vec2(.5f,.5f),path[1],path[0],path[1]},2,0,.5f);
            True(result.CopyCoverage().SequenceEqual(repeated.CopyCoverage()));
            var restored=PaintMaskStroke.Apply(result,path,2,255,1);Equal((byte)255,restored.GetCoverage(8,8));
            True(ReferenceEquals(source,PaintMaskStroke.Apply(source,path,2,0,0)));
            var zero=new PaintMask(16,16,new byte[256]);
            Equal((byte)128,PaintMaskStroke.Apply(zero,path,2,255,.5f).GetCoverage(8,8));
        });
        Test("mask stroke rejects invalid input and bounded work before changing source",()=>
        {
            var mask=new PaintMask(1024,1024,new byte[1024*1024]);var point=new[]{new Vec2(.5f,.5f)};
            Expect("PAINT_MASK_REQUIRED",()=>PaintMaskStroke.Apply(null,point,2,0,1));
            Expect("INVALID_MASK_STRENGTH",()=>PaintMaskStroke.Apply(mask,point,2,0,2));
            Expect("INVALID_BRUSH",()=>PaintMaskStroke.Apply(mask,point,0,0,1));
            Expect("INVALID_STROKE",()=>PaintMaskStroke.Apply(mask,new[]{new Vec2(-1,0)},2,0,1));
            Expect("STROKE_BUDGET_EXCEEDED",()=>PaintMaskStroke.Apply(mask,Enumerable.Repeat(point[0],1025),2,0,1));
            Expect("STROKE_BUDGET_EXCEEDED",()=>PaintMaskStroke.Apply(mask,Enumerable.Repeat(point[0],40),512,0,0));
            True(mask.CopyCoverage().All(v=>v==0));
        });
        Test("mask command keeps color pixels, pins context, retries once and survives native Undo roundtrip",()=>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);string id=GraphId();
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(f.graph))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.MigratePaintLayers(PaintEditing.Context(f.graph,f.paint),id))));
            LayerEditContext Context()=>LayerEditing.Context(w.Document.Objects[0].Graph,f.paint);
            PaintLayer Layer()=>w.Document.Objects[0].Graph.Nodes[f.paint].LayerStack.Layers[0];
            var points=new[]{new Vec2(.5f,.5f)};string noMask=w.Document.StateHash;
            Code("PAINT_MASK_REQUIRED",commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(Context(),PaintLayerChange.MaskStroke(id,points,5,0,1)))));
            Equal(noMask,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(Context(),PaintLayerChange.Mask(id,new PaintMask(64,64,Enumerable.Repeat((byte)255,4096).ToArray()))))));
            var context=Context();string before=w.Document.StateHash;var pixels=Layer().Image.CopyRgba();long revision=w.Document.DocumentRevision;
            var change=PaintLayerChange.MaskStroke(id,points,5,0,.5f);points[0]=new Vec2(2,2);
            var command=w.NewCommand(AuthoringOperation.EditPaintLayers(context,change));Ok(commands.Execute(command));string after=w.Document.StateHash;
            Equal(revision+1,w.Document.DocumentRevision);Equal((byte)128,Layer().Mask.GetCoverage(32,32));True(pixels.SequenceEqual(Layer().Image.CopyRgba()));
            Ok(commands.Execute(command));Equal(revision+1,w.Document.DocumentRevision);
            Code("PAINT_CONTEXT_STALE",commands.Execute(w.NewCommand(AuthoringOperation.EditPaintLayers(context,change))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("mask-stroke-native");ProjectStore.Save(dir,w,0);var read=ProjectStore.Open(dir);
            Equal(after,read.Document.StateHash);Equal((byte)128,read.Document.Objects[0].Graph.Nodes[f.paint].LayerStack.Layers[0].Mask.GetCoverage(32,32));
        });
    }
}
