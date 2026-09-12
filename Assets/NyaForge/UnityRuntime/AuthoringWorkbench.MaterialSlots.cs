using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button enableMaterialSlots,independentMaterial;
        DropdownField materialSlotChoice;
        readonly List<int> visibleMaterialSlots=new List<int>();
        int selectedMaterialSlot=-1;
        void BuildMaterialSlots()
        {
            enableMaterialSlots=Button("部位別の材質編集を有効にする",()=>Try(()=>
                Execute(AuthoringOperation.ReplaceGraph(MaterialSlotEditing.ConvertOutput(workspace.Document.ActiveObject.Graph)))),"material-slots-enable");
            materialPanel.Add(enableMaterialSlots);
            materialSlotChoice=new DropdownField("材質を分ける部位",new List<string>{"なし"},0) { name="material-slot-choice" };
            materialSlotChoice.style.flexDirection=FlexDirection.Column;materialPanel.Add(materialSlotChoice);
            materialSlotChoice.RegisterValueChangedCallback(_=>
            {
                CancelMaterialGesture();
                if(materialSlotChoice.index<0 || materialSlotChoice.index>=visibleMaterialSlots.Count) return;
                selectedMaterialSlot=visibleMaterialSlots[materialSlotChoice.index];
                selectedMaterial=OutputSurfaceConnections.Resolve(workspace.Document.ActiveObject.Graph,selectedMaterialSlot)?.MaterialNodeId ?? "";
                RefreshMaterials();RefreshPaint();
            });
            independentMaterial=Button("この部位の材質を複製して分ける",()=>Try(()=>
            {
                string id=Guid.NewGuid().ToString("D");
                Execute(AuthoringOperation.ReplaceGraph(MaterialSlotEditing.MakeIndependent(workspace.Document.ActiveObject.Graph,selectedMaterialSlot,id)));
                if(workspace.Document.ActiveObject.Graph.Nodes.ContainsKey(id)) selectedMaterial=id;
                SelectEditStage(0);RefreshMaterials();
            }),"material-slot-independent");materialPanel.Add(independentMaterial);
            BuildMaterialFaces();
        }
        void RefreshMaterialSlots()
        {
            if(enableMaterialSlots==null) return;
            var output=workspace.Preview.Output;
            enableMaterialSlots.SetEnabled(workspace.Preview.IsComplete && output?.Material!=null);
            var previous=selectedMaterialSlot;visibleMaterialSlots.Clear();
            if(output?.SlotMaterials!=null) visibleMaterialSlots.AddRange(output.PolygonRendering?.MaterialSlotMap ?? Enumerable.Range(0,output.Mesh.Submeshes.Count).ToArray());
            selectedMaterialSlot=visibleMaterialSlots.Contains(previous) ? previous : (visibleMaterialSlots.Count==0 ? -1 : visibleMaterialSlots[0]);
            materialSlotChoice.choices=visibleMaterialSlots.Count==0 ? new List<string>{"なし"} : visibleMaterialSlots.Select(s=>"部位 "+s+" · 材質 "+output.SlotMaterials[s].MaterialNodeId.Substring(0,8)).ToList();
            materialSlotChoice.SetValueWithoutNotify(selectedMaterialSlot<0 ? "なし" : materialSlotChoice.choices[visibleMaterialSlots.IndexOf(selectedMaterialSlot)]);
            materialSlotChoice.SetEnabled(selectedMaterialSlot>=0);independentMaterial.SetEnabled(selectedMaterialSlot>=0);
            RefreshMaterialFaces();
        }
    }
}
