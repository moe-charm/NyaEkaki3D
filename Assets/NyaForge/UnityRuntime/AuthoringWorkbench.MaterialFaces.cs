using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button splitMaterialFaces,moveMaterialFaces;
        void BuildMaterialFaces()
        {
            splitMaterialFaces=Button("選択面を新しい部位に分ける",()=>Try(()=>AssignMaterialFaces(true)),"material-faces-split");
            moveMaterialFaces=Button("選択面を上の部位へ移す",()=>Try(()=>AssignMaterialFaces(false)),"material-faces-move");
            materialPanel.Add(splitMaterialFaces);materialPanel.Add(moveMaterialFaces);
            var help=new Label("PolygonEditで面を選択してください。新しい部位は編集中の材質を複製します。既存部位へ移す場合、その部位の材質は変えません。") ;
            help.style.whiteSpace=WhiteSpace.Normal;materialPanel.Add(help);
        }
        void RefreshMaterialFaces()
        {
            if(splitMaterialFaces==null) return;
            bool ready=activeEditContext!=null && selectedFaces.Count>0 && workspace.Preview.IsComplete && workspace.Preview.Output?.SlotMaterials!=null;
            splitMaterialFaces.SetEnabled(ready && selectedMaterial!="");moveMaterialFaces.SetEnabled(ready && selectedMaterialSlot>=0);
        }
        void AssignMaterialFaces(bool create)
        {
            if(activeEditContext==null || selectedFaces.Count==0) throw new InvalidOperationException("PolygonEditで面を選択してください。");
            CancelMaterialGesture();var graph=workspace.Document.ActiveObject.Graph;var bindings=workspace.Preview.Output.SlotMaterials;
            int slot=selectedMaterialSlot;
            if(create)
            {
                slot=0;while(slot<AuthoringLimits.MaxSubmeshes && bindings.ContainsKey(slot)) slot++;
                if(slot==AuthoringLimits.MaxSubmeshes) throw new InvalidOperationException("材質部位の上限に達しました。");
            }
            string material=create ? selectedMaterial : bindings[slot].MaterialNodeId;
            var changed=MaterialFaceEditing.Assign(graph,activeEditContext,selectedFaces.OrderBy(id=>id),slot,material);
            string newId=create ? Guid.NewGuid().ToString("D") : material;
            if(create) changed=MaterialSlotEditing.MakeIndependent(changed,slot,newId);
            Execute(AuthoringOperation.ReplaceGraph(changed));
            if(workspace.Preview.Output?.SlotMaterials!=null && workspace.Preview.Output.SlotMaterials.TryGetValue(slot,out var result) && result.MaterialNodeId==newId)
            { selectedMaterialSlot=slot;selectedMaterial=newId; }
            RefreshMaterials();
        }
    }
}
