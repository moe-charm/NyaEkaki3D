using System.Linq;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout edgeInsertPanel;
        FloatField edgeInsertPercent;
        Label edgeInsertInfo;
        Button edgeInsertButton;
        void BuildEdgeInsertion(VisualElement parent)
        {
            edgeInsertPanel=new Foldout { text="辺に頂点を追加",value=false,name="edge-insert-panel" };parent.Add(edgeInsertPanel);
            edgeInsertInfo=new Label { name="edge-insert-endpoints" };edgeInsertPanel.Add(edgeInsertInfo);
            edgeInsertPercent=Number(edgeInsertPanel,"位置 (%)",50,"edge-insert-percent");
            edgeInsertPercent.tooltip="小さい頂点IDから大きい頂点IDへ向かう位置。50で中央、0と100は指定できません。";
            edgeInsertButton=Button("選択辺に頂点を追加",()=>Try(()=>
            {
                long before=workspace.Document.DocumentRevision;
                Execute(AuthoringOperation.InsertPolygonEdgeVertex(activeEditContext,SelectedPolygonVertices(),edgeInsertPercent.value/100));
                if(workspace.Document.DocumentRevision==before) return;
                var value=DisplayedGraphValue();ulong created=value.Polygon.IdWatermarks.Vertex;
                Select(value.PolygonRendering.RenderVertexMap.Select((binding,index)=>(binding,index)).Where(p=>p.binding.VertexId==created).Select(p=>p.index));
            }),"insert-edge-vertex");edgeInsertPanel.Add(edgeInsertButton);
        }
        void RefreshEdgeInsertion(bool editable,bool faceSelection)
        {
            var ids=SelectedPolygonVertices();bool enabled=editable && !faceSelection && ids.Length==2;
            edgeInsertButton.SetEnabled(enabled);edgeInsertPercent.SetEnabled(enabled);
            edgeInsertInfo.text=enabled ? "頂点ID "+ids[0]+" → "+ids[1] : "点モードで辺の両端を選択";
        }
    }
}
