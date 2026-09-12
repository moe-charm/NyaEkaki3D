using System;
using System.IO;

namespace NyaForge.Authoring
{
    /// <summary>Coordinates atomic project saves. Version-specific data lives in codecs.</summary>
    public static class ProjectStore
    {
        public const string ManifestName = "project.nyaforge.json";
        public static long Save(string directory, AuthoringWorkspace workspace, long expectedSaveVersion)
        {
            if (workspace == null) throw new ArgumentNullException("workspace");
            directory = Storage.DirectoryPath(directory);
            lock (workspace.Gate)
            {
                Checks.Require(!workspace.Executing, "REENTRANT_SAVE", "Cannot save inside a projection transaction.");
                using (Storage.Lock(directory))
                {
                    string path = Path.Combine(directory, ManifestName);
                    long version = 0;
                    bool graphProfile = !workspace.Document.IsEmpty && !workspace.Document.Objects[0].IsStaticProfile;
                    if (File.Exists(path))
                    {
                        var token = Storage.ReadObject(path);
                        int schema = Storage.SchemaVersion(token);
                        if (schema == 3 && workspace.Document.IsEmpty) graphProfile = true;
                        Checks.Require(schema == (graphProfile ? 3 : 2), "MIGRATION_REQUIRED", "Save this project profile to a new directory; the original is preserved.");
                        string documentId;
                        if (graphProfile)
                        {
                            var old = GraphProjectCodec.ReadManifest(token); documentId = old.DocumentId; version = old.SaveVersion;
                        }
                        else
                        {
                            var old = ProjectCodec.ReadManifest(token); documentId = old.DocumentId; version = old.SaveVersion;
                        }
                        Checks.Require(documentId == workspace.Document.DocumentId, "SAVE_CONFLICT", "Destination belongs to another project.");
                    }
                    Checks.Require(expectedSaveVersion >= 0 && expectedSaveVersion == version, "SAVE_CONFLICT", "Project changed; reopen before overwriting.");
                    long nextVersion = checked(version + 1);
                    byte[] bytes;
                    if (graphProfile)
                    {
                        bytes = Storage.JsonBytes(GraphProjectCodec.Encode(workspace.Document, nextVersion));
                        foreach (var item in workspace.Document.Objects) GraphBlobStore.WriteLocked(directory, item.Graph);
                    }
                    else
                    {
                        bytes = Storage.JsonBytes(ProjectCodec.Encode(workspace.Document, nextVersion));
                        ProjectCodec.WriteBlobs(directory, workspace.Document);
                    }
                    Storage.AtomicWrite(path, bytes, true);
                    workspace.SaveVersion = nextVersion;
                    workspace.SavedDirectory = directory;
                    workspace.SavedStateHash = workspace.Document.StateHash;
                    return nextVersion;
                }
            }
        }
        public static AuthoringWorkspace Open(string directory)
        {
            directory = Storage.DirectoryPath(directory);
            var token = Storage.ReadObject(Path.Combine(directory, ManifestName));
            if (Storage.SchemaVersion(token) == 1) return LegacyProjectCodec.Read(directory, token);
            if (Storage.SchemaVersion(token) == 3)
            {
                var graphManifest = GraphProjectCodec.ReadManifest(token);
                var graphDoc = GraphProjectCodec.Decode(directory, graphManifest);
                return new AuthoringWorkspace(graphDoc) { SaveVersion = graphManifest.SaveVersion, SavedDirectory = directory, SavedStateHash = graphDoc.StateHash };
            }
            var manifest = ProjectCodec.ReadManifest(token);
            var doc = ProjectCodec.Decode(directory, manifest);
            return new AuthoringWorkspace(doc)
            {
                SaveVersion = manifest.SaveVersion, SavedDirectory = directory, SavedStateHash = doc.StateHash
            };
        }
    }
}
