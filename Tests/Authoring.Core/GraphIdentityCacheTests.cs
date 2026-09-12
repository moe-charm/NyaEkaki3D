using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunGraphIdentityCacheTests()
    {
        Test("cached graph identity matches canonical writer and never suppresses persistence blobs",()=>
        {
            string source=Guid.NewGuid().ToString("D"),paint=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
            var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,PolygonPrimitives.Plane(Guid.NewGuid().ToString("D")),new RestTransform(1,new Vec3())),GraphNode.Paint(paint),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(source,"mesh",output,"mesh"),new GraphEdge(paint,"image",output,"baseColor")},output);
            var context=PaintEditing.Context(graph,paint);
            var layer=new PaintLayer(Guid.NewGuid().ToString("D"),"Identity",new PaintImage(8,8,new Rgba32(255,0,0)));
            var stack=new PaintLayers(8,8,new[]{layer});graph=graph.ReplaceNode(GraphNode.LayeredPaint(paint,stack,context.UvHash,context.MeshDomain));
            string first=GraphContentIdentity.Hash(graph);Equal(first,Checks.Hash(GraphBinaryCodec.Encode(graph,Checks.Hash)));
            Equal(first,GraphContentIdentity.Hash(graph));
            var blobs=new Dictionary<string,byte[]>();int calls=0;
            string Write(byte[] bytes) { calls++;string hash=Checks.Hash(bytes);blobs[hash]=bytes;return hash; }
            var encoded=GraphBinaryCodec.Encode(graph,Write);int expected=calls;True(expected>=3);
            Equal(first,Checks.Hash(encoded));calls=0;GraphBinaryCodec.Encode(graph,Write);Equal(expected,calls);
            var restored=GraphBinaryCodec.Decode(encoded,hash=>blobs[hash]);Equal(first,GraphContentIdentity.Hash(restored));
            var changed=graph.ReplaceNode(GraphNode.LayeredPaint(paint,stack.Replace(layer.WithImage(new PaintImage(8,8,new Rgba32(0,0,255)))),context.UvHash,context.MeshDomain));
            False(first==GraphContentIdentity.Hash(changed));Equal(GraphContentIdentity.Hash(changed),Checks.Hash(GraphBinaryCodec.Encode(changed,Checks.Hash)));
            Equal(first,GraphContentIdentity.Hash(graph));
        });
        Test("optional command timing preserves commit and cached retry semantics",()=>
        {
            var workspace=AuthoringWorkspace.CreateEmpty();var service=new AuthoringCommandService(workspace);
            string source,edit;var graph=PlaneGraph(out source,out edit);var command=workspace.NewCommand(AuthoringOperation.AddGraph(graph));
            var timings=new CommandTimings();var result=service.Execute(command,timings:timings);Ok(result);
            string state=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;
            True(timings.TotalMs>=timings.ValidationMs+timings.CandidateMs+timings.EvaluationMs);
            True(ReferenceEquals(result,service.Execute(command,timings:timings)));Equal(state,workspace.Document.StateHash);Equal(revision,workspace.Document.DocumentRevision);
        });
    }
}
