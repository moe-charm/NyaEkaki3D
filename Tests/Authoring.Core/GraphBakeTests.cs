using System;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunGraphBakeTests()
    {
        Test("graph Bake exports final output and source provenance after reopen", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            string baseline = GraphEvaluator.Evaluate(graph).Output.Mesh.ContentHash;
            var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.TranslateGraphVertices(GraphEditing.Context(graph, edit), new[] { 0 }, new Vec3(.01f, 0, 0)))));
            string saved = Dir("graph-bake-project"); ProjectStore.Save(saved, w, 0); w = ProjectStore.Open(saved);
            var bake = BakeStore.Read(BakeStore.Export(Dir("graph-bake-export"), w));
            Equal(w.Evaluate().ContentHash, bake.MeshContentHash); Equal(baseline, bake.BaselineHash);
            True(bake.NormalsPreservedAfterPositionEdit); Near(-.09f, bake.Mesh.Positions[0].X);
        });
        Test("incomplete graph Bake does not export last good preview or create destination", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit); var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Disconnect(edit, "mesh")))); True(w.Preview.IsStale);
            string path = Path.Combine(Root, "incomplete-graph-export");
            Expect("GRAPH_INCOMPLETE", () => BakeStore.Export(path, w)); False(Directory.Exists(path));
        });
        foreach (float scale in new[] { 1f, 100f }) Test("graph source Bake scale " + scale, () =>
        {
            var w = Fresh(); var graph = w.Document.Objects[0].Graph;
            var source = AuthoringFixtures.Panel(scale); string sourceId = GraphId(), output = GraphId();
            graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Source(sourceId, source, new RestTransform(scale, new Vec3(3, 2, 1))), GraphNode.Output(output) },
                new[] { new GraphEdge(sourceId, "mesh", output, "mesh") }, output);
            w = new AuthoringWorkspace(w.Document.WithGraph(graph, 1));
            var bake = BakeStore.Read(BakeStore.Export(Dir("graph-source-bake-" + scale), w));
            Near(scale, bake.Transform.Scale); Near(3, bake.Transform.Translation.X); Equal(source.ContentHash, bake.BaselineHash);
            False(bake.NormalsPreservedAfterPositionEdit); Equal(2, bake.Mesh.Submeshes.Count);
        });
    }
}
