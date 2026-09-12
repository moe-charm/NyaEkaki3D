using System.Collections.Generic;
using System.IO;
using System.Text;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifySaveFailureGuard(string output, List<string> checks)
        {
            foreach (string sidecar in new[] { ProjectAttachments.Expressions, ProjectAttachments.Springs })
            foreach (bool existing in new[] { false, true })
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateFixture(), null);
                string directory = Path.Combine(output, "save-failure-" + existing + "-" + Path.GetFileNameWithoutExtension(sidecar));
                Directory.CreateDirectory(directory);
                projectPath.SetValueWithoutNotify(directory);
                string previousManifest = null;
                var oldMetadata = VerificationMetadata("old");
                if (existing)
                {
                    workspace.SetAttachments(oldMetadata);
                    Check(TrySaveProject(), "Initial snapshot save failed");
                    previousManifest = File.ReadAllText(Path.Combine(directory, ProjectStore.ManifestName));
                }
                var metadata = VerificationMetadata("new");
                workspace.SetAttachments(metadata);
                string blobs = Path.Combine(directory, "blobs"); Directory.CreateDirectory(blobs);
                using (var locked = new FileStream(Path.Combine(blobs, metadata.Hashes[sidecar] + ".bin"), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    var bytes = metadata.Read(sidecar); locked.Write(bytes, 0, bytes.Length); locked.Flush(true);
                    Check(!TrySaveForExit(), "Failed sidecar save allowed exit");
                    Check(workspace.IsDirty && workspace.SaveVersion == (existing ? 1 : 0), "Failed snapshot advanced saved state");
                    if (existing)
                    {
                        Check(previousManifest == File.ReadAllText(Path.Combine(directory, ProjectStore.ManifestName)), "Failed snapshot replaced manifest");
                        Check(ProjectStore.Open(directory).Attachments.ContentHash == oldMetadata.ContentHash, "Old metadata was lost");
                    }
                    else Check(!File.Exists(Path.Combine(directory, ProjectStore.ManifestName)), "Failed Save As published a project");
                    Check(HasUnsaved && saveIncomplete, "Failed save lost its pending state");
                    Check(!WantsToQuit(), "Failed save bypassed quit confirmation");
                    bool replaced = false;
                    ConfirmReplace(() => replaced = true);
                    Check(!replaced, "Failed save allowed unconfirmed replacement");
                    CancelReplace();
                }
                if (existing)
                {
                    var saved = SaveMcpProject(new ProjectSaveRequest(workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, workspace.SaveVersion));
                    Check((bool)saved["success"], "MCP handler retry failed after blob unlock");
                }
                else Check(TrySaveProject(), "Save As retry failed after blob unlock");
                Check(workspace.SaveVersion == (existing ? 2 : 1) && !HasUnsaved, "Retry did not finish the composite save");
                OpenProject();
                Check(!HasUnsaved && workspace.Attachments.ContentHash == metadata.ContentHash, "Retried project did not reopen");
                Check(importedVrmSession.Title == "new" && importedVrmSpringSession.Title == "new", "Workbench did not decode snapshot metadata");
            }
            checks.Add("Atomic metadata snapshots: expression/Spring blob IO failures preserve old manifest/settings and dirty/version, deny exit/replacement, and permit GUI/MCP-handler retry/reopen");
        }

        static ProjectAttachments VerificationMetadata(string title)
        {
            string prefix = "{\"version\":1,\"sourceHash\":\"" + new string('0', 64) + "\",\"format\":\"vrm0\",\"title\":\"" + title + "\",\"author\":\"fixture\",";
            return new ProjectAttachments(new Dictionary<string, byte[]>
            {
                [ProjectAttachments.Expressions] = Encoding.UTF8.GetBytes(prefix + "\"expressions\":[]}"),
                [ProjectAttachments.Springs] = Encoding.UTF8.GetBytes(prefix + "\"springBones\":[],\"colliderGroups\":[]}")
            });
        }
    }
}
