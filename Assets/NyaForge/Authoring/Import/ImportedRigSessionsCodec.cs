using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Versioned graph-to-rig session table for multi-object authoring projects.</summary>
    public static class ImportedRigSessionsCodec
    {
        const int Magic = 0x4752594E; // NYRG, little-endian
        const int Version = 1;
        const int MaxSessions = 64;

        public static byte[] Write(IReadOnlyDictionary<string, ImportedRigSession> sessions)
        {
            Checks.Require(sessions != null && sessions.Count > 0 && sessions.Count <= MaxSessions,
                "INVALID_IMPORT", "A rig session table must contain one to 64 sessions.");
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(sessions.Count);
                foreach (var item in sessions.OrderBy(pair => pair.Key, StringComparer.Ordinal))
                {
                    Checks.Id(item.Key);
                    Checks.Require(item.Value != null && item.Value.GraphId == item.Key,
                        "INVALID_IMPORT", "Rig session graph identity differs from its table key.");
                    var payload = ImportedRigSessionCodec.Write(item.Value);
                    Checks.Require(payload.Length > 0 && payload.Length <= AuthoringLimits.MaxBlobBytes,
                        "BUDGET_EXCEEDED", "Rig session payload exceeds capacity.");
                    writer.Write(item.Key); writer.Write(payload.Length); writer.Write(payload);
                }
                var bytes = stream.ToArray();
                Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Rig session table exceeds capacity.");
                return bytes;
            }
        }

        public static IReadOnlyDictionary<string, ImportedRigSession> Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes,
                "INVALID_IMPORT", "Rig session table is missing or too large.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_IMPORT", "Not a rig session table.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported rig session table version.");
                    int count = reader.ReadInt32(); Checks.Require(count > 0 && count <= MaxSessions, "BUDGET_EXCEEDED", "Rig session count exceeds capacity.");
                    var result = new Dictionary<string, ImportedRigSession>(StringComparer.Ordinal);
                    for (int i = 0; i < count; i++)
                    {
                        string graphId = reader.ReadString(); Checks.Id(graphId);
                        Checks.Require(!result.ContainsKey(graphId), "DUPLICATE_IMPORT", "Rig session graph identity repeats.");
                        int length = reader.ReadInt32(); Checks.Require(length > 0 && length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Rig session payload exceeds capacity.");
                        byte[] payload = reader.ReadBytes(length); Checks.Require(payload.Length == length, "INVALID_IMPORT", "Rig session table is truncated.");
                        var session = ImportedRigSessionCodec.Read(payload);
                        Checks.Require(session.GraphId == graphId, "INVALID_IMPORT", "Rig session graph identity differs from its table key.");
                        result.Add(graphId, session);
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_IMPORT", "Trailing rig session table data is not allowed.");
                    return new ReadOnlyDictionary<string, ImportedRigSession>(result);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
            catch (IOException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
        }
    }
}
