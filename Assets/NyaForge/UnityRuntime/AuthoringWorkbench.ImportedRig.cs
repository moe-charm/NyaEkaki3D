using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        ImportedRigSession importedRigSession;
        Label importedRigStatus;

        void BuildImportedRigStatus(VisualElement parent)
        {
            importedRigStatus = new Label { name = "imported-rig-status" };
            importedRigStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(importedRigStatus);
        }

        void RefreshImportedRigStatus()
        {
            if (importedRigStatus == null) return;
            if (importedRigSession == null) { importedRigStatus.text = "取込骨対応: なし"; return; }
            var graph = workspace?.Document.Objects.Select(item => item.Graph).FirstOrDefault(value => value != null && value.GraphId == importedRigSession.GraphId);
            try
            {
                importedRigSession.ResolveHumanoid(graph);
                importedRigStatus.text = "取込骨対応: " + importedRigSession.NodeToBone.Count + " bone · humanoid " + importedRigSession.HumanoidNodes.Count;
            }
            catch (AuthoringException error)
            {
                importedRigStatus.text = "取込骨対応: 更新が必要です（" + error.Code + "）。元データと現在の骨格を確認してください。";
            }
        }
    }
}
