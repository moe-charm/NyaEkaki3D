using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>GraphId-keyed expression sessions for multi-object authoring projects.</summary>
    public static class VrmExpressionSessionsCodec
    {
        const int Magic = 0x4558564E; // NVXE, little-endian
        const int Version = 1;
        const int MaxSessions = 64;

        public static byte[] Write(IReadOnlyDictionary<string, VrmExpressionSession> sessions)
        {
            Checks.Require(sessions != null && sessions.Count > 0 && sessions.Count <= MaxSessions, "INVALID_VRM", "An expression session table must contain one to 64 sessions.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(sessions.Count);
                foreach (var item in sessions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    Checks.Id(item.Key); Checks.Require(item.Value != null, "INVALID_VRM", "Expression session cannot be null.");
                    var payload = VrmExpressionSessionCodec.Write(item.Value);
                    writer.Write(item.Key); writer.Write(payload.Length); writer.Write(payload);
                }
                var bytes = stream.ToArray(); Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Expression session table exceeds capacity."); return bytes;
            }
        }

        public static IReadOnlyDictionary<string, VrmExpressionSession> Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "INVALID_VRM", "Expression session table is missing or too large.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_VRM", "Not an expression session table.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported expression session table version.");
                    int count = reader.ReadInt32(); Checks.Require(count > 0 && count <= MaxSessions, "BUDGET_EXCEEDED", "Expression session count exceeds capacity.");
                    var result = new Dictionary<string, VrmExpressionSession>(StringComparer.Ordinal);
                    for (int i = 0; i < count; i++)
                    {
                        string graphId = reader.ReadString(); Checks.Id(graphId); Checks.Require(!result.ContainsKey(graphId), "DUPLICATE_IMPORT", "Expression session graph identity repeats.");
                        int length = reader.ReadInt32(); Checks.Require(length > 0 && length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Expression session payload exceeds capacity.");
                        byte[] payload = reader.ReadBytes(length); Checks.Require(payload.Length == length, "INVALID_VRM", "Expression session table is truncated.");
                        result.Add(graphId, VrmExpressionSessionCodec.Read(payload));
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_VRM", "Trailing expression session table data is not allowed.");
                    return new ReadOnlyDictionary<string, VrmExpressionSession>(result);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_VRM", error.Message); }
            catch (IOException error) { throw new AuthoringException("INVALID_VRM", error.Message); }
        }

        public static bool IsTable(byte[] bytes) => bytes != null && bytes.Length >= 4 && BitConverter.ToInt32(bytes, 0) == Magic;
    }
}
