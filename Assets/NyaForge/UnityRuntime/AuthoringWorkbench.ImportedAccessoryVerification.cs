using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using Newtonsoft.Json.Linq;

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
                string rootBindingHash = bound.Binding.ContentHash;
                Check(boundGraph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.SkinDeform) &&
                    bound.Binding.Weights.Count == workspace.Preview.Evaluation.MeshOutputs.Values.First(value => value.Mesh != null).Mesh.VertexCount &&
                    bound.Binding.Weights.Values.SelectMany(values => values).Select(value => value.BoneId).Distinct().Count() == 1 &&
                    bound.Binding.Weights.Values.All(values => values.Count == 1 && Math.Abs(values[0].Weight - 1f) < 1e-6f),
                    "Accessory skin-bind did not initialize all vertices to the selected avatar root");

                // The clothing panel exposes a deterministic bone-segment
                // proximity seed after the explicit Root initialization. Keep
                // this as a separate action so the artist can inspect and
                // correct the result in Rig before committing a pose/export.
                attachmentTargetChoice = avatarObjectId;
                RefreshAttachmentControls();
                TransferAccessoryWeights();
                boundGraph = workspace.Document.ActiveObject.Graph;
                bound = boundGraph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.SkinBind);
                Check(bound.Binding.ContentHash != rootBindingHash && bound.Binding.Weights.Count > 0,
                    "Accessory automatic weight initialization did not update the SkinBind node");
                Check(bound.Binding.Weights.Values.All(values => values.Count >= 1 && values.Count <= 4 &&
                    Math.Abs(values.Sum(value => value.Weight) - 1f) < 1e-5f),
                    "Accessory automatic weights were not normalized within the four-influence limit");
                string boneBindingHash = bound.Binding.ContentHash;
                var preservedUnselectedWeights = bound.Binding.Weights[2]
                    .Select(value => value.BoneId + ":" + value.Weight.ToString("R", System.Globalization.CultureInfo.InvariantCulture)).ToArray();
                attachmentTargetChoice = avatarObjectId;
                RefreshAttachmentControls();
                accessorySurfaceTriangleIds.SetValueWithoutNotify("0");
                accessoryClothingVertexIds.SetValueWithoutNotify("0,1");
                TransferAccessorySurfaceWeights();
                boundGraph = workspace.Document.ActiveObject.Graph;
                bound = boundGraph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.SkinBind);
                Check(bound.Binding.ContentHash != boneBindingHash && bound.Binding.Weights.Count > 0,
                    "Accessory avatar-surface weight initialization did not update the SkinBind node");
                Check(bound.Binding.Weights.Values.All(values => values.Count >= 1 && values.Count <= 4 &&
                    Math.Abs(values.Sum(value => value.Weight) - 1f) < 1e-5f),
                    "Accessory avatar-surface weights were not normalized within the four-influence limit");
                Check(status.text.Contains("1面領域"),
                    "Accessory surface weight initialization did not report the selected avatar triangle region");
                Check(status.text.Contains("2頂点") && bound.Binding.Weights[2]
                    .Select(value => value.BoneId + ":" + value.Weight.ToString("R", System.Globalization.CultureInfo.InvariantCulture))
                    .SequenceEqual(preservedUnselectedWeights),
                    "Accessory surface weight initialization changed an unselected clothing vertex");
                string beforeFit = workspace.Evaluate().ContentHash;
                var beforeUnselectedPosition = workspace.Preview.Evaluation.MeshOutputs[accessoryEdit.NodeId].Mesh.Positions[2];
                accessoryFitOffsetMm.SetValueWithoutNotify(2);
                accessoryFitMaxDistanceMm.SetValueWithoutNotify(50);
                attachmentTargetChoice = avatarObjectId;
                RefreshAttachmentControls();
                FitAccessoryToAvatarSurface();
                Check(workspace.Evaluate().ContentHash != beforeFit,
                    "Accessory avatar-surface fit did not update the edited clothing geometry");
                Check(status.text.Contains("移動") && status.text.Contains("最大投影距離") && status.text.Contains("最大移動量"),
                    "Accessory surface fit status did not expose measured quality metrics");
                Check(status.text.Contains("1面領域"),
                    "Accessory surface fit did not report the selected avatar triangle region");
                Check(status.text.Contains("2頂点") && workspace.Preview.Evaluation.MeshOutputs[accessoryEdit.NodeId].Mesh.Positions[2].Equals(beforeUnselectedPosition),
                    "Accessory surface fit changed an unselected clothing vertex");
                string failedFitState = workspace.Document.StateHash;
                long failedFitRevision = workspace.Document.DocumentRevision;
                accessoryFitMaxDistanceMm.SetValueWithoutNotify(1);
                FitAccessoryToAvatarSurface();
                Check(workspace.Document.StateHash == failedFitState && workspace.Document.DocumentRevision == failedFitRevision,
                    "Out-of-range avatar-surface fit changed the clothing document");
                accessoryFitMaxDistanceMm.SetValueWithoutNotify(50);
                accessorySurfaceTriangleIds.SetValueWithoutNotify("");
                accessoryClothingVertexIds.SetValueWithoutNotify("");

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
                    workspace.Document.DocumentRevision, glbDirectory, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices(), SkinnedJointLocalTransforms());
                Check(File.Exists(glb.Path), "Skin-bound accessory standard GLB export was not published");
                Check(File.Exists(glb.ReportPath), "Skin-bound accessory GLB export report was not published");
                var glbReport = JObject.Parse(File.ReadAllText(glb.ReportPath));
                Check((string)glbReport["documentId"] == workspace.Document.DocumentId &&
                    (long)glbReport["documentRevision"] == workspace.Document.DocumentRevision &&
                    (string)glbReport["stateHash"] == workspace.Document.StateHash &&
                    (string)glbReport["glbHash"] == Checks.Hash(File.ReadAllBytes(glb.Path)) &&
                    (string)glbReport["profile"] == GlbExportProfile.SkinnedGeometry.ToString() &&
                    (int)glbReport["objectCount"] == workspace.Document.Objects.Count,
                    "Skin-bound accessory GLB report did not pin the exported snapshot");
                var exportedInventory = GlbSceneInventoryReader.Read(File.ReadAllBytes(glb.Path));
                Check(exportedInventory.Instances.Count == 2 && exportedInventory.Instances.All(item => item.SkinIndex.HasValue),
                    "Skin-bound accessory GLB did not retain both avatar and clothing skin instances");
                OpenProject();
                Execute(AuthoringOperation.SelectObject(accessoryObjectId));
                Check(workspace.Document.StateHash == skinHash && !workspace.IsDirty &&
                    workspace.Document.ActiveObject.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.SkinBind),
                    "Skin-bound accessory changed after native Save/Open");
                // The same workflow can now publish the avatar and its
                // skin-bound clothing as one VRM package. The humanoid and
                // expression metadata belong to the selected avatar graph;
                // the clothing graph contributes its own skinned mesh while
                // reusing the copied stable skeleton.
                Execute(AuthoringOperation.SelectObject(avatarObjectId));
                vrmName.SetValueWithoutNotify("Avatar with clothing");
                vrmAuthors.SetValueWithoutNotify("NyaForge");
                vrmLicenseUrl.SetValueWithoutNotify("https://example.com/nyaforge-clothing-verification");
                ExportVrm1();
                var vrmExports = Directory.GetDirectories(Path.Combine(skinProject, "exports"), "vrm1-*");
                Check(vrmExports.Length > 0, "Skin-bound clothing VRM export directory was not published");
                var vrmModel = Path.Combine(vrmExports.OrderByDescending(path => Directory.GetLastWriteTimeUtc(path)).First(), VrmExportService.FileName);
                var vrmReportPath = Path.Combine(Path.GetDirectoryName(vrmModel), VrmExportService.ReportFileName);
                Check(File.Exists(vrmModel) && File.Exists(vrmReportPath), "Skin-bound clothing VRM package or report was not published");
                var vrmReport = JObject.Parse(File.ReadAllText(vrmReportPath));
                Check((int)vrmReport["objectCount"] == 2 && VrmMetadataReader.Read(File.ReadAllBytes(vrmModel)).HumanoidNodes.Count >= 15,
                    "Skin-bound clothing VRM output did not retain both objects and humanoid metadata");
                var vrmInventory = GlbSceneInventoryReader.Read(File.ReadAllBytes(vrmModel));
                Check(vrmInventory.Instances.Count == 2 && vrmInventory.Instances.All(instance => instance.SkinIndex.HasValue),
                    "Skin-bound clothing VRM output did not retain both skinned mesh instances");
                checks.Add("separate VRM avatar + static GLB accessory: EditMesh, rigid BoneId attachment, Save/Open, Root-initialized, bone-proximity and avatar-surface weight initialization, explicit avatar pose copy + Save/Open, multi-object GLB/VRM export");
            }
            finally { ReplaceWorkspace(previous, previousPath); }
        }
    }
}
