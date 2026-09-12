using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace NyaForge.Authoring
{
    /// <summary>Immutable owned metadata bytes, published with the project manifest.</summary>
    public sealed class ProjectAttachments
    {
        public const string Expressions = "vrm-expression-session.nyaforge.json";
        public const int MaxCount = 3;
        public const string Rig = "imported-rig-session.nyaforge.json";
        public const string Springs = "vrm-spring-session.nyaforge.json";
        public static readonly ProjectAttachments Empty = new ProjectAttachments(new Dictionary<string, byte[]>());
        readonly Dictionary<string, byte[]> values;
        public IReadOnlyDictionary<string, string> Hashes { get; }
        public string ContentHash { get; }

        public ProjectAttachments(IDictionary<string, byte[]> source)
        {
            Checks.Require(source != null && source.Count <= MaxCount, "INVALID_ATTACHMENT", "Expected at most three owned import metadata attachments.");
            values = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            var hashes = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var item in source.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                ValidateName(item.Key);
                Checks.Require(item.Value != null && item.Value.Length > 0 && item.Value.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "Attachment exceeds capacity.");
                var bytes = (byte[])item.Value.Clone();
                values.Add(item.Key, bytes); hashes.Add(item.Key, Checks.Hash(bytes));
            }
            Hashes = new ReadOnlyDictionary<string, string>(hashes);
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(1); writer.Write(hashes.Count);
                foreach (var item in hashes.OrderBy(p => p.Key, StringComparer.Ordinal)) { writer.Write(item.Key); writer.Write(item.Value); }
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        public byte[] Read(string name)
        {
            ValidateName(name);
            return values.TryGetValue(name, out var bytes) ? (byte[])bytes.Clone() : null;
        }

        internal static void ValidateName(string name)
        {
            Checks.Require(name == Expressions || name == Springs || name == Rig, "INVALID_ATTACHMENT", "Unknown project attachment name.");
        }
    }
}
