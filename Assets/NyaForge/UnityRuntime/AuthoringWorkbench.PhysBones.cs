using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        PhysBonesTargetDocument importedPhysBonesDocument;
        PhysBonesTargetProfile importedPhysBonesTarget;
        Label physBonesStatus;
        Button physBonesExportButton;

        void BuildPhysBonesStatus(VisualElement parent)
        {
            physBonesStatus = new Label { name = "physbones-status" };
            physBonesStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(physBonesStatus);
            physBonesExportButton = Button("PhysBones targetを書き出す", ExportPhysBonesTarget, "physbones-target-export");
            parent.Add(physBonesExportButton);
        }

        void SetImportedPhysBones(PhysBonesTargetDocument document)
        {
            importedPhysBonesDocument = document;
            importedPhysBonesTarget = document != null && document.IsSupported ? document.Profile : null;
            RefreshPhysBonesStatus();
        }

        void ClearImportedPhysBones()
        {
            importedPhysBonesDocument = null;
            importedPhysBonesTarget = null;
            RefreshPhysBonesStatus();
        }

        void RefreshPhysBonesStatus()
        {
            if (physBonesStatus == null) return;
            if (importedPhysBonesDocument == null)
            {
                physBonesStatus.text = "PhysBones設定: なし";
                physBonesExportButton?.SetEnabled(false);
                return;
            }
            if (!importedPhysBonesDocument.IsSupported)
            {
                physBonesStatus.text = "PhysBones設定: 未対応wire version " + importedPhysBonesDocument.WireVersion + "（保持のみ。更新版で開いてください）";
                physBonesExportButton?.SetEnabled(false);
                return;
            }
            var profile = importedPhysBonesDocument.Profile;
            physBonesStatus.text = "PhysBones設定: " + profile.Chains.Count + " chain · SDK " + profile.SdkVersion;
            var skeleton = FindCurrentSkeleton();
            if (skeleton == null)
            {
                physBonesStatus.text += " · 骨格未接続";
                physBonesExportButton?.SetEnabled(false);
                return;
            }
            bool valid = true;
            try { Checks.Require(profile.SkeletonHash == skeleton.ContentHash, "SIMULATION_SKELETON_CHANGED", "PhysBones profile skeleton identity changed; rebind the setup."); }
            catch (AuthoringException error) { valid = false; physBonesStatus.text += " · stale（" + error.Code + "）: 再対応が必要"; }
            physBonesExportButton?.SetEnabled(valid);
        }

        void ExportPhysBonesTarget()
        {
            Try(() =>
            {
                if (importedPhysBonesTarget == null) throw new InvalidOperationException("対応済みのPhysBones targetを開いてください。");
                var skeleton = FindCurrentSkeleton();
                if (skeleton == null) throw new InvalidOperationException("PhysBones targetに対応する骨格がありません。");
                string project = Path.GetFullPath(projectPath.value);
                string directory = Path.Combine(project, "exports", "physbones-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
                string manifest = PhysBonesTargetPackage.Export(directory, importedPhysBonesTarget, skeleton, importedSecondaryMotionAsset);
                SetStatus("PhysBones targetを書き出しました: " + manifest);
            });
        }

        SkeletonDefinition FindCurrentSkeleton()
        {
            if (workspace == null || workspace.Document.IsEmpty) return null;
            return workspace.Document.Objects.Select(item => item.Graph).Where(graph => graph != null)
                .SelectMany(graph => graph.Nodes.Values).Select(node => node.Skeleton).FirstOrDefault(skeleton => skeleton != null);
        }
    }
}
