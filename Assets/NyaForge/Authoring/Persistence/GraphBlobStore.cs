using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    /// <summary>Immutable graph assets. Project manifests own the returned hash reference.</summary>
    public static class GraphBlobStore
    {
        public const long MaxReferencedBytes = 64L * 1024 * 1024;

        public static string Write(string directory, AuthoringGraph graph)
        {
            directory = Storage.DirectoryPath(directory);
            using (Storage.Lock(directory)) return WriteLocked(directory, graph);
        }

        internal static string WriteLocked(string directory, AuthoringGraph graph)
        {
            var blobs = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            long total = 0;
            string Add(byte[] bytes)
            {
                string hash = Checks.Hash(bytes);
                if (!blobs.ContainsKey(hash))
                {
                    total = checked(total + bytes.Length);
                    Checks.Require(total <= MaxReferencedBytes, "BUDGET_EXCEEDED", "Graph asset byte budget exceeded.");
                    blobs.Add(hash, bytes);
                }
                return hash;
            }
            byte[] graphBytes = GraphBinaryCodec.Encode(graph, Add);
            string graphHash = Add(graphBytes);
            // Publish dependencies first. The immutable graph blob is the last asset.
            foreach (var pair in blobs)
                if (pair.Key != graphHash) Storage.WriteBlob(directory, pair.Key, pair.Value);
            Storage.WriteBlob(directory, graphHash, graphBytes);
            return graphHash;
        }

        public static AuthoringGraph Read(string directory, string hash)
        {
            directory = Storage.DirectoryPath(directory);
            var blobs = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            long total = 0;
            byte[] Read(string reference)
            {
                byte[] bytes;
                if (blobs.TryGetValue(reference, out bytes)) return bytes;
                bytes = Storage.ReadBlob(directory, reference);
                total = checked(total + bytes.Length);
                Checks.Require(total <= MaxReferencedBytes, "BUDGET_EXCEEDED", "Graph asset byte budget exceeded.");
                blobs.Add(reference, bytes);
                return bytes;
            }
            return GraphBinaryCodec.Decode(Read(hash), Read);
        }
    }
}
