using System.IO;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyVrmImportRoundtrip(string output, List<string> checks)
        {
            VerifyImportFailureIsolation(output, checks);
            string selectionDirectory = Path.Combine(output, "mesh-selection"); Directory.CreateDirectory(selectionDirectory);
            string selectionPath = Path.Combine(selectionDirectory, "multi-mesh.vrm"); File.WriteAllBytes(selectionPath, VrmVerificationFixture.CreateMultiMeshSelection());
            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            modelImportMeshIndex.SetValueWithoutNotify(1); modelImportSkinIndex.SetValueWithoutNotify(0);
            InspectModelSelection(selectionPath);
            Check(modelImportSelectionStatus.text.Contains("mesh 1") && modelImportSelectionStatus.text.Contains("skins 1"), "Mesh selection inventory was not shown in the import GUI");
            ImportModel(selectionPath);
            Check(importedRigSession != null && importedRigSession.SourceSkin != null && importedRigSession.SourceSkinBinding != null, "Selected multi-mesh skin import did not retain source payload");
            Check(workspace.Document.Objects[0].Graph.Nodes.Values.Any(node => node.SourceMesh != null && node.SourceMesh.TopologyHash == importedRigSession.SourceSkinBinding.MeshTopologyHash), "Selected mesh graph was not created");
            modelImportMeshIndex.SetValueWithoutNotify(0); modelImportSkinIndex.SetValueWithoutNotify(0);
            checks.Add("GLB import GUI: candidate inventory inspection and explicit mesh/skin selection for a multi-mesh fixture");
            foreach (bool legacy in new[] { false, true })
            {
                string directory = Path.Combine(output, legacy ? "vrm0-import" : "vrm1-import");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "fixture.vrm");
                File.WriteAllBytes(path, VrmVerificationFixture.Create(legacy));
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                ImportModel(path);
                Check(!workspace.Document.IsEmpty && importedVrmSession != null && importedVrmSpringSession != null, "VRM import did not create graph and sessions");
                string graphHash = workspace.Document.EditSourceHash;
                string metadataHash = workspace.Attachments.ContentHash;
                string sourceHash = importedVrmSession.SourceHash;
                projectPath.SetValueWithoutNotify(Path.Combine(directory, "project"));
                Check(TrySaveProject(), "Imported VRM could not save");
                string project = Path.Combine(directory, "project");
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                projectPath.SetValueWithoutNotify(project);
                OpenProject();
                Check(!HasUnsaved && workspace.Document.EditSourceHash == graphHash, "Imported VRM graph changed on Open");
                Check(workspace.Attachments.ContentHash == metadataHash, "Imported VRM settings changed on Open");
                Check(importedVrmSession.SourceHash == sourceHash && importedVrmSpringSession.SourceHash == sourceHash, "Imported VRM source identity was lost");
                Check(importedRigSession != null && importedRigSession.SourceHash == sourceHash, "Imported rig mapping was lost");
                var graph = workspace.Document.Objects[0].Graph;
                var humanoid = importedRigSession.ResolveHumanoid(graph);
                Check(humanoid["hips"] == importedRigSession.NodeToBone[0], "Humanoid mapping changed on Open");
                var skeletonNode = graph.Nodes[importedRigSession.SkeletonNodeId];
                Check(importedRigSession.Hierarchy != null && importedRigSession.Hierarchy.Parents.Count == 3 && importedRigSession.Hierarchy.Parents[1] == 0 && importedRigSession.Hierarchy.Children[0][0] == 1, "Source hierarchy was lost on Open");
                Check(importedRigSession.SourceNodeOrigins != null && importedRigSession.SourceNodeOrigins[1].Y == .3f, "Source node origin was lost on Open");
                var restPose = PoseSet.Create(skeletonNode.Skeleton, skeletonNode.Skeleton.Bones.Select(b => new BonePose(b.BoneId, PoseTransform.FromTranslation(b.Head))));
                var nodeSpace = new NyaForge.Authoring.Import.ImportedNodeSpace(importedRigSession, graph, restPose);
                SpringCheckNear(new Vec3(0, .5f, 0), nodeSpace.TransformPoint(1, new Vec3(0, .2f, 0)));
                var moved = SkeletonEditing.MoveBone(skeletonNode.Skeleton, humanoid["hips"], new Vec3(.01f, 0, 0), new Vec3(.01f, 0, 0));
                Execute(AuthoringOperation.ReplaceGraph(graph.ReplaceNode(GraphNode.SkeletonNode(skeletonNode.NodeId, moved))));
                Check(importedRigStatus.text.Contains("IMPORT_SKELETON_CHANGED"), "Edited skeleton did not report stale mapping");
                Execute(AuthoringOperation.Undo());
                importedRigSession.Resolve(workspace.Document.Objects[0].Graph);
                Check(!importedRigStatus.text.Contains("IMPORT_SKELETON_CHANGED"), "Undo did not restore mapping status");
                Check(importedVrmSession.Expressions.Count == 1 && importedVrmSession.Expressions[0].Weights.Values.Single() == .5f, "Imported VRM morph expression was lost");
                var nodes = importedVrmSpringSession.ColliderGroups[0].ColliderNodeIndices;
                Check(importedVrmSpringSession.HasCompleteDetails, "Imported VRM lost detailed Spring geometry");
                var shapes = importedVrmSpringSession.ColliderGroups[0].Shapes;
                Check(shapes[1].Offset.Value.Y == .1f && shapes[1].Radius == .2f, "Imported VRM sphere geometry changed");
                var direction = importedVrmSpringSession.SpringBones[0].Joints[0].GravityDirection.Value;
                Check(direction.X == (legacy ? 1 : 2) && direction.Z == (legacy ? -1 : 4), "Imported VRM gravity direction changed");
                Check(nodes.SequenceEqual(legacy ? new[] { 0, 0 } : new[] { 0, 0, 1 }), "Imported VRM collider node order changed");
                Check(importedVrmSession.Authors.SequenceEqual(legacy ? new[] { "Nya, Charm" } : new[] { "Nya", "Moe, Charm" }), "Imported VRM authors changed");
                if (!legacy)
                {
                    var joints = importedVrmSpringSession.SpringBones[0].Joints;
                    Check(joints[0].Stiffness == 1 && joints[0].DragForce == .5f && joints[1].Stiffness == 0 && joints[1].DragForce == 0, "VRM defaults and explicit zero were mixed");
                    Check(shapes[2].Kind == "capsule" && shapes[2].Tail.Value.Y == .1f, "Imported VRM capsule tail changed");
                }
            }
            VerifySpringPlayback(output, checks);
            checks.Add("VRM0/1 file import to Workbench graph, composite Save, empty workspace and Open: skin/morph graph, source identity, authors, expression weights and repeated collider nodes");
        }
    }
}
