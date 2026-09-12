using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Simulation;
using NyaForge.Authoring.Rig;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        PhysBonesTargetDocument importedPhysBonesDocument;
        PhysBonesTargetProfile importedPhysBonesTarget;
        Label physBonesStatus;

        void BuildPhysBonesStatus(VisualElement parent)
        {
            physBonesStatus = new Label { name = "physbones-status" };
            physBonesStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(physBonesStatus);
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
                return;
            }
            if (!importedPhysBonesDocument.IsSupported)
            {
                physBonesStatus.text = "PhysBones設定: 未対応wire version " + importedPhysBonesDocument.WireVersion + "（保持のみ。更新版で開いてください）";
                return;
            }
            var profile = importedPhysBonesDocument.Profile;
            physBonesStatus.text = "PhysBones設定: " + profile.Chains.Count + " chain · SDK " + profile.SdkVersion;
            var skeleton = FindCurrentSkeleton();
            if (skeleton == null)
            {
                physBonesStatus.text += " · 骨格未接続";
                return;
            }
            try { profile.ValidateFor(null, skeleton); }
            catch (AuthoringException error) { physBonesStatus.text += " · stale（" + error.Code + "）: 再対応が必要"; }
        }

        SkeletonDefinition FindCurrentSkeleton()
        {
            if (workspace == null || workspace.Document.IsEmpty) return null;
            return workspace.Document.Objects.Select(item => item.Graph).Where(graph => graph != null)
                .SelectMany(graph => graph.Nodes.Values).Select(node => node.Skeleton).FirstOrDefault(skeleton => skeleton != null);
        }
    }
}
