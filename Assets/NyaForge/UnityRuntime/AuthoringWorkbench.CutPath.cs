using System;
using System.Globalization;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout cutPathPanel;
        TextField cutPathText;
        FloatField cutPathPercent;
        Label cutPathInfo;
        Button cutPathCommit;
        BoundaryHighlightProjection cutPathLine;
        string cutPathContext;
        void BuildCutPath(VisualElement parent)
        {
            cutPathPanel=new Foldout { text="複数の面を連続で切る",value=false,name="cut-path-panel" };parent.Add(cutPathPanel);
            var help=new Label("点モードで辺の両端を選び、通過順に追加します。各面は1回だけ横断できます。");help.style.whiteSpace=WhiteSpace.Normal;cutPathPanel.Add(help);
            BuildCutPathPicking(cutPathPanel);
            cutPathPercent=Number(cutPathPanel,"辺上の位置 (%)",50,"cut-path-percent");
            cutPathPanel.Add(Button("選択した辺を末尾に追加",()=>Try(()=>
            {
                var ids=SelectedPolygonVertices();
                if(faceMode.value || ids.Length!=2) throw new InvalidOperationException("点モードで辺の両端2点を選択してください。");
                var point=new EdgeCutLocation(ids[0],ids[1],cutPathPercent.value/100);
                var polygon=DisplayedGraphValue()?.Polygon;
                if(activeEditContext==null || polygon==null || !polygon.EdgeFaces.ContainsKey(point.Edge)) throw new InvalidOperationException("選択した2点は編集段の辺ではありません。");
                var path=ReadCutPath();
                if(path.Length>=256 || path.Any(p=>p.Edge.Equals(point.Edge))) throw new InvalidOperationException("辺は重複なしで256個まで登録できます。");
                WriteCutPath(path.Concat(new[]{point}).ToArray());
            }),"cut-path-add"));
            cutPathText=new TextField("通過順：頂点ID 頂点ID 位置%") { multiline=true,name="cut-path-text" };
            cutPathText.style.flexDirection=FlexDirection.Column;cutPathText.style.minHeight=100;cutPathPanel.Add(cutPathText);
            var hint=new Label("1行に1辺。位置は小さいID→大きいID、0/100は端点。行を編集して位置や順序を修正できます。");hint.style.whiteSpace=WhiteSpace.Normal;cutPathPanel.Add(hint);
            cutPathPanel.Add(Button("最後の辺を取り消す",()=> { var rows=(cutPathText.value??"").Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Where(r=>!string.IsNullOrWhiteSpace(r)).ToArray();cutPathText.value=string.Join("\n",rows.Take(Math.Max(0,rows.Length-1))); },"cut-path-remove"));
            cutPathPanel.Add(Button("経路をクリア",()=>cutPathText.value="","cut-path-clear"));
            cutPathInfo=new Label();cutPathInfo.style.whiteSpace=WhiteSpace.Normal;cutPathPanel.Add(cutPathInfo);
            cutPathCommit=Button("経路全体の切断を確定",()=>Try(()=>
            {
                long before=workspace.Document.DocumentRevision;
                Execute(AuthoringOperation.CutPolygonPath(activeEditContext,ReadCutPath()));
                if(workspace.Document.DocumentRevision!=before) { selection.Clear();Refresh(); }
            }),"commit-cut-path");cutPathPanel.Add(cutPathCommit);
            cutPathText.RegisterValueChangedCallback(_=>RefreshCutPath());cutPathPanel.RegisterValueChangedCallback(_=>RefreshCutPath());
        }
        EdgeCutLocation[] ReadCutPath()
        {
            var rows=(cutPathText.value??"").Split(new[]{'\r','\n'},StringSplitOptions.RemoveEmptyEntries).Where(r=>!string.IsNullOrWhiteSpace(r)).Take(257).ToArray();
            if(rows.Length>256) throw new InvalidOperationException("経路は256辺までです。");
            return rows.Select(row=>
            {
                var words=row.Split(new[]{' ', ',', '\t'},StringSplitOptions.RemoveEmptyEntries);
                if(words.Length!=3 || !ulong.TryParse(words[0],out var a) || !ulong.TryParse(words[1],out var b) || a==0 || b==0 || a==b || !float.TryParse(words[2],NumberStyles.Float,CultureInfo.InvariantCulture,out var percent))
                    throw new InvalidOperationException("各行を「頂点ID 頂点ID 位置%」で指定してください。例：1 4 50");
                return new EdgeCutLocation(a,b,percent/100);
            }).ToArray();
        }
        void WriteCutPath(EdgeCutLocation[] path)=>cutPathText.value=string.Join("\n",path.Select(p=>p.Edge.A+" "+p.Edge.B+" "+(p.Fraction*100).ToString("R",CultureInfo.InvariantCulture)));
        void RefreshCutPath()
        {
            if(cutPathCommit==null) return;
            ClearCutPathHover();
            string context=activeEditContext==null ? null : activeEditContext.NodeId+"/"+workspace.Document.StateHash;
            if(context!=cutPathContext) { cutPathContext=context;cutPathText.SetValueWithoutNotify("");cutPathPickMode.SetValueWithoutNotify(false); }
            cutPathPickMode.SetEnabled(activeEditContext!=null);
            if(!cutPathPanel.value || activeEditContext==null) cutPathPickMode.SetValueWithoutNotify(false);
            RefreshViewportHint();cutPathCommit.SetEnabled(false);cutPathLine?.Clear();if(!cutPathPanel.value) return;
            try
            {
                var value=DisplayedGraphValue();
                if(activeEditContext==null || value?.Polygon==null) throw new InvalidOperationException("PolygonEdit段で操作してください。");
                var path=ReadCutPath();
                if(path.Length<2) throw new InvalidOperationException("切断する経路を2辺以上登録してください。");
                var candidate=PolygonCutPath.Cut(value.Polygon,path);ulong allocated=value.Polygon.IdWatermarks.Vertex;
                var ids=path.Select(p=>p.Fraction==0 ? p.Edge.A : p.Fraction==1 ? p.Edge.B : ++allocated).ToArray();
                if(cutPathLine==null) cutPathLine=new BoundaryHighlightProjection(stage.transform,new Color(1,.4f,.7f),false);
                cutPathLine.Show(candidate,value.Transform,ids);cutPathCommit.SetEnabled(true);
                cutPathInfo.text=(path.Length-1)+"面をピンクの経路で切断します。確定まで文書は変わりません。";
            }
            catch(Exception e) { cutPathInfo.text=e.Message; }
        }
    }
}




