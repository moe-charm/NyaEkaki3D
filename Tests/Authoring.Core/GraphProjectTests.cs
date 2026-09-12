using System;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunGraphProjectTests()
    {
        Test("native graph project roundtrip and optimistic save conflict", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit);
            var workspace = new AuthoringWorkspace(Fresh().Document.WithGraph(graph, 1));
            string dir = Dir("native-graph"); Equal(1L, ProjectStore.Save(dir, workspace, 0));
            var loaded = ProjectStore.Open(dir); False(loaded.IsDirty);
            Equal(workspace.Document.StateHash, loaded.Document.StateHash);
            Equal(workspace.Evaluate().ContentHash, loaded.Evaluate().ContentHash);
            Equal(3, (int)JObject.Parse(File.ReadAllText(Path.Combine(dir, ProjectStore.ManifestName)))["schemaVersion"]);
            workspace.Document = workspace.Document.WithGraph(graph.ReplaceNode(GraphNode.Plane(plane, .4f, .2f)), 2);
            True(workspace.IsDirty); Equal(2L, ProjectStore.Save(dir, workspace, 1));
            Expect("SAVE_CONFLICT", () => ProjectStore.Save(dir, loaded, 1));
            Equal(workspace.Document.StateHash, ProjectStore.Open(dir).Document.StateHash);
        });
        Test("native incomplete unknown graph preserves payload and rejects corrupt references", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane, out edit).WithEdges(Array.Empty<GraphEdge>());
            graph = graph.ReplaceNode(GraphNode.UnknownBinary(plane, "future.primitive", 2, new byte[] { 255, 0, 128 }));
            var workspace = new AuthoringWorkspace(Fresh().Document.WithGraph(graph, 1));
            string dir = Dir("native-incomplete"); ProjectStore.Save(dir, workspace, 0);
            var loaded = ProjectStore.Open(dir);
            False(loaded.Document.Objects[0].EvaluateGraph().IsComplete);
            Equal((byte)255, loaded.Document.Objects[0].Graph.Nodes[plane].UnknownPayloadBytes[0]);
            Equal(workspace.Document.StateHash, loaded.Document.StateHash);
            string path = Path.Combine(dir, ProjectStore.ManifestName);
            var token = JObject.Parse(File.ReadAllText(path)); token["objects"][0]["graphHash"] = "../../escape";
            File.WriteAllText(path, token.ToString()); Expect("INVALID_HASH", () => ProjectStore.Open(dir));
        });
        Test("static to graph migration requires new directory and preserves original", () =>
        {
            var workspace = Fresh(); string old = Dir("graph-migration-old"); ProjectStore.Save(old, workspace, 0);
            byte[] before = File.ReadAllBytes(Path.Combine(old, ProjectStore.ManifestName));
            string plane, edit; workspace.Document = workspace.Document.WithGraph(PlaneGraph(out plane, out edit), 1);
            Expect("MIGRATION_REQUIRED", () => ProjectStore.Save(old, workspace, 1));
            Equal(Convert.ToBase64String(before), Convert.ToBase64String(File.ReadAllBytes(Path.Combine(old, ProjectStore.ManifestName))));
            string target = Dir("graph-migration-new"); ProjectStore.Save(target, workspace, 0);
            True(ProjectStore.Open(old).Document.Objects[0].IsStaticProfile);
            False(ProjectStore.Open(target).Document.Objects[0].IsStaticProfile);
        });
    }
}
