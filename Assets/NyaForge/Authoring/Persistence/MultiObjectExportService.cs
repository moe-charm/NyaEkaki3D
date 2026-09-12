using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace NyaForge.Authoring
{
    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class MultiObjectExportManifest
    {
        [JsonProperty(Required = Required.Always)] public int SchemaVersion = 1;
        [JsonProperty(Required = Required.Always)] public string Profile = MultiObjectExportService.Profile;
        [JsonProperty(Required = Required.Always)] public string Units = "meters";
        [JsonProperty(Required = Required.Always)] public string Coordinates = Storage.Coordinates;
        [JsonProperty(Required = Required.Always)] public string DocumentId;
        [JsonProperty(Required = Required.Always)] public string StateHash;
        [JsonProperty(Required = Required.Always)] public long DocumentRevision;
        [JsonProperty(Required = Required.Always)] public MultiObjectExportItem[] Objects;
    }

    [JsonObject(MemberSerialization.OptIn)]
    internal sealed class MultiObjectExportItem
    {
        [JsonProperty(Required = Required.Always)] public string ObjectId;
        [JsonProperty(Required = Required.Always)] public string Name;
        [JsonProperty(Required = Required.Always)] public string Kind;
        [JsonProperty(Required = Required.Always)] public string Manifest;
    }

    public sealed class MultiObjectExportEntry
    {
        public string ObjectId { get; }
        public string Name { get; }
        public ProjectExportKind Kind { get; }
        public string ManifestPath { get; }

        internal MultiObjectExportEntry(string objectId, string name, ProjectExportKind kind, string manifestPath)
        { ObjectId = objectId; Name = name; Kind = kind; ManifestPath = manifestPath; }
    }

    public sealed class MultiObjectExportDocument
    {
        public int SchemaVersion { get; }
        public string DocumentId { get; }
        public string StateHash { get; }
        public long DocumentRevision { get; }
        public IReadOnlyList<MultiObjectExportEntry> Objects { get; }

        internal MultiObjectExportDocument(MultiObjectExportManifest manifest, IReadOnlyList<MultiObjectExportEntry> objects)
        {
            SchemaVersion = manifest.SchemaVersion; DocumentId = manifest.DocumentId; StateHash = manifest.StateHash;
            DocumentRevision = manifest.DocumentRevision; Objects = objects;
        }
    }

    public sealed class MultiObjectExportResult
    {
        public string ManifestPath { get; }
        public string DocumentId { get; }
        public long Revision { get; }
        public string StateHash { get; }
        public int ObjectCount { get; }

        internal MultiObjectExportResult(string manifestPath, AuthoringDocument document)
        { ManifestPath = manifestPath; DocumentId = document.DocumentId; Revision = document.DocumentRevision; StateHash = document.StateHash; ObjectCount = document.Objects.Count; }
    }

    /// <summary>Packages one immutable multi-object document as independently readable object exports.</summary>
    public static class MultiObjectExportService
    {
        public const string Profile = "multi-object-package-v1";
        public const string ManifestName = "multi-object.nyaforge-bake.json";

        public static MultiObjectExportResult Export(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            directory = Storage.DirectoryPath(directory);
            lock (workspace.Gate)
            {
                Checks.Require(!workspace.Executing, "REENTRANT_EXPORT", "Cannot export during a projection transaction.");
                Checks.Require(workspace.InstanceId == instance, "STALE_INSTANCE", "Export targets another instance.");
                Checks.Require(workspace.Document.DocumentId == document, "DOCUMENT_CHANGED", "Export targets another document.");
                Checks.Require(workspace.Document.DocumentRevision == revision, "REVISION_CONFLICT", "Document changed before export.");
                Checks.Require(workspace.Document.Objects.Count > 1, "MULTI_OBJECT_REQUIRED", "Use the single-object export for a document with one object.");
                Checks.Require(!Directory.Exists(directory) && !File.Exists(directory), "EXPORT_DESTINATION_EXISTS", "Export destination already exists.");

                string staging = directory + ".staging-" + Guid.NewGuid().ToString("N");
                try
                {
                    Directory.CreateDirectory(staging);
                    var items = new List<MultiObjectExportItem>();
                    int index = 0;
                    foreach (var item in workspace.Document.Objects)
                    {
                        var singleDocument = new AuthoringDocument(workspace.Document.DocumentId, workspace.Document.Name,
                            workspace.Document.DocumentRevision, new[] { item }, item.ObjectId, false);
                        var singleWorkspace = new AuthoringWorkspace(singleDocument);
                        string objectDirectory = Path.Combine(staging, "objects", index.ToString("D2", System.Globalization.CultureInfo.InvariantCulture) + "-" + item.ObjectId);
                        var export = ProjectExportService.Export(singleWorkspace, singleWorkspace.InstanceId,
                            singleDocument.DocumentId, singleDocument.DocumentRevision, objectDirectory);
                        string relative = Path.GetRelativePath(staging, export.ManifestPath).Replace(Path.DirectorySeparatorChar, '/');
                        items.Add(new MultiObjectExportItem { ObjectId = item.ObjectId, Name = workspace.Document.Name, Kind = export.Kind.ToString(), Manifest = relative });
                        index++;
                    }
                    var manifest = new MultiObjectExportManifest
                    {
                        DocumentId = workspace.Document.DocumentId, StateHash = workspace.Document.StateHash,
                        DocumentRevision = workspace.Document.DocumentRevision, Objects = items.ToArray()
                    };
                    string manifestPath = Path.Combine(staging, ManifestName);
                    Storage.AtomicWrite(manifestPath, Storage.JsonBytes(manifest), false);
                    Directory.CreateDirectory(Path.GetDirectoryName(directory));
                    Directory.Move(staging, directory);
                    return new MultiObjectExportResult(Path.Combine(directory, ManifestName), workspace.Document);
                }
                catch
                {
                    if (Directory.Exists(staging)) Directory.Delete(staging, true);
                    throw;
                }
            }
        }

        public static MultiObjectExportDocument Read(string manifestPath)
        {
            manifestPath = Path.GetFullPath(manifestPath);
            var manifest = Storage.ReadJson<MultiObjectExportManifest>(manifestPath);
            Checks.Require(manifest.SchemaVersion == 1, "UNSUPPORTED_FORMAT", "Unsupported multi-object export version.");
            Checks.Require(manifest.Profile == Profile && manifest.Units == "meters" && manifest.Coordinates == Storage.Coordinates,
                "EXPORT_UNSUPPORTED_FEATURE", "Unsupported multi-object export profile.");
            Checks.Id(manifest.DocumentId); Checks.HashText(manifest.StateHash);
            Checks.Require(manifest.DocumentRevision >= 0 && manifest.DocumentRevision < long.MaxValue, "INVALID_REVISION", "Invalid document revision.");
            Checks.Require(manifest.Objects != null && manifest.Objects.Length > 1 && manifest.Objects.Length <= 64, "INVALID_MANIFEST", "A multi-object package needs two to 64 objects.");
            string root = Path.GetDirectoryName(manifestPath);
            var ids = new HashSet<string>(StringComparer.Ordinal);
            var result = new List<MultiObjectExportEntry>(manifest.Objects.Length);
            foreach (var item in manifest.Objects)
            {
                Checks.Require(item != null, "INVALID_MANIFEST", "Object entry is missing."); Checks.Id(item.ObjectId); Checks.Name(item.Name); Checks.Require(ids.Add(item.ObjectId), "DUPLICATE_OBJECT", "Object identity repeats.");
                Checks.Require(Enum.TryParse(item.Kind, out ProjectExportKind kind), "INVALID_MANIFEST", "Unknown object export kind.");
                string child = SafePath(root, item.Manifest);
                if (kind == ProjectExportKind.Mesh) BakeStore.Read(child);
                else if (kind == ProjectExportKind.Surface) SurfaceBakeStore.Read(child);
                else if (kind == ProjectExportKind.Material) MaterialBakeStore.Read(child);
                else MultiMaterialBakeStore.Read(child);
                result.Add(new MultiObjectExportEntry(item.ObjectId, item.Name, kind, child));
            }
            return new MultiObjectExportDocument(manifest, result);
        }

        static string SafePath(string root, string relative)
        {
            Checks.Require(!string.IsNullOrWhiteSpace(relative) && !Path.IsPathRooted(relative), "INVALID_MANIFEST", "Object manifest path must be relative.");
            string full = Path.GetFullPath(Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar)));
            string prefix = root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
            Checks.Require(full.StartsWith(prefix, StringComparison.OrdinalIgnoreCase), "INVALID_MANIFEST", "Object manifest path escapes the package.");
            return full;
        }
    }
}
