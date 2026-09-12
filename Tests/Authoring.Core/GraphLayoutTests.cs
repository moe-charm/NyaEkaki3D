using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;

internal static partial class Program
{
    static void RunGraphLayoutTests()
    {
        Test("canvas layout roundtrip is independent of document and save version", () =>
        {
            var w = Fresh(); string graph = w.Document.Objects[0].Graph.GraphId, node = GraphId(); var state = w.Document.StateHash;
            string directory = Dir("layout"); Equal(0, GraphLayoutStore.Load(directory, w.Document.DocumentId, graph).Count);
            GraphLayoutStore.Save(directory, w.Document.DocumentId, graph, new Dictionary<string, CanvasPoint> { [node] = new CanvasPoint(250, 12345) });
            var loaded = GraphLayoutStore.Load(directory, w.Document.DocumentId, graph); Near(250, loaded[node].X); Near(12345, loaded[node].Y);
            Equal(state, w.Document.StateHash); Equal(0L, w.SaveVersion); False(w.CanUndo);
            Equal(0, GraphLayoutStore.Load(directory, w.Document.DocumentId, GraphId()).Count);
        });
        Test("canvas rejects corrupt lengths identity and nonfinite coordinates", () =>
        {
            string dir = Dir("layout-invalid"), doc = GraphId(), graph = GraphId(), node = GraphId();
            GraphLayoutStore.Save(dir, doc, graph, new Dictionary<string, CanvasPoint> { [node] = new CanvasPoint(1, 2) });
            string path = GraphLayoutStore.FilePath(dir, doc, graph); byte[] original = File.ReadAllBytes(path);
            var bytes = (byte[])original.Clone(); bytes[40] = 2; File.WriteAllBytes(path, bytes);
            Expect("INVALID_LAYOUT", () => GraphLayoutStore.Load(dir, doc, graph));
            File.WriteAllBytes(path, original); string other = GraphId(); File.Copy(path, GraphLayoutStore.FilePath(dir, other, graph));
            Expect("SOURCE_CHANGED", () => GraphLayoutStore.Load(dir, other, graph));
            Expect("NON_FINITE", () => new CanvasPoint(float.NaN, 0));
            Expect("INVALID_LAYOUT", () => new CanvasPoint(-1, 0));
            Expect("INVALID_ID", () => GraphLayoutStore.FilePath(dir, "../escape", graph));
        });
    }
}
