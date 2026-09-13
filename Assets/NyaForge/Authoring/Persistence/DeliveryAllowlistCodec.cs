using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring
{
    /// <summary>Versioned explicit object IDs permitted in generic delivery exports.</summary>
    public static class DeliveryAllowlistCodec
    {
        const int Magic = 0x4C57414E; // NAWL
        const int Version = 1;
        const int MaxObjects = 64;

        public static byte[] Write(IEnumerable<string> objectIds)
        {
            Checks.Require(objectIds != null, "INVALID_DELIVERY_ALLOWLIST", "Delivery object IDs are required.");
            var ids = objectIds.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            Checks.Require(ids.Length > 0 && ids.Length <= MaxObjects, "INVALID_DELIVERY_ALLOWLIST", "Delivery object count is outside the project capacity.");
            Checks.Require(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length, "INVALID_DELIVERY_ALLOWLIST", "Delivery object IDs must be unique.");
            foreach (var id in ids) Checks.Id(id);
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(ids.Length);
                foreach (var id in ids) writer.Write(id);
                var bytes = stream.ToArray();
                Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Delivery allowlist metadata exceeds capacity.");
                return bytes;
            }
        }

        public static IReadOnlyCollection<string> Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes,
                "INVALID_DELIVERY_ALLOWLIST", "Delivery allowlist metadata is missing or too large.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_DELIVERY_ALLOWLIST", "Not delivery allowlist metadata.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported delivery allowlist metadata version.");
                    int count = reader.ReadInt32();
                    Checks.Require(count > 0 && count <= MaxObjects, "BUDGET_EXCEEDED", "Delivery object count is outside the project capacity.");
                    var ids = new List<string>(count);
                    for (int i = 0; i < count; i++)
                    {
                        string id = reader.ReadString(); Checks.Id(id);
                        Checks.Require(!ids.Contains(id, StringComparer.Ordinal), "INVALID_DELIVERY_ALLOWLIST", "Delivery object ID repeats.");
                        ids.Add(id);
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_DELIVERY_ALLOWLIST", "Trailing delivery allowlist data is not allowed.");
                    return new ReadOnlyCollection<string>(ids);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_DELIVERY_ALLOWLIST", error.Message); }
            catch (IOException error) { throw new AuthoringException("INVALID_DELIVERY_ALLOWLIST", error.Message); }
        }
    }
}
