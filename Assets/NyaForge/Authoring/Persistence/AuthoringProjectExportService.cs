using System;
using System.IO;

namespace NyaForge.Authoring
{
    /// <summary>Exports a feature-preserving native project package without mutating the source workspace.</summary>
    public static class AuthoringProjectExportService
    {
        public static string Export(AuthoringWorkspace workspace, string instance, string document, long revision, string directory)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            directory = Storage.DirectoryPath(directory);
            lock (workspace.Gate)
            {
                Checks.Require(!workspace.Executing, "REENTRANT_EXPORT", "Cannot export during a projection transaction.");
                Checks.Require(workspace.InstanceId == instance, "STALE_INSTANCE", "Export targets another instance.");
                Checks.Require(workspace.Document.DocumentId == document, "DOCUMENT_CHANGED", "Export targets another document.");
                Checks.Require(workspace.Document.DocumentRevision == revision, "REVISION_CONFLICT", "Document changed before export.");
                Checks.Require(!workspace.Document.IsEmpty, "NO_EXPORTABLE_OBJECT", "Add a mesh before exporting.");
                Checks.Require(!workspace.Document.Objects[0].IsStaticProfile, "GRAPH_PROFILE_REQUIRED", "Native feature export requires a graph project.");
                Checks.Require(workspace.Preview.IsComplete && !workspace.Preview.IsStale, "GRAPH_INCOMPLETE", "Export requires complete current evaluation.");
                ProjectExportService.ValidateAttachmentReferences(workspace.Document);
                Checks.Require(!Directory.Exists(directory) && !File.Exists(directory), "EXPORT_DESTINATION_EXISTS", "Export destination already exists.");

                var detached = new AuthoringWorkspace(workspace.Document);
                detached.SetAttachments(workspace.Attachments);
                ProjectStore.Save(directory, detached, 0);
                return Path.Combine(directory, ProjectStore.ManifestName);
            }
        }
    }
}
