using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyImportedAccessoryWorkflow(string output, List<string> checks)
        {
            var previous = workspace; string previousPath = savedDirectory;
            try
            {
                string avatarPath = Path.Combine(output, "accessory-avatar.vrm");
                string accessoryPath = Path.Combine(output, "accessory-static.glb");
                File.WriteAllBytes(avatarPath, VrmVerificationFixture.Create(false));
                File.WriteAllBytes(accessoryPath, VrmVerificationFixture.CreateStaticAccessory());

                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                modelImportInstanceIndex.SetValueWithoutNotify(0); // VRM fixture's skinned mesh node
                InspectModelSelection(avatarPath); ImportModel(avatarPath);
                string avatarObjectId = workspace.Document.ActiveObjectId;
                Check(importedRigSession != null && workspace.Document.ActiveObject.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.EditMesh),
                    "Imported avatar did not expose an editable graph");

                modelImportInstanceIndex.SetValueWithoutNotify(0);
                InspectModelSelection(accessoryPath); ImportModel(accessoryPath);
                string accessoryObjectId = workspace.Document.ActiveObjectId;
                var accessoryGraph = workspace.Document.ActiveObject.Graph;
                var accessoryEdit = accessoryGraph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.EditMesh);
                Check(accessoryEdit != null, "Imported static accessory did not create an EditMesh stage");

                SelectEditStage(editStageIds.IndexOf(accessoryEdit.NodeId)); Select(new[] { 0 });
                moveX.SetValueWithoutNotify(1); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0);
                string beforeEdit = workspace.Evaluate().ContentHash; MoveSelection();
                Check(workspace.Evaluate().ContentHash != beforeEdit, "Imported static accessory vertex edit did not change output");

                Check(attachmentTargetIds.Count == 1 && attachmentBoneIds.Count > 0, "Imported avatar was not exposed as an attachment target");
                attachmentTarget.value = attachmentTarget.choices[0]; attachmentBone.index = 0;
                attachmentOffsetX.SetValueWithoutNotify(0); attachmentOffsetY.SetValueWithoutNotify(0); attachmentOffsetZ.SetValueWithoutNotify(0);
                ApplyAttachment();
                var attachment = workspace.Document.ActiveObject.Graph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.Attachment);
                Check(attachment.AttachmentTargetObjectId == avatarObjectId && attachment.AttachmentBoneId == attachmentBoneIds[0],
                    "Imported accessory attachment did not retain target and stable BoneId");

                string project = Path.Combine(output, "imported-accessory-project"); projectPath.SetValueWithoutNotify(project); SaveProject();
                string savedHash = workspace.Document.StateHash;
                string nativeManifest = ProjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId,
                    workspace.Document.DocumentRevision, Path.Combine(project, "exports", "native")).ManifestPath;
                Check(File.Exists(nativeManifest), "Imported accessory native export was not published");
                OpenProject();
                Check(workspace.Document.Objects.Count == 2 && workspace.Document.StateHash == savedHash && !workspace.IsDirty,
                    "Imported avatar and accessory changed after native Save/Open");
                Execute(AuthoringOperation.SelectObject(accessoryObjectId));
                var reopenedAttachment = workspace.Document.ActiveObject.Graph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.Attachment);
                Check(reopenedAttachment.AttachmentTargetObjectId == avatarObjectId && reopenedAttachment.AttachmentBoneId == attachment.AttachmentBoneId,
                    "Imported accessory attachment identity was lost after Open");
                checks.Add("separate VRM avatar + static GLB accessory: import, EditMesh vertex edit, stable BoneId attachment, native Save/Open and feature-preserving export");
            }
            finally { ReplaceWorkspace(previous, previousPath); }
        }
    }
}
