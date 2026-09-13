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
                    bool snapshot = workspace.Attachments.Hashes.Count > 0 || workspace.SavedAttachmentsHash != ProjectAttachments.Empty.ContentHash
                        || File.Exists(Path.Combine(directory, ProjectAttachments.Expressions)) || File.Exists(Path.Combine(directory, ProjectAttachments.Springs)) || File.Exists(Path.Combine(directory, ProjectAttachments.Rig)) || File.Exists(Path.Combine(directory, ProjectAttachments.RigSessions)) || File.Exists(Path.Combine(directory, ProjectAttachments.PhysBones)) || File.Exists(Path.Combine(directory, ProjectAttachments.SecondaryMotion)) || File.Exists(Path.Combine(directory, ProjectAttachments.ReferenceProtection)) || File.Exists(Path.Combine(directory, ProjectAttachments.DeliveryAllowlist)) || File.Exists(Path.Combine(directory, ProjectAttachments.ImportDiagnostics));
                    bool graphProfile = !workspace.Document.IsEmpty && !workspace.Document.ActiveObject.IsStaticProfile;
                    if (File.Exists(path))
                    {
                        var token = Storage.ReadObject(path);
                        snapshot |= Storage.SchemaVersion(token) == 4;
                        token = ProjectSnapshotCodec.Document(token);
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
                    if (snapshot) bytes = ProjectSnapshotCodec.Write(directory, bytes, workspace.Attachments);
                    Storage.AtomicWrite(path, bytes, true);
                    workspace.SaveVersion = nextVersion;
                    workspace.SavedDirectory = directory;
                    workspace.SavedStateHash = workspace.Document.StateHash;
                    workspace.SavedAttachmentsHash = workspace.Attachments.ContentHash;
                    return nextVersion;
                }
            }
        }
        public static AuthoringWorkspace Open(string directory)
        {
            directory = Storage.DirectoryPath(directory);
            var token = Storage.ReadObject(Path.Combine(directory, ManifestName));
            var attachments = ProjectSnapshotCodec.Read(directory, token);
            token = ProjectSnapshotCodec.Document(token);
            if (Storage.SchemaVersion(token) == 1) return RestoreAttachments(LegacyProjectCodec.Read(directory, token), attachments);
            if (Storage.SchemaVersion(token) == 3)
            {
                var graphManifest = GraphProjectCodec.ReadManifest(token);
                var graphDoc = GraphProjectCodec.Decode(directory, graphManifest);
                return RestoreAttachments(new AuthoringWorkspace(graphDoc) { SaveVersion = graphManifest.SaveVersion, SavedDirectory = directory, SavedStateHash = graphDoc.StateHash }, attachments);
            }
            var manifest = ProjectCodec.ReadManifest(token);
            var doc = ProjectCodec.Decode(directory, manifest);
            return RestoreAttachments(new AuthoringWorkspace(doc)
            {
                SaveVersion = manifest.SaveVersion, SavedDirectory = directory, SavedStateHash = doc.StateHash
            }, attachments);
        }
        static AuthoringWorkspace RestoreAttachments(AuthoringWorkspace workspace, ProjectAttachments attachments)
        {
            workspace.SetAttachments(attachments);
            workspace.SavedAttachmentsHash = attachments.ContentHash;
            return workspace;
        }
    }
}
