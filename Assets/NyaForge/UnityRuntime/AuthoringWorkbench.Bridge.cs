using System;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout bridgePanel;
        DropdownField bridgeTarget;
        IntegerField bridgeOffset;
        Button bridgeButton;
        BoundaryHighlightProjection bridgeHighlight;
        void BuildBridge(VisualElement parent)
        {
            bridgePanel=new Foldout { text="2境界をつなぐ",value=false,name="bridge-panel" };parent.Add(bridgePanel);
            bridgePanel.Add(new Label("つなぐ相手（水色）"));bridgeTarget=new DropdownField { name="bridge-target" };bridgePanel.Add(bridgeTarget);
            bridgePanel.Add(new Label("接続ずれ (-1で自動)"));bridgeOffset=new IntegerField { value=-1,name="bridge-offset" };bridgePanel.Add(bridgeOffset);
            bridgeButton=Button("2つの境界をつなぐ",()=>Try(()=>
            {
                int a=boundaryChoice.index,b=bridgeTarget.index;
                if(a<0 || b<0 || a>=boundaryLoops.Length || b>=boundaryLoops.Length) throw new InvalidOperationException("2つの境界を選んでください。");
                Execute(AuthoringOperation.BridgePolygonBoundaries(activeEditContext,boundaryLoops[a],boundaryLoops[b],bridgeOffset.value));
            }),"bridge-boundaries");bridgePanel.Add(bridgeButton);
            bridgeTarget.RegisterValueChangedCallback(_=>UpdateBridgeHighlight());bridgePanel.RegisterValueChangedCallback(_=>UpdateBridgeHighlight());
        }
        void RefreshBridgeChoices()
        {
            int index=bridgeTarget.index;
            bridgeTarget.choices=boundaryChoice.choices.ToList();
            bridgeTarget.index=Math.Max(0,Math.Min(index<0 ? 1 : index,bridgeTarget.choices.Count-1));
            bridgeTarget.SetEnabled(boundaryLoops.Length>=2);UpdateBridgeHighlight();
        }
        void UpdateBridgeHighlight()
        {
            if(bridgeTarget==null) return;
            int a=boundaryChoice.index,b=bridgeTarget.index;var value=DisplayedGraphValue();
            bool valid=activeEditContext!=null && value?.Polygon!=null && a>=0 && b>=0 && a<boundaryLoops.Length && b<boundaryLoops.Length && a!=b;
            bridgeButton.SetEnabled(valid && boundaryLoops[a].Length==boundaryLoops[b].Length);
            if(!valid || !boundaryPanel.value || !bridgePanel.value) { bridgeHighlight?.Clear();return; }
            if(bridgeHighlight==null) bridgeHighlight=new BoundaryHighlightProjection(stage.transform,new Color(.2f,.7f,1));
            bridgeHighlight.Show(value.Polygon,value.Transform,boundaryLoops[b]);
        }
    }
}
