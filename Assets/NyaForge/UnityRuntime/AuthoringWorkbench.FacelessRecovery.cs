using System.Linq;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        VisualElement facelessRecovery;
        Label facelessRecoveryInfo;
        Button facelessRecoveryButton;
        string facelessRecoveryNode;
        void BuildFacelessRecovery(VisualElement parent)
        {
            facelessRecovery=new VisualElement { name="faceless-recovery" };parent.Add(facelessRecovery);
            facelessRecoveryInfo=new Label();facelessRecoveryInfo.style.whiteSpace=WhiteSpace.Normal;facelessRecovery.Add(facelessRecoveryInfo);
            facelessRecoveryButton=Button("面がない編集段へ戻る",()=>Try(()=>SelectEditStage(editStageIds.IndexOf(facelessRecoveryNode))),"recover-faceless-edit");facelessRecovery.Add(facelessRecoveryButton);
        }
        void RefreshFacelessRecovery()
        {
            facelessRecoveryNode=null;
            if(IsGraph && !workspace.Preview.IsComplete)
            {
                var graph=workspace.Document.ActiveObject.Graph;
                facelessRecoveryNode=editStageIds.FirstOrDefault(id=>graph.Nodes.TryGetValue(id,out var node) && node.TypeId==BuiltinNodes.PolygonEdit &&
                    workspace.Preview.Evaluation.MeshOutputs.TryGetValue(id,out var value) && value.Polygon?.Faces.Count==0);
            }
            facelessRecovery.style.display=facelessRecoveryNode==null ? DisplayStyle.None : DisplayStyle.Flex;
            if(facelessRecoveryNode==null) return;
            facelessRecoveryInfo.text="面がないため、後続の処理を評価できません。編集段で点と面を作るか、Undoで削除を戻してください。保存したPaint・材質の設定は保持されています。";
            facelessRecoveryButton.SetEnabled(activeEditContext?.NodeId!=facelessRecoveryNode);
        }
    }
}
