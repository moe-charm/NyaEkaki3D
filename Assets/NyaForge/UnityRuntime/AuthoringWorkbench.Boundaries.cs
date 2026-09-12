using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        DropdownField boundaryChoice;
        Foldout boundaryPanel;
        Button fillBoundaryButton;
        BoundaryHighlightProjection boundaryHighlight;
        ulong[][] boundaryLoops=Array.Empty<ulong[]>();
        void BuildBoundaryEditing(VisualElement parent)
        {
            boundaryPanel=new Foldout { text="境界を閉じる",value=false,name="boundary-panel" };parent.Add(boundaryPanel);parent=boundaryPanel;
            boundaryPanel.RegisterValueChangedCallback(_=>UpdateBoundaryHighlight());
            parent.Add(new Label("閉じる境界"));
            boundaryChoice=new DropdownField { name="boundary-choice" };parent.Add(boundaryChoice);
            boundaryChoice.RegisterValueChangedCallback(_=> { UpdateBoundaryTooltip();UpdateBoundaryHighlight(); });
            fillBoundaryButton=Button("境界に面を張る",()=>Try(()=>
            {
                int index=boundaryChoice.index;
                if(index<0 || index>=boundaryLoops.Length) throw new InvalidOperationException("閉じる境界を選んでください。");
                Execute(AuthoringOperation.FillPolygonBoundary(activeEditContext,boundaryLoops[index]));
            }),"fill-boundary");parent.Add(fillBoundaryButton);
            fillBoundaryButton.tooltip="選んだ閉じた境界を1面でふさぎます。既存面のUVは保持し、新しい面だけUVを投影します。";
            BuildBridge(parent);
        }
        void RefreshBoundaryEditing(PolygonMesh polygon,bool editable)
        {
            int previous=boundaryChoice.index;string reason="開いた境界なし";
            boundaryLoops=Array.Empty<ulong[]>();
            if(editable) try { boundaryLoops=PolygonBoundaries.Find(polygon).ToArray(); }
            catch(Exception e) { reason=e.Message; }
            boundaryChoice.choices=boundaryLoops.Length==0 ? new System.Collections.Generic.List<string>{reason} :
                boundaryLoops.Select((loop,i)=>"境界"+(i+1)+" / "+loop.Length+"頂点").ToList();
            boundaryChoice.index=boundaryLoops.Length==0 ? 0 : Math.Max(0,Math.Min(previous,boundaryLoops.Length-1));
            boundaryChoice.SetEnabled(editable && boundaryLoops.Length>0);fillBoundaryButton.SetEnabled(editable && boundaryLoops.Length>0);
            UpdateBoundaryTooltip();
            RefreshBridgeChoices();
            UpdateBoundaryHighlight();
        }
        void UpdateBoundaryTooltip()
        {
            int index=boundaryChoice.index;
            boundaryChoice.tooltip=index>=0 && index<boundaryLoops.Length ? "頂点ID: "+string.Join(", ",boundaryLoops[index]) : boundaryChoice.value;
        }
        void UpdateBoundaryHighlight()
        {
            UpdateBridgeHighlight();
            var value=DisplayedGraphValue();int index=boundaryChoice?.index ?? -1;
            if(!boundaryPanel.value || activeEditContext==null || value?.Polygon==null || index<0 || index>=boundaryLoops.Length)
            { boundaryHighlight?.Clear();return; }
            if(boundaryHighlight==null) boundaryHighlight=new BoundaryHighlightProjection(stage.transform);
            boundaryHighlight.Show(value.Polygon,value.Transform,boundaryLoops[index]);
        }
    }
}
