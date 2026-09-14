using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    /// <summary>Schema 4 wraps an unchanged schema 2/3 document and immutable metadata references.</summary>
    internal static class ProjectSnapshotCodec
    {
        internal static JObject Document(JObject root)
        {
            if (Storage.SchemaVersion(root) != 4) return root;
            Checks.Require(root.Count == 3 && root["project"] is JObject && root["attachments"] is JArray, "INVALID_MANIFEST", "Invalid project snapshot envelope.");
            var document = (JObject)root["project"];
            int version = Storage.SchemaVersion(document);
            Checks.Require(version == 2 || version == 3, "UNSUPPORTED_FORMAT", "Snapshot must contain a schema 2 or 3 project.");
            return document;
        }

        internal static byte[] Write(string directory, byte[] documentBytes, ProjectAttachments attachments)
        {
            var entries = new JArray();
            foreach (var item in attachments.Hashes.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                Storage.WriteBlob(directory, item.Value, attachments.Read(item.Key));
                entries.Add(new JObject { ["name"] = item.Key, ["hash"] = item.Value });
            }
            var document = JObject.Parse(System.Text.Encoding.UTF8.GetString(documentBytes));
            return Storage.JsonBytes(new JObject { ["schemaVersion"] = 4, ["project"] = document, ["attachments"] = entries });
        }

        internal static ProjectAttachments Read(string directory, JObject root)
        {
            var values = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            if (Storage.SchemaVersion(root) == 4)
            {
                Document(root);
                var entries = (JArray)root["attachments"];
                Checks.Require(entries.Count <= ProjectAttachments.MaxCount, "BUDGET_EXCEEDED", "Too many metadata attachments.");
                foreach (var token in entries)
                {
                    var entry = token as JObject;
                    Checks.Require(entry != null && entry.Count == 2 && entry["name"]?.Type == JTokenType.String && entry["hash"]?.Type == JTokenType.String, "INVALID_MANIFEST", "Invalid attachment reference.");
                    string name = (string)entry["name"], hash = (string)entry["hash"];
                    ProjectAttachments.ValidateName(name); Checks.HashText(hash);
                    Checks.Require(!values.ContainsKey(name), "INVALID_MANIFEST", "Repeated attachment reference.");
                    values.Add(name, Storage.ReadBlob(directory, hash));
                }
            }
            else
            {
                // Import old sidecars once. Schema 4 never consults these mutable
                // files, including after an attachment has been removed.
                foreach (string name in new[] { ProjectAttachments.Expressions, ProjectAttachments.Springs, ProjectAttachments.Rig, ProjectAttachments.RigSessions, ProjectAttachments.PhysBones, ProjectAttachments.SecondaryMotion, ProjectAttachments.ReferenceProtection, ProjectAttachments.DeliveryAllowlist, ProjectAttachments.ImportDiagnostics, ProjectAttachments.ObjectLabels })
                {
                    string path = Path.Combine(directory, name);
                    if (File.Exists(path)) values.Add(name, Storage.ReadBounded(path, AuthoringLimits.MaxBlobBytes));
                }
            }
            return new ProjectAttachments(values);
        }
    }
}
