using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyPhysBonesTargetStatus(string output, List<string> checks)
        {
            var skeleton = FindCurrentSkeleton();
            Check(skeleton != null && skeleton.Bones.Count > 0, "PhysBones verification needs an imported skeleton");
            string root = skeleton.Bones[0].BoneId;
            var chain = new PhysBonesChain("verification", root, new[] { root }, PhysBonesEndpointMode.Auto, "", null, PhysBonesMultiChildType.Ignore, null, null, null, PhysBonesParameters.Default, PhysBonesInteraction.Default, null);
            var profile = new PhysBonesTargetProfile("vrchat.physbones", "verification-sdk", "", skeleton.ContentHash, "", new[] { chain });
            var owned = new Dictionary<string, byte[]>(); foreach (var name in workspace.Attachments.Hashes.Keys) owned[name] = workspace.Attachments.Read(name); owned[ProjectAttachments.PhysBones] = PhysBonesTargetCodec.Write(profile); workspace.SetAttachments(new ProjectAttachments(owned));
            string project = Path.Combine(output, "physbones-status"); projectPath.SetValueWithoutNotify(project); Check(TrySaveProject(), "PhysBones target status project could not save"); OpenProject();
            Check(importedPhysBonesTarget != null && physBonesStatus.text.Contains("PhysBones設定: 1 chain") && physBonesStatus.text.Contains("verification-sdk"), "PhysBones target was not decoded into the Workbench status");
            ExportPhysBonesTarget();
            Check(Directory.GetFiles(project, PhysBonesTargetPackage.ManifestName, SearchOption.AllDirectories).Length == 1, "Workbench did not export a PhysBones target package");
            var unknown = PhysBonesTargetCodec.Write(profile); BitConverter.GetBytes(99).CopyTo(unknown, 4); var unknownOwned = new Dictionary<string, byte[]>(); foreach (var name in workspace.Attachments.Hashes.Keys) if (name != ProjectAttachments.PhysBones) unknownOwned[name] = workspace.Attachments.Read(name); unknownOwned[ProjectAttachments.PhysBones] = unknown; workspace.SetAttachments(new ProjectAttachments(unknownOwned)); string unknownProject = Path.Combine(output, "physbones-unknown"); projectPath.SetValueWithoutNotify(unknownProject); Check(TrySaveProject(), "Unknown PhysBones target project could not save"); OpenProject();
            Check(importedPhysBonesTarget == null && physBonesStatus.text.Contains("未対応wire version 99") && workspace.Attachments.Read(ProjectAttachments.PhysBones).SequenceEqual(unknown), "Unknown PhysBones target was not retained by the Workbench");
            checks.Add("PhysBones GUI status: supported target decodes after Save/Open, target package export is available, and unknown wire version remains retained with an explicit read-only message");
        }
    }
}
