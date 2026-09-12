using System.IO;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyVrmImportRoundtrip(string output, List<string> checks)
        {
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
            checks.Add("VRM0/1 file import to Workbench graph, composite Save, empty workspace and Open: skin/morph graph, source identity, authors, expression weights and repeated collider nodes");
        }
    }
}
