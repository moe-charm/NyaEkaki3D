using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunGraphDocumentTests()
    {
        Test("document owns arbitrary graph and revision preserves its identity", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            var doc = Fresh().Document.WithGraph(graph, 1);
            False(doc.Objects[0].IsStaticProfile);
            True(ReferenceEquals(graph, doc.Objects[0].Graph));
            Equal(4, doc.Evaluate().VertexCount);
            Equal(doc.StateHash, doc.AtRevision(2).StateHash);
            var changed = doc.WithGraph(graph.ReplaceNode(GraphNode.Plane(plane, .4f, .2f)), 2);
            False(doc.StateHash == changed.StateHash);
            Equal(graph.Nodes[plane].Width, doc.Objects[0].Graph.Nodes[plane].Width);
            var reordered = new AuthoringGraph(graph.GraphId, graph.Nodes.Values.Reverse(), graph.Edges.Reverse(), graph.OutputNodeId);
            Equal(doc.StateHash, doc.WithGraph(reordered, 3).StateHash);
        });
        Test("incomplete document retains graph without pretending to be static", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit).WithEdges(Array.Empty<GraphEdge>());
            var doc = Fresh().Document.WithGraph(graph, 1);
            False(doc.Objects[0].EvaluateGraph().IsComplete);
            Equal(doc.StateHash, doc.AtRevision(2).StateHash);
            Expect("GRAPH_PROFILE_REQUIRED", () => { var ignored = doc.BaselineMesh; });
            Expect("GRAPH_PROFILE_REQUIRED", () => doc.Changed(2, true, new Dictionary<int, Vec3>()));
            Equal(0, doc.Objects[0].Graph.Edges.Count);
        });
    }
}
