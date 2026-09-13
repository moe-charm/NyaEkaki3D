using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static void RunOriginalImageGraphTests()
    {
        Test("original image source survives graph wire roundtrip with immutable bytes", () =>
        {
            string paintId = Guid.NewGuid().ToString("D");
            string sourceId = Guid.NewGuid().ToString("D");
            byte[] encoded = PaintPng.Encode(new PaintImage(4, 2, new Rgba32(12, 34, 56, 255)));
            var source = new GraphOriginalImage(paintId, 4096, 2048, "image/png", encoded);
            var graph = new AuthoringGraph(Guid.NewGuid().ToString("D"),
                new[] { GraphNode.Paint(paintId, 4, 2, new PaintImage(4, 2, new Rgba32(12, 34, 56, 255))), GraphNode.OriginalImageNode(sourceId, source) },
                Array.Empty<GraphEdge>(), "");
            var blobs = new Dictionary<string, byte[]>();
            byte[] wire = GraphBinaryCodec.Encode(graph, bytes => { string hash = Checks.Hash(bytes); blobs[hash] = bytes; return hash; });
            var restored = GraphBinaryCodec.Decode(wire, hash => blobs[hash]);
            var restoredSource = restored.Nodes[sourceId].OriginalImage;
            True(restoredSource != null);
            Equal(paintId, restoredSource.PaintNodeId);
            Equal(4096, restoredSource.Width); Equal(2048, restoredSource.Height);
            Equal("image/png", restoredSource.MimeType); Equal(encoded.Length, restoredSource.EncodedByteCount);
            Equal("", restoredSource.PreviewImageHash);
            True(encoded.SequenceEqual(restoredSource.CopyEncodedBytes()));
            Equal(GraphContentIdentity.Hash(graph), GraphContentIdentity.Hash(restored));
            encoded[encoded.Length - 1] ^= 0x7f;
            True(!encoded.SequenceEqual(restoredSource.CopyEncodedBytes()));
        });

        Test("original image source rejects unsupported format and unsafe dimensions", () =>
        {
            byte[] bytes = new byte[] { 1, 2, 3 };
            Expect("UNSUPPORTED_FORMAT", () => new GraphOriginalImage(Guid.NewGuid().ToString("D"), 1, 1, "image/webp", bytes));
            Expect("IMAGE_DIMENSION_EXCEEDED", () => new GraphOriginalImage(Guid.NewGuid().ToString("D"), 8193, 1, "image/png", bytes));
            Expect("IMAGE_BUDGET_EXCEEDED", () => new GraphOriginalImage(Guid.NewGuid().ToString("D"), 1, 1, "image/png", Array.Empty<byte>()));
        });

        Test("graph inspection exposes original image provenance without raw bytes", () =>
        {
            string paintId = GraphId(); string originalId = GraphId();
            var graph = new AuthoringGraph(GraphId(), new[] {
                GraphNode.Paint(paintId, 4, 2, new PaintImage(4, 2, new Rgba32(12, 34, 56, 255))),
                GraphNode.OriginalImageNode(originalId, new GraphOriginalImage(paintId, 4096, 2048, "image/jpeg", new byte[] { 4, 5, 6 }))
            }, Array.Empty<GraphEdge>(), "");
            var workspace = AuthoringWorkspace.CreateEmpty();
            Ok(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph))));
            var node = AuthoringGraphReader.Read(workspace, workspace.InstanceId)["graph"]["nodes"].Single(item => (string)item["nodeId"] == originalId);
            Equal(paintId, (string)node["originalImage"]["paintNodeId"]);
            Equal(4096, (int)node["originalImage"]["width"]); Equal(2048, (int)node["originalImage"]["height"]);
            Equal("image/jpeg", (string)node["originalImage"]["mimeType"]); Equal(3, (int)node["originalImage"]["encodedByteCount"]);
            Equal(Checks.Hash(new byte[] { 4, 5, 6 }), (string)node["originalImage"]["contentHash"]);
            True(node["originalImage"]["bytes"] == null);
        });
    }
}
