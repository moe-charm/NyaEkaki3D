using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static void RunPolygonStorageTests()
    {
        Test("polygon graph native save preserves cage and refuses render-index editing", () =>
        {
            var polygon = TriangleMeshAdapter.Import(GraphId(), AuthoringFixtures.Panel(1));
            string source = GraphId(), edit = GraphId(), sink = GraphId();
            var graph = new AuthoringGraph(GraphId(), new[] { GraphNode.Polygon(source, polygon, new RestTransform(1, new Vec3())), GraphNode.Edit(edit), GraphNode.Output(sink) },
                new[] { new GraphEdge(source, "mesh", edit, "mesh"), new GraphEdge(edit, "mesh", sink, "mesh") }, sink);
            var w = AuthoringWorkspace.CreateEmpty(); var commands = new AuthoringCommandService(w); Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            string directory = Dir("polygon-native"); ProjectStore.Save(directory, w, 0); var loaded = ProjectStore.Open(directory);
            Equal(w.Document.StateHash, loaded.Document.StateHash); Equal(polygon.DomainId, loaded.Preview.Output.Polygon.DomainId);
            Equal(polygon.Faces.Count, loaded.Preview.Output.Polygon.Faces.Count);
            True(loaded.Preview.Output.PolygonRendering.RenderVertexMap.Count > 0);
            Expect("EDIT_MODE_UNSUPPORTED", () => GraphEditing.Context(loaded.Document.Objects[0].Graph, edit));
            Equal(loaded.Evaluate().ContentHash, BakeStore.Read(BakeStore.Export(Dir("polygon-native-bake"), loaded)).MeshContentHash);
        });
        Test("polygon asset roundtrip preserves stable identity and all corner data", () =>
        {
            var mesh = TriangleMeshAdapter.Import(GraphId(), AuthoringFixtures.Panel(100)); string dir = Dir("polygon-assets");
            string hash = PolygonBlobStore.Write(dir, mesh); var loaded = PolygonBlobStore.Read(dir, hash);
            Equal(mesh.DomainId, loaded.DomainId); Equal(PolygonRenderAdapter.Build(mesh).Mesh.ContentHash, PolygonRenderAdapter.Build(loaded).Mesh.ContentHash);
            True(mesh.Faces.SelectMany(f => f.Corners).Select(c => c.Id).SequenceEqual(loaded.Faces.SelectMany(f => f.Corners).Select(c => c.Id)));
            var reordered = new PolygonMesh(mesh.DomainId, mesh.Vertices.Values.Reverse(), mesh.Faces.Reverse());
            Equal(hash, PolygonBlobStore.Write(dir, reordered));
            False(System.IO.File.Exists(System.IO.Path.Combine(dir, ProjectStore.ManifestName)));
        });
        Test("polygon storage rejects forged envelopes before materializing elements", () =>
        {
            var mesh = TriangleMeshAdapter.Import(GraphId(), AuthoringFixtures.Panel(1)); var bytes = PolygonBinaryCodec.Write(mesh);
            Expect("INVALID_BLOB", () => PolygonBinaryCodec.Read(bytes.Concat(new byte[] { 0 }).ToArray()));
            Expect("INVALID_BLOB", () => PolygonBinaryCodec.Read(bytes.Take(60).ToArray()));
            var changed = (byte[])bytes.Clone(); Array.Copy(BitConverter.GetBytes(int.MaxValue), 0, changed, 44, 4);
            Expect("BUDGET_EXCEEDED", () => PolygonBinaryCodec.Read(changed));
            changed = (byte[])bytes.Clone(); Array.Copy(BitConverter.GetBytes(8), 0, changed, 52, 4);
            Expect("INVALID_BLOB", () => PolygonBinaryCodec.Read(changed));
        });
    }
}
