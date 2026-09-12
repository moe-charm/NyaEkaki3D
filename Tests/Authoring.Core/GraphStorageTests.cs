using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

internal static partial class Program
{
    static string RawGraph(string directory, byte[] bytes)
    {
        string hash = Checks.Hash(bytes); Directory.CreateDirectory(Path.Combine(directory,"blobs"));
        File.WriteAllBytes(Path.Combine(directory,"blobs",hash + ".bin"),bytes); return hash;
    }
    static void RunGraphStorageTests()
    {
        Test("graph asset roundtrip preserves arbitrary connections and edits", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane,out edit);
            graph = GraphEditing.Translate(graph,GraphEditing.Context(graph,edit),new[] { 0 },new Vec3(.01f,0,0));
            var before = GraphEvaluator.Evaluate(graph);
            string dir = Dir("graph-storage"), hash = GraphBlobStore.Write(dir,graph);
            var loaded = GraphBlobStore.Read(dir,hash); var after = GraphEvaluator.Evaluate(loaded);
            True(after.IsComplete); Equal(before.Output.SnapshotHash,after.Output.SnapshotHash);
            Equal(graph.GraphId,loaded.GraphId); Equal(graph.OutputNodeId,loaded.OutputNodeId);
            Equal(2,loaded.Edges.Count); Equal(1,loaded.Nodes[edit].Offsets.Count);
            Equal(hash,GraphBlobStore.Write(dir,loaded));
            False(File.Exists(Path.Combine(dir,ProjectStore.ManifestName)));
        });
        Test("graph source and delta references preserve scale 100", () =>
        {
            var w = AuthoringWorkspace.CreateFixture(100); Ok(Edit(w,0,new Vec3(.01f,0,0)));
            var graph = w.Document.Objects[0].Graph; string dir = Dir("graph-source-storage");
            var loaded = GraphBlobStore.Read(dir,GraphBlobStore.Write(dir,graph));
            var result = GraphEvaluator.Evaluate(loaded);
            Near(-.09f,result.Output.Transform.ToAvatarPoint(result.Output.Mesh.Positions[0]).X);
            Equal(w.Evaluate().ContentHash,result.Output.Mesh.ContentHash);
        });
        Test("graph hash is independent of node and edge enumeration", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane,out edit);
            var reordered = new AuthoringGraph(graph.GraphId,graph.Nodes.Values.Reverse(),graph.Edges.Reverse(),graph.OutputNodeId);
            string dir = Dir("graph-canonical");
            Equal(GraphBlobStore.Write(dir,graph),GraphBlobStore.Write(dir,reordered));
        });
        Test("unknown node payload and incomplete graph survive disk roundtrip", () =>
        {
            string id = GraphId(), output = GraphId(); string payload = "将来のデータ\n{\"value\":123}\0";
            var graph = new AuthoringGraph(GraphId(),new[] { GraphNode.Unknown(id,"vendor.future",99,payload),GraphNode.Output(output) },
                new[] { new GraphEdge(id,"mesh",output,"mesh") },output);
            string dir = Dir("graph-unknown"), hash = GraphBlobStore.Write(dir,graph);
            var loaded = GraphBlobStore.Read(dir,hash);
            Equal(payload,loaded.Nodes[id].UnknownPayload); Equal(99,loaded.Nodes[id].Version);
            False(GraphEvaluator.Evaluate(loaded).IsComplete);
            var disconnected = graph.WithEdges(Array.Empty<GraphEdge>());
            var restored = GraphBlobStore.Read(dir,GraphBlobStore.Write(dir,disconnected));
            Equal(0,restored.Edges.Count); Equal(2,restored.Nodes.Count);
        });
        Test("future binary node payload survives without UTF8 interpretation", () =>
        {
            var payload = new byte[] { 255,254,0,128,1,2,3 };
            string id = GraphId(); var node = GraphNode.UnknownBinary(id,BuiltinNodes.MeshSource,2,payload);
            payload[0] = 0;
            var graph = new AuthoringGraph(GraphId(),new[] { node },Array.Empty<GraphEdge>(),"");
            string dir = Dir("graph-opaque"), hash = GraphBlobStore.Write(dir,graph);
            var loaded = GraphBlobStore.Read(dir,hash).Nodes[id];
            False(loaded.UnknownPayloadIsText); Equal((byte)255,loaded.UnknownPayloadBytes[0]);
            True(node.UnknownPayloadBytes.SequenceEqual(loaded.UnknownPayloadBytes));
            Equal(hash,GraphBlobStore.Write(dir,GraphBlobStore.Read(dir,hash)));
        });
        Test("stale graph edit remains unresolved after save rather than rebasing", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane,out edit);
            graph = GraphEditing.Translate(graph,GraphEditing.Context(graph,edit),new[] { 0 },new Vec3(.01f,0,0));
            graph = graph.ReplaceNode(GraphNode.Plane(plane,.4f,.1f));
            string dir = Dir("graph-unresolved"); var loaded = GraphBlobStore.Read(dir,GraphBlobStore.Write(dir,graph));
            Equal(graph.Nodes[edit].ExpectedInputSnapshot,loaded.Nodes[edit].ExpectedInputSnapshot);
            Equal(1,loaded.Nodes[edit].Offsets.Count);
            True(GraphEvaluator.Evaluate(loaded).Diagnostics.Any(d => d.Code == "EDIT_INPUT_CHANGED"));
        });
        Test("graph corruption and referenced geometry corruption are rejected", () =>
        {
            var graph = Fresh().Document.Objects[0].Graph; string dir = Dir("graph-corrupt");
            string hash = GraphBlobStore.Write(dir,graph), path = Path.Combine(dir,"blobs",hash + ".bin");
            var bytes = File.ReadAllBytes(path); bytes[bytes.Length - 1] ^= 1; File.WriteAllBytes(path,bytes);
            Expect("HASH_MISMATCH",() => GraphBlobStore.Read(dir,hash));
            string clean = Dir("graph-dependency-corrupt"); hash = GraphBlobStore.Write(clean,graph);
            var source = graph.Nodes.Values.First(n => n.SourceMesh != null);
            string dependency = Path.Combine(clean,"blobs",source.SourceMesh.ContentHash + ".bin");
            bytes = File.ReadAllBytes(dependency); bytes[bytes.Length - 1] ^= 1; File.WriteAllBytes(dependency,bytes);
            Expect("HASH_MISMATCH",() => GraphBlobStore.Read(clean,hash));
            Expect("HASH_MISMATCH",() => GraphBlobStore.Write(clean,graph));
        });
        Test("graph wire format rejects trailing bytes unknown version and oversized counts", () =>
        {
            string plane, edit; var graph = PlaneGraph(out plane,out edit); string dir = Dir("graph-wire");
            string hash = GraphBlobStore.Write(dir,graph); var bytes = File.ReadAllBytes(Path.Combine(dir,"blobs",hash + ".bin"));
            Expect("INVALID_BLOB",() => GraphBlobStore.Read(dir,RawGraph(dir,bytes.Concat(new byte[] { 0 }).ToArray())));
            var changed = (byte[])bytes.Clone(); Array.Copy(BitConverter.GetBytes(99),0,changed,4,4);
            Expect("UNSUPPORTED_FORMAT",() => GraphBlobStore.Read(dir,RawGraph(dir,changed)));
            changed = (byte[])bytes.Clone();
            int position = 8; position += 4 + BitConverter.ToInt32(changed,position); position += 4 + BitConverter.ToInt32(changed,position);
            Array.Copy(BitConverter.GetBytes(int.MaxValue),0,changed,position,4);
            Expect("BUDGET_EXCEEDED",() => GraphBlobStore.Read(dir,RawGraph(dir,changed)));
            Expect("INVALID_BLOB",() => GraphBlobStore.Read(dir,RawGraph(dir,bytes.Take(12).ToArray())));
        });
    }
}
