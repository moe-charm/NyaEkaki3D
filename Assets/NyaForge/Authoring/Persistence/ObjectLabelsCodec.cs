using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring
{
    /// <summary>Versioned human-facing names keyed by stable authored object IDs.</summary>
    public static class ObjectLabelsCodec
    {
        const int Magic = 0x4C424F4E; // NOBL
        const int Version = 1;
        const int MaxObjects = 64;

        public static byte[] Write(IReadOnlyDictionary<string, string> labels)
        {
            Checks.Require(labels != null, "INVALID_OBJECT_LABELS", "Object labels are required.");
            var values = labels.Where(item => !string.IsNullOrWhiteSpace(item.Value))
                .OrderBy(item => item.Key, StringComparer.Ordinal).ToArray();
            Checks.Require(values.Length > 0 && values.Length <= MaxObjects,
                "INVALID_OBJECT_LABELS", "Object label count is outside the project capacity.");
            Checks.Require(values.Select(item => item.Key).Distinct(StringComparer.Ordinal).Count() == values.Length,
                "INVALID_OBJECT_LABELS", "Object label IDs must be unique.");
            foreach (var item in values) { Checks.Id(item.Key); Checks.Name(item.Value); }
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic); writer.Write(Version); writer.Write(values.Length);
                foreach (var item in values) { writer.Write(item.Key); writer.Write(item.Value); }
                var bytes = stream.ToArray();
                Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Object labels exceed capacity.");
                return bytes;
            }
        }

        public static IReadOnlyDictionary<string, string> Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes,
                "INVALID_OBJECT_LABELS", "Object labels are missing or too large.");
            try
            {
                using (var stream = new MemoryStream(bytes, false)) using (var reader = new BinaryReader(stream))
                {
                    Checks.Require(reader.ReadInt32() == Magic, "INVALID_OBJECT_LABELS", "Not object labels metadata.");
                    Checks.Require(reader.ReadInt32() == Version, "UNSUPPORTED_FORMAT", "Unsupported object labels metadata version.");
                    int count = reader.ReadInt32();
                    Checks.Require(count > 0 && count <= MaxObjects, "BUDGET_EXCEEDED", "Object label count is outside the project capacity.");
                    var values = new Dictionary<string, string>(StringComparer.Ordinal);
                    for (int i = 0; i < count; i++)
                    {
                        string id = reader.ReadString(), name = reader.ReadString();
                        Checks.Id(id); Checks.Name(name);
                        Checks.Require(values.TryAdd(id, name), "INVALID_OBJECT_LABELS", "Object label ID repeats.");
                    }
                    Checks.Require(stream.Position == stream.Length, "INVALID_OBJECT_LABELS", "Trailing object labels data is not allowed.");
                    return new ReadOnlyDictionary<string, string>(values);
                }
            }
            catch (EndOfStreamException error) { throw new AuthoringException("INVALID_OBJECT_LABELS", error.Message); }
            catch (IOException error) { throw new AuthoringException("INVALID_OBJECT_LABELS", error.Message); }
        }
    }
}
