using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button addSlotPaint;
        void BuildSlotPaint()
        {
            addSlotPaint=Button("選択部位に新しい白紙Paint",()=>Try(()=>
            {
                string material=Guid.NewGuid().ToString("D"),paint=Guid.NewGuid().ToString("D");
                var graph=workspace.Document.ActiveObject.Graph;
                Execute(AuthoringOperation.ReplaceGraph(MaterialSlotPaintEditing.AddBlank(graph,selectedMaterialSlot,material,paint)));
                if(workspace.Document.ActiveObject.Graph.Nodes.ContainsKey(paint)) { selectedPaint=paint;selectedMaterial=material; }
                SelectEditStage(0);RefreshPaint();RefreshMaterials();Frame();
            }),"add-slot-paint");paintPanel.Add(addSlotPaint);
        }
        void RefreshSlotPaint()
        {
            if(addSlotPaint==null) return;
            bool ready=workspace.Preview.IsComplete && workspace.Preview.Output?.Polygon!=null && workspace.Preview.Output?.SlotMaterials!=null && selectedMaterialSlot>=0;
            addSlotPaint.SetEnabled(ready);
            addSlotPaint.text=ready ? "部位 "+selectedMaterialSlot+" に白紙Paint（旧画像は保持）" : "選択部位に新しい白紙Paint";
        }
    }
}
