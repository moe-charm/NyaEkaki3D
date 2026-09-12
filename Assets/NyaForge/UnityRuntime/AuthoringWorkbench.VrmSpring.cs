using System;
using NyaForge.Authoring.Import;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        VrmSpringSession importedVrmSpringSession;
        Label vrmSpringStatus;

        void BuildVrmSpringStatus(VisualElement parent)
        {
            vrmSpringStatus = new Label { name = "vrm-spring-status" };
            vrmSpringStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(vrmSpringStatus);
        }

        void RefreshVrmSpringStatus()
        {
            if (vrmSpringStatus == null) return;
            if (importedVrmSpringSession == null) { vrmSpringStatus.text = "SpringBone設定: なし"; return; }
            int joints = 0; foreach (var group in importedVrmSpringSession.SpringBones) joints += group.Joints.Count;
            vrmSpringStatus.text = "SpringBone設定: " + importedVrmSpringSession.SpringBones.Count + " chain · " + joints + " joint · " + importedVrmSpringSession.ColliderGroups.Count + " collider group（設定保存済み・物理未実装）";
        }

        void SetImportedVrmSpring(VrmMetadata metadata)
        {
            importedVrmSpringSession = metadata == null ? null : VrmSpringSession.Create(metadata);
            RefreshVrmSpringStatus();
        }

        void ClearImportedVrmSpring()
        {
            importedVrmSpringSession = null;
            RefreshVrmSpringStatus();
        }
    }
}
