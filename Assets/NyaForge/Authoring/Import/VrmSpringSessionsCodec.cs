using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>GraphId-keyed SpringBone sessions for multi-object authoring projects.</summary>
    public static class VrmSpringSessionsCodec
    {
        const int Magic = 0x5358564E; // NVXS, little-endian
        const int Version = 1;
        const int MaxSessions = 64;

        public static byte[] Write(IReadOnlyDictionary<string, VrmSpringSession> sessions)
        {
            Checks.Require(sessions != null && sessions.Count > 0 && sessions.Count <= MaxSessions, "INVALID_VRM", "A Spring session table must contain one to 64 sessions.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(sessions.Count);
                foreach (var item in sessions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    Checks.Id(item.Key); Checks.Require(item.Value != null, "INVALID_VRM", "Spring session cannot be null.");
                    var payload = VrmSpringSessionCodec.Write(item.Value);
                    writer.Write(item.Key); writer.Write(payload.Length); writer.Write(payload);
                }
                var bytes = stream.ToArray(); Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Spring session table exceeds capacity."); return bytes;
            }
        }

        public static IReadOnlyDictionary<string, VrmSpringSession> Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "INVALID_VRM", "Spring session table is missing or too large.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_VRM", "Not a Spring session table.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported Spring session table version.");
                    int count = reader.ReadInt32(); Checks.Require(count > 0 && count <= MaxSessions, "BUDGET_EXCEEDED", "Spring session count exceeds capacity.");
                    var result = new Dictionary<string, VrmSpringSession>(StringComparer.Ordinal);
                    for (int i = 0; i < count; i++)
                    {
                        string graphId = reader.ReadString(); Checks.Id(graphId); Checks.Require(!result.ContainsKey(graphId), "DUPLICATE_IMPORT", "Spring session graph identity repeats.");
                        int length = reader.ReadInt32(); Checks.Require(length > 0 && length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Spring session payload exceeds capacity.");
                        byte[] payload = reader.ReadBytes(length); Checks.Require(payload.Length == length, "INVALID_VRM", "Spring session table is truncated.");
                        result.Add(graphId, VrmSpringSessionCodec.Read(payload));
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_VRM", "Trailing Spring session table data is not allowed.");
                    return new ReadOnlyDictionary<string, VrmSpringSession>(result);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_VRM", error.Message); }
            catch (IOException error) { throw new AuthoringException("INVALID_VRM", error.Message); }
        }

        public static bool IsTable(byte[] bytes) => bytes != null && bytes.Length >= 4 && BitConverter.ToInt32(bytes, 0) == Magic;
    }
}
