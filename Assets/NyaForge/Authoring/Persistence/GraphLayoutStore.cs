using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public readonly struct CanvasPoint
    {
        public readonly float X, Y;
        public CanvasPoint(float x, float y)
        {
            Checks.Finite(x); Checks.Finite(y);
            Checks.Require(x >= 0 && x <= 1920 && y >= 0 && y <= 100000, "INVALID_LAYOUT", "Canvas position is outside bounds.");
            X = x; Y = y;
        }
    }

    /// <summary>Local view state, separate from project save versions, hashes and Undo.</summary>
    public static class GraphLayoutStore
    {
        const int Magic = 0x4c46594e, Version = 1;
        public static string FilePath(string directory, string documentId, string graphId)
        {
            Checks.Id(documentId); Checks.Id(graphId);
            return Path.Combine(Path.GetFullPath(directory), documentId + "_" + graphId + ".layout.bin");
        }

        public static void Save(string directory, string documentId, string graphId, IDictionary<string, CanvasPoint> positions)
        {
            string path = FilePath(directory, documentId, graphId);
            Checks.Require(positions != null && positions.Count <= AuthoringGraph.MaxNodes, "BUDGET_EXCEEDED", "Canvas node count exceeds budget.");
            byte[] bytes;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(new Guid(documentId).ToByteArray()); writer.Write(new Guid(graphId).ToByteArray());
                writer.Write(positions.Count);
                foreach (var pair in positions.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    Checks.Id(pair.Key); var point = new CanvasPoint(pair.Value.X, pair.Value.Y);
                    writer.Write(new Guid(pair.Key).ToByteArray()); writer.Write(point.X); writer.Write(point.Y);
                }
                bytes = stream.ToArray();
            }
            using (Storage.Lock(Path.GetDirectoryName(path))) Storage.AtomicWrite(path, bytes, true);
        }

        public static IReadOnlyDictionary<string, CanvasPoint> Load(string directory, string documentId, string graphId)
        {
            string path = FilePath(directory, documentId, graphId); var points = new Dictionary<string, CanvasPoint>();
            if (!File.Exists(path)) return new ReadOnlyDictionary<string, CanvasPoint>(points);
            byte[] bytes = Storage.ReadBounded(path, 44 + AuthoringGraph.MaxNodes * 24);
            Checks.Require(bytes.Length >= 44, "INVALID_LAYOUT", "Truncated canvas layout.");
            using (var stream = new MemoryStream(bytes)) using (var reader = new BinaryReader(stream))
            {
                Checks.Require(reader.ReadInt32() == Magic && reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported canvas layout.");
                Checks.Require(new Guid(reader.ReadBytes(16)).ToString("D") == documentId && new Guid(reader.ReadBytes(16)).ToString("D") == graphId,
                    "SOURCE_CHANGED", "Canvas belongs to another document or graph.");
                int count = reader.ReadInt32();
                Checks.Require(count >= 0 && count <= AuthoringGraph.MaxNodes && bytes.Length == 44 + count * 24, "INVALID_LAYOUT", "Canvas count or length differs.");
                for (int i = 0; i < count; i++)
                {
                    string id = new Guid(reader.ReadBytes(16)).ToString("D"); var point = new CanvasPoint(reader.ReadSingle(), reader.ReadSingle());
                    Checks.Require(!points.ContainsKey(id), "INVALID_LAYOUT", "Duplicate canvas node."); points.Add(id, point);
                }
            }
            return new ReadOnlyDictionary<string, CanvasPoint>(points);
        }
    }
}
