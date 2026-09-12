using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonEditingTests()
    {
        foreach (float scale in new[] { 1f, 100f }) Test("PolygonEdit stable IDs keep source and roundtrip with Undo scale " + scale, () =>
        {
            var polygon = TriangleMeshAdapter.Import(GraphId(), AuthoringFixtures.Panel(scale));
            string source = GraphId(), edit = GraphId(), sink = GraphId();
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Polygon(source, polygon, new RestTransform(scale, new Vec3())), GraphNode.PolygonEdit(edit), GraphNode.Output(sink) },
                new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", sink, "mesh") }, sink);
            var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph)))); var context = GraphEditing.Context(graph, edit);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.TranslatePolygonVertices(context, new ulong[] { 1 }, new Vec3(.01f, 0, 0)))));
            Near(-.09f, w.Preview.Output.Transform.ToAvatarPoint(w.Preview.Output.Polygon.Vertices[1].Position).X);
            Near(-.1f, w.Document.Objects[0].Graph.Nodes[source].Transform.ToAvatarPoint(polygon.Vertices[1].Position).X);
            string hash = w.Document.StateHash; Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo()))); True(w.Document.Objects[0].Graph.Nodes[edit].SourcePolygon == null);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo()))); Equal(hash, w.Document.StateHash);
            string dir = Dir("polygon-edit-" + scale); ProjectStore.Save(dir, w, 0); var loaded = ProjectStore.Open(dir); Equal(hash, loaded.Document.StateHash);
            True(BakeStore.Read(BakeStore.Export(Dir("polygon-edit-bake-" + scale), loaded)).NormalsPreservedAfterPositionEdit);
            string before = w.Document.StateHash;
            Code("INVALID_SELECTION", commands.Execute(w.NewCommand(AuthoringOperation.TranslatePolygonVertices(context, new ulong[] { 9999 }, new Vec3(.01f, 0, 0)))));
            Equal(before, w.Document.StateHash);
            var upstream = polygon.MoveVertices(new ulong[] { 2 }, new Vec3(.01f / scale, 0, 0));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.UpdateNode(GraphNode.Polygon(source, upstream, new RestTransform(scale, new Vec3()))))));
            False(w.Preview.IsComplete);
            Code("EDIT_CONTEXT_STALE", commands.Execute(w.NewCommand(AuthoringOperation.TranslatePolygonVertices(context, new ulong[] { 1 }, new Vec3(.01f, 0, 0)))));
        });
    }
}
