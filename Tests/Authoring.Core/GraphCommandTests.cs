using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunGraphCommandTests()
    {
        Test("graph command shares Undo and retains explicitly stale preview", () =>
        {
            var w = Fresh(); var service = new AuthoringCommandService(w);
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            var command = w.NewCommand(AuthoringOperation.ReplaceGraph(graph));
            var committed = service.Execute(command); Ok(committed); True(committed.EvaluationComplete);
            True(ReferenceEquals(committed, service.Execute(command)));
            string mesh = w.Preview.Output.Mesh.ContentHash;
            var incomplete = service.Execute(w.NewCommand(AuthoringOperation.ReplaceGraph(graph.WithEdges(Array.Empty<GraphEdge>()))));
            Ok(incomplete); Equal("COMMITTED_INCOMPLETE", incomplete.Code); False(incomplete.EvaluationComplete);
            True(incomplete.MeshContentHash == null); True(w.Preview.IsStale); Equal(1L, w.Preview.OutputRevision.Value);
            Equal(mesh, w.Preview.Output.Mesh.ContentHash);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo()))); True(w.Preview.IsComplete); False(w.Preview.IsStale);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Redo()))); False(w.Preview.IsComplete);
            string dir = Dir("graph-command-incomplete"); ProjectStore.Save(dir, w, 0);
            var reopened = ProjectStore.Open(dir); False(reopened.Preview.IsComplete); True(reopened.Preview.Output == null);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo())));
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo()))); True(w.Document.Objects[0].IsStaticProfile);
        });
        Test("graph command rejection preserves document history and preview", () =>
        {
            var w = Fresh(); var service = new AuthoringCommandService(w); var before = w.Document; var preview = w.Preview;
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            Code("GRAPH_PROJECTION_REQUIRED", service.Execute(w.NewCommand(AuthoringOperation.ReplaceGraph(graph)), new ProbeProjection()));
            True(ReferenceEquals(before, w.Document)); True(ReferenceEquals(preview, w.Preview)); False(w.CanUndo);
            var command = w.NewCommand(AuthoringOperation.ReplaceGraph(graph)); Ok(service.Execute(command));
            command.Operations = new[] { AuthoringOperation.ReplaceGraph(graph.ReplaceNode(GraphNode.Plane(plane, .4f, .2f))) };
            Code("COMMAND_ID_REUSED", service.Execute(command)); Equal(1L, w.Document.DocumentRevision);
        });
    }
}
