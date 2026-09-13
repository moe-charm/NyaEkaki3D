using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

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

                // Convert the same edited accessory from rigid attachment to a
                // proper skin graph. The command intentionally starts every
                // vertex at the avatar root; the Rig panel can then paint the
                // remaining weights without rebuilding the clothing mesh.
                RemoveAttachment();
                attachmentTargetChoice = avatarObjectId;
                RefreshAttachmentControls();
                BindAccessoryToAvatar();
                var boundGraph = workspace.Document.ActiveObject.Graph;
                var bound = boundGraph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.SkinBind);
                Check(boundGraph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.SkinDeform) &&
                    bound.Binding.Weights.Count == workspace.Preview.Evaluation.MeshOutputs.Values.First(value => value.Mesh != null).Mesh.VertexCount &&
                    bound.Binding.Weights.Values.SelectMany(values => values).Select(value => value.BoneId).Distinct().Count() == 1 &&
                    bound.Binding.Weights.Values.All(values => values.Count == 1 && Math.Abs(values[0].Weight - 1f) < 1e-6f),
                    "Accessory skin-bind did not initialize all vertices to the selected avatar root");

                // Exercise the same explicit pose-copy action exposed by the
                // Workbench. Move the source avatar, copy its evaluated pose
                // into the clothing graph, and prove the saved pose survives
                // reopening before resetting both graphs for GLB export.
                Execute(AuthoringOperation.SelectObject(avatarObjectId));
                var avatarGraph = workspace.Document.ActiveObject.Graph;
                var avatarPoseNode = avatarGraph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.Pose);
                var avatarSkeleton = TryResolveSkeleton(RigFor(workspace.Document.ActiveObject), avatarGraph);
                var avatarRoot = avatarSkeleton.Bones.First(bone => string.IsNullOrEmpty(bone.ParentBoneId));
                var copiedAvatarPose = PoseEditing.SetRotationZ(avatarPoseNode.Pose, avatarSkeleton, avatarRoot.BoneId, 25);
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(avatarPoseNode.NodeId, copiedAvatarPose)));
                Execute(AuthoringOperation.SelectObject(accessoryObjectId));
                attachmentTargetChoice = avatarObjectId; RefreshAttachmentControls();
                CopyAvatarPose();
                var copiedClothingPose = workspace.Document.ActiveObject.Graph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.Pose).Pose;
                Check(copiedClothingPose.ContentHash == copiedAvatarPose.ContentHash,
                    "Explicit avatar pose copy did not rebind the clothing Pose node");
                string poseProject = Path.Combine(output, "imported-accessory-pose-project");
                projectPath.SetValueWithoutNotify(poseProject); SaveProject();
                OpenProject(); Execute(AuthoringOperation.SelectObject(accessoryObjectId));
                var reopenedCopiedPose = workspace.Document.ActiveObject.Graph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.Pose).Pose;
                Check(reopenedCopiedPose.ContentHash == copiedAvatarPose.ContentHash && !workspace.IsDirty,
                    "Explicit avatar pose copy changed after native Save/Open");

                // Standard skinned GLB output is a rest-pose profile. Restore
                // both source and clothing poses explicitly before continuing.
                var restAvatarPose = PoseSet.Create(avatarSkeleton, avatarSkeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
                Execute(AuthoringOperation.SelectObject(avatarObjectId));
                avatarPoseNode = workspace.Document.ActiveObject.Graph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.Pose);
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(avatarPoseNode.NodeId, restAvatarPose)));
                Execute(AuthoringOperation.SelectObject(accessoryObjectId));
                GraphNode clothingPoseNode; SkeletonDefinition clothingSkeleton;
                if (!TryResolvePose(out clothingPoseNode, out clothingSkeleton)) throw new InvalidOperationException("clothing pose node was not restored");
                var restClothingPose = PoseSet.Create(clothingSkeleton, clothingSkeleton.Bones.Select(bone => new BonePose(bone.BoneId, PoseTransform.FromTranslation(bone.Head))));
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(clothingPoseNode.NodeId, restClothingPose)));
                string skinProject = Path.Combine(output, "imported-accessory-skin-project");
                projectPath.SetValueWithoutNotify(skinProject); SaveProject();
                string skinHash = workspace.Document.StateHash;
                string skinExport = ProjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId,
                    workspace.Document.DocumentRevision, Path.Combine(skinProject, "exports", "native-skin")).ManifestPath;
                Check(File.Exists(skinExport), "Skin-bound accessory native export was not published");
                string glbDirectory = Path.Combine(skinProject, "exports", "skinned-glb");
                var glb = GlbExportService.ExportSkinnedWithTransforms(workspace, workspace.InstanceId, workspace.Document.DocumentId,
                    workspace.Document.DocumentRevision, glbDirectory, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices());
                Check(File.Exists(glb.Path), "Skin-bound accessory standard GLB export was not published");
                var exportedInventory = GlbSceneInventoryReader.Read(File.ReadAllBytes(glb.Path));
                Check(exportedInventory.Instances.Count == 2 && exportedInventory.Instances.All(item => item.SkinIndex.HasValue),
                    "Skin-bound accessory GLB did not retain both avatar and clothing skin instances");
                OpenProject();
                Execute(AuthoringOperation.SelectObject(accessoryObjectId));
                Check(workspace.Document.StateHash == skinHash && !workspace.IsDirty &&
                    workspace.Document.ActiveObject.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.SkinBind),
                    "Skin-bound accessory changed after native Save/Open");
                checks.Add("separate VRM avatar + static GLB accessory: EditMesh, rigid BoneId attachment, Save/Open, Root-initialized skin-bind, explicit avatar pose copy + Save/Open, weight-ready native and standard GLB export");
            }
            finally { ReplaceWorkspace(previous, previousPath); }
        }
    }
}
