using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout edgeCutPanel;
        TextField edgeCutA,edgeCutB;
        FloatField edgeCutFirst,edgeCutSecond;
        Label edgeCutInfo;
        Button edgeCutButton;
        BoundaryHighlightProjection edgeCutLine;
        string edgeCutContext;
        void BuildEdgeCut(VisualElement parent)
        {
            edgeCutPanel=new Foldout { text="辺の途中で面を切る",value=false,name="edge-cut-panel" };parent.Add(edgeCutPanel);
            edgeCutPanel.Add(new Label("点モードで辺の両端2点を選択して登録"));
            edgeCutA=new TextField("辺Aの頂点ID") { name="cut-edge-a" };edgeCutA.style.flexDirection=FlexDirection.Column;edgeCutPanel.Add(edgeCutA);
            edgeCutPanel.Add(Button("選択を辺Aに登録",()=>RegisterCutEdge(edgeCutA),"cut-register-a"));
            edgeCutFirst=Number(edgeCutPanel,"辺Aの位置 (%)",50,"cut-first-percent");
            edgeCutB=new TextField("辺Bの頂点ID") { name="cut-edge-b" };edgeCutB.style.flexDirection=FlexDirection.Column;edgeCutPanel.Add(edgeCutB);
            edgeCutPanel.Add(Button("選択を辺Bに登録",()=>RegisterCutEdge(edgeCutB),"cut-register-b"));
            edgeCutSecond=Number(edgeCutPanel,"辺Bの位置 (%)",50,"cut-second-percent");
            edgeCutPanel.Add(new Label("小さいID→大きいID。0/100は端点。"));
            edgeCutInfo=new Label();edgeCutInfo.style.whiteSpace=WhiteSpace.Normal;edgeCutPanel.Add(edgeCutInfo);
            edgeCutButton=Button("切断を確定",()=>Try(()=>
            {
                long before=workspace.Document.DocumentRevision;
                Execute(AuthoringOperation.CutPolygonBetweenEdges(activeEditContext,ReadCutEdges(),edgeCutFirst.value/100,edgeCutSecond.value/100));
                if(workspace.Document.DocumentRevision!=before) { selection.Clear();Refresh(); }
            }),"commit-edge-cut");edgeCutPanel.Add(edgeCutButton);
            edgeCutPanel.Add(Button("切断の指定をクリア",()=> { edgeCutA.SetValueWithoutNotify("");edgeCutB.SetValueWithoutNotify("");RefreshEdgeCut(); },"clear-edge-cut"));
            edgeCutA.RegisterValueChangedCallback(_=>RefreshEdgeCut());edgeCutB.RegisterValueChangedCallback(_=>RefreshEdgeCut());
            edgeCutFirst.RegisterValueChangedCallback(_=>RefreshEdgeCut());edgeCutSecond.RegisterValueChangedCallback(_=>RefreshEdgeCut());
            edgeCutPanel.RegisterValueChangedCallback(_=>RefreshEdgeCut());
        }
        void RegisterCutEdge(TextField field)=>Try(()=>
        {
            var ids=SelectedPolygonVertices();
            if(faceMode.value || ids.Length!=2) throw new InvalidOperationException("点モードで辺の両端を選択してください。");
            field.value=string.Join(", ",ids);
        });
        ulong[] ReadCutEdges()
        {
            ulong[] Read(string text)
            {
                var words=(text??"").Split(new[]{',',' '},StringSplitOptions.RemoveEmptyEntries);
                if(words.Length!=2) throw new InvalidOperationException("各辺の頂点IDを2つ指定してください。");
                return words.Select(w=> { if(!ulong.TryParse(w,out var id) || id==0) throw new InvalidOperationException("頂点IDは正の整数です。");return id; }).ToArray();
            }
            return Read(edgeCutA.value).Concat(Read(edgeCutB.value)).ToArray();
        }
        void RefreshEdgeCut()
        {
            if(edgeCutButton==null) return;
            string context=activeEditContext==null ? null : activeEditContext.NodeId+"/"+workspace.Document.StateHash;
            if(context!=edgeCutContext) { edgeCutContext=context;edgeCutA.SetValueWithoutNotify("");edgeCutB.SetValueWithoutNotify(""); }
            edgeCutButton.SetEnabled(false);edgeCutLine?.Clear();if(!edgeCutPanel.value) return;
            try
            {
                var value=DisplayedGraphValue();
                if(activeEditContext==null || value?.Polygon==null) throw new InvalidOperationException("PolygonEdit段で操作してください。");
                var ids=ReadCutEdges();float a=edgeCutFirst.value/100,b=edgeCutSecond.value/100;
                var candidate=PolygonEdgeCut.Cut(value.Polygon,ids,a,b);
                ulong allocated=value.Polygon.IdWatermarks.Vertex;
                ulong start=a==0 ? Math.Min(ids[0],ids[1]) : a==1 ? Math.Max(ids[0],ids[1]) : ++allocated;
                ulong end=b==0 ? Math.Min(ids[2],ids[3]) : b==1 ? Math.Max(ids[2],ids[3]) : ++allocated;
                if(edgeCutLine==null) edgeCutLine=new BoundaryHighlightProjection(stage.transform,new Color(1,.4f,.7f),false);
                edgeCutLine.Show(candidate,value.Transform,new[]{start,end});edgeCutButton.SetEnabled(true);edgeCutInfo.text="ピンクの線で面を切れます。確定まで文書は変わりません。";
            }
            catch(Exception e) { edgeCutInfo.text=e.Message; }
        }
    }
}
