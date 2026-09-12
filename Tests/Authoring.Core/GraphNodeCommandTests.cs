using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunGraphNodeCommandTests()
    {
        Test("node commands build edit save and undo from empty project", () =>
        {
            var w = AuthoringWorkspace.CreateEmpty(); var service = new AuthoringCommandService(w);
            var graph = new AuthoringGraph(GraphId(), Array.Empty<GraphNode>(), Array.Empty<GraphEdge>(), "");
            Ok(service.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph)))); False(w.Preview.IsComplete);
            string plane = GraphId(), edit = GraphId(), output = GraphId();
            Ok(service.Execute(w.NewCommand(AuthoringOperation.AddNode(GraphNode.Plane(plane)), AuthoringOperation.AddNode(GraphNode.Edit(edit)),
                AuthoringOperation.AddNode(GraphNode.Output(output)), AuthoringOperation.SetOutput(output),
                AuthoringOperation.Connect(new GraphEdge(plane, "mesh", edit, "mesh")), AuthoringOperation.Connect(new GraphEdge(edit, "mesh", output, "mesh")))));
            True(w.Preview.IsComplete); var context = GraphEditing.Context(w.Document.Objects[0].Graph, edit);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.TranslateGraphVertices(context, new[] { 0 }, new Vec3(.01f, 0, 0)))));
            Near(-.09f, w.Evaluate().Positions[0].X);
            string dir = Dir("node-command-save"); ProjectStore.Save(dir, w, 0); Equal(w.Document.StateHash, ProjectStore.Open(dir).Document.StateHash);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.RemoveNode(output)))); False(w.Preview.IsComplete); True(w.Preview.IsStale);
            Equal(1, w.Document.Objects[0].Graph.Edges.Count);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo()))); True(w.Preview.IsComplete);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo())));
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo()))); Equal(0, w.Document.Objects[0].Graph.Nodes.Count);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Undo()))); True(w.Document.IsEmpty); True(w.Preview.Output == null);
        });
        Test("node command batch rollback and stale direct edit context", () =>
        {
            var w = AuthoringWorkspace.CreateEmpty(); var service = new AuthoringCommandService(w);
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            var before = w.Document; var preview = w.Preview; string scalar = GraphId();
            var failed = service.Execute(w.NewCommand(AuthoringOperation.AddNode(GraphNode.Number(scalar, .5f)),
                AuthoringOperation.Disconnect(edit, "mesh"), AuthoringOperation.Connect(new GraphEdge(scalar, "value", edit, "mesh"))));
            False(failed.Success); True(ReferenceEquals(before, w.Document)); True(ReferenceEquals(preview, w.Preview));
            var context = GraphEditing.Context(graph, edit);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Plane(plane, .4f, .2f)))));
            Code("EDIT_CONTEXT_STALE", service.Execute(w.NewCommand(AuthoringOperation.TranslateGraphVertices(context, new[] { 0 }, new Vec3(.01f, 0, 0)))));
            Code("NODE_TYPE_CHANGED", service.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Number(plane, 1)))));
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Disconnect(edit, "mesh")))); False(w.Preview.IsComplete);
            Ok(service.Execute(w.NewCommand(AuthoringOperation.Connect(new GraphEdge(plane, "mesh", edit, "mesh"))))); True(w.Preview.IsComplete);
        });
    }
}
