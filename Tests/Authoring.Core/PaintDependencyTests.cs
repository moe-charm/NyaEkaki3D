using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunPaintDependencyTests()
    {
        Test("paint dependencies include branched retained images but exclude unrelated and blank nodes", () =>
        {
            var f = PaintFixture();
            Equal(0,PaintDependencies.DownstreamImages(f.graph,f.source).Count);
            var painted = PaintEditing.Stroke(f.graph,PaintEditing.Context(f.graph,f.paint),new[]{new Vec2(.5f,.5f)},4,new Rgba32(255,0,0));
            var saved = painted.Nodes[f.paint]; string second = GraphId(), unrelated = GraphId(), blank = GraphId(), mirror = GraphId();
            var nodes = painted.Nodes.Values.Concat(new[] {
                GraphNode.Paint(second,64,64,saved.PaintImage,saved.PaintUvHash,saved.ExpectedDomain),
                GraphNode.Paint(unrelated,64,64,saved.PaintImage,saved.PaintUvHash,saved.ExpectedDomain),GraphNode.Paint(blank),GraphNode.Mirror(mirror) });
            var graph = new AuthoringGraph(painted.GraphId,nodes,painted.Edges.Concat(new[]{
                new GraphEdge(f.source,"mesh",mirror,"mesh"),new GraphEdge(mirror,"mesh",second,"mesh"),new GraphEdge(f.source,"mesh",blank,"mesh") }),f.output);
            var before = GraphBinaryCodec.Encode(graph, Checks.Hash);
            var expected = new[]{f.paint,second}.OrderBy(id=>id,StringComparer.Ordinal);
            True(expected.SequenceEqual(PaintDependencies.DownstreamImages(graph,f.source)));
            True(new[]{second}.SequenceEqual(PaintDependencies.DownstreamImages(graph,mirror)));
            True(before.SequenceEqual(GraphBinaryCodec.Encode(graph, Checks.Hash)));
            Expect("NODE_NOT_FOUND",()=>PaintDependencies.DownstreamImages(graph,GraphId()));
        });
    }
}



