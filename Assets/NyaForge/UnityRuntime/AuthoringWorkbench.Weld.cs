using System.Linq;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button weldButton;
        void BuildWeld(VisualElement parent)
        {
            weldButton=Button("選択頂点を中心に統合",()=>Try(()=>
            {
                var ids=SelectedPolygonVertices();
                long before=workspace.Document.DocumentRevision;
                Execute(AuthoringOperation.WeldPolygonVertices(activeEditContext,ids));
                if(workspace.Document.DocumentRevision==before) return;
                var value=DisplayedGraphValue();ulong retained=ids.Min();
                Select(value.PolygonRendering.RenderVertexMap.Select((binding,index)=>(binding,index)).Where(p=>p.binding.VertexId==retained).Select(p=>p.index));
            }),"weld-vertices");parent.Add(weldButton);
            weldButton.tooltip="点モードで2頂点以上を選択。平均位置へ統合し、潰れた面は除去します。各面のUVは残しますが、面内で潰れる角は最小IDの属性を採用します。";
        }
    }
}
