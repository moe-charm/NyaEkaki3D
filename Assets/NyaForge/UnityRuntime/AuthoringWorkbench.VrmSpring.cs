using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Simulation;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        VrmSpringSession importedVrmSpringSession;
        Label vrmSpringStatus;
        SecondaryMotionDocument importedSecondaryMotionDocument;
        SecondaryMotionAsset importedSecondaryMotionAsset;
        Label secondaryMotionStatus;

        void BuildVrmSpringStatus(VisualElement parent)
        {
            vrmSpringStatus = new Label { name = "vrm-spring-status" };
            vrmSpringStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(vrmSpringStatus);
            secondaryMotionStatus = new Label { name = "secondary-motion-status" };
            secondaryMotionStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(secondaryMotionStatus);
            BuildSpringPlayback(parent);
        }

        void RefreshVrmSpringStatus()
        {
            if (vrmSpringStatus == null) return;
            RefreshSpringPlayback();
            if (importedVrmSpringSession == null) { vrmSpringStatus.text = "SpringBone設定: なし"; return; }
            int joints = 0; foreach (var group in importedVrmSpringSession.SpringBones) joints += group.Joints.Count;
            vrmSpringStatus.text = "SpringBone設定: " + importedVrmSpringSession.SpringBones.Count + " chain · " + joints + " joint · " + importedVrmSpringSession.ColliderGroups.Count + " collider group";
            if (!importedVrmSpringSession.HasCompleteDetails) vrmSpringStatus.text += " 詳細不足：再取込または元設定の確認が必要です。";
        }

        void RefreshSecondaryMotionStatus()
        {
            if (secondaryMotionStatus == null) return;
            if (importedSecondaryMotionDocument == null) { secondaryMotionStatus.text = "共通揺れ設定: なし"; return; }
            if (!importedSecondaryMotionDocument.IsSupported)
            {
                secondaryMotionStatus.text = "共通揺れ設定: 未対応wire version " + importedSecondaryMotionDocument.WireVersion + "（保持のみ・再生不可）";
                return;
            }
            try
            {
                importedSecondaryMotionAsset.ValidateFor(FindCurrentSkeleton(), workspace?.Evaluate());
                secondaryMotionStatus.text = "共通揺れ設定: " + importedSecondaryMotionAsset.Profile.SimulatorId + " · " + importedSecondaryMotionAsset.Chains.Count + " chain · 保存済み";
            }
            catch (AuthoringException error)
            {
                secondaryMotionStatus.text = "共通揺れ設定: 再bindが必要です（" + error.Code + "）。元の設定は保持されています。";
            }
        }

        void ClearImportedVrmSpring()
        {
            importedVrmSpringSession = null;
            RefreshVrmSpringStatus();
        }

        void ClearImportedSecondaryMotion()
        {
            importedSecondaryMotionDocument = null;
            importedSecondaryMotionAsset = null;
            RefreshSecondaryMotionStatus();
        }
    }
}
