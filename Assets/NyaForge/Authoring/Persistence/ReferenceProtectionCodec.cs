using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring
{
    /// <summary>Versioned list of authored object IDs that must remain reference-only.</summary>
    public static class ReferenceProtectionCodec
    {
        const int Magic = 0x5046524E; // NRFP
        const int Version = 1;
        const int MaxObjects = 64;

        public static byte[] Write(IEnumerable<string> objectIds)
        {
            Checks.Require(objectIds != null, "INVALID_REFERENCE_PROTECTION", "Reference object IDs are required.");
            var ids = objectIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            Checks.Require(ids.Length > 0 && ids.Length <= MaxObjects, "INVALID_REFERENCE_PROTECTION", "Reference object count is outside the project capacity.");
            Checks.Require(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length, "INVALID_REFERENCE_PROTECTION", "Reference object IDs must be unique.");
            foreach (var id in ids) Checks.Id(id);
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(ids.Length);
                foreach (var id in ids) writer.Write(id);
                var bytes = stream.ToArray();
                Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Reference protection metadata exceeds capacity.");
                return bytes;
            }
        }

        public static IReadOnlyCollection<string> Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes,
                "INVALID_REFERENCE_PROTECTION", "Reference protection metadata is missing or too large.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_REFERENCE_PROTECTION", "Not reference protection metadata.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported reference protection metadata version.");
                    int count = reader.ReadInt32();
                    Checks.Require(count > 0 && count <= MaxObjects, "BUDGET_EXCEEDED", "Reference object count is outside the project capacity.");
                    var ids = new List<string>(count);
                    for (int i = 0; i < count; i++)
                    {
                        string id = reader.ReadString(); Checks.Id(id);
                        Checks.Require(!ids.Contains(id, StringComparer.Ordinal), "INVALID_REFERENCE_PROTECTION", "Reference object ID repeats.");
                        ids.Add(id);
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_REFERENCE_PROTECTION", "Trailing reference protection data is not allowed.");
                    return new ReadOnlyCollection<string>(ids);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_REFERENCE_PROTECTION", error.Message); }
            catch (IOException error) { throw new AuthoringException("INVALID_REFERENCE_PROTECTION", error.Message); }
        }
    }
}
