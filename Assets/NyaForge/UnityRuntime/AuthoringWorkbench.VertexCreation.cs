using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout vertexCreatePanel;
        FloatField vertexCreateX,vertexCreateY,vertexCreateZ;
        Button vertexCreateButton;
        void BuildVertexCreation(VisualElement parent)
        {
            vertexCreatePanel=new Foldout { text="新しい頂点を置く",value=false,name="vertex-create-panel" };parent.Add(vertexCreatePanel);
            vertexCreatePanel.Add(new Label("モデル内の座標（mm）"));
            vertexCreateX=Number(vertexCreatePanel,"X",0,"vertex-create-x");
            vertexCreateY=Number(vertexCreatePanel,"Y",0,"vertex-create-y");
            vertexCreateZ=Number(vertexCreatePanel,"Z",0,"vertex-create-z");
            vertexCreateButton=Button("この位置に頂点を追加",()=>Try(()=>
            {
                var value=DisplayedGraphValue();long before=workspace.Document.DocumentRevision;
                var position=new Vec3(vertexCreateX.value,vertexCreateY.value,vertexCreateZ.value)*(1f/(1000*value.Transform.Scale));
                Execute(AuthoringOperation.AddPolygonVertex(activeEditContext,position));
                if(workspace.Document.DocumentRevision==before) return;
                value=DisplayedGraphValue();ulong id=value.Polygon.IdWatermarks.Vertex;
                Select(PolygonEditPoints.VertexIds(value.Polygon).Select((vertex,index)=>(vertex,index)).Where(p=>p.vertex==id).Select(p=>p.index));
            }),"add-polygon-vertex");vertexCreatePanel.Add(vertexCreateButton);
        }
    }
}
