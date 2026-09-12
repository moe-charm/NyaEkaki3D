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
        BoundaryHighlightProjection cutHoverEdge;
        VisualElement cutHoverPoint;
        Label cutHoverInfo;
        PolygonEdgeScreenHit cutHoverHit;
        void BuildCutPathHover(VisualElement parent)
        {
            cutHoverInfo=new Label();cutHoverInfo.style.whiteSpace=WhiteSpace.Normal;parent.Add(cutHoverInfo);
            cutHoverPoint=new VisualElement { name="cut-hover-point",pickingMode=PickingMode.Ignore };
            cutHoverPoint.style.position=Position.Absolute;cutHoverPoint.style.width=10;cutHoverPoint.style.height=10;
            cutHoverPoint.style.backgroundColor=new Color(1,.85f,.2f);cutHoverPoint.style.display=DisplayStyle.None;
            view.Add(cutHoverPoint);
            view.RegisterCallback<PointerMoveEvent>(e=>RefreshCutPathHover(e.position));
            view.RegisterCallback<PointerLeaveEvent>(_=>ClearCutPathHover());
            view.RegisterCallback<PointerDownEvent>(_=>ClearCutPathHover());
        }
        void ClearCutPathHover()
        {
            cutHoverHit=null;cutHoverEdge?.Clear();
            if(cutHoverPoint!=null) cutHoverPoint.style.display=DisplayStyle.None;
            if(cutHoverInfo!=null) cutHoverInfo.text="";
        }
        void RefreshCutPathHover(Vector2 position)
        {
            ClearCutPathHover();
            if(!active || !CutPathPicking || orbiting || !view.worldBound.Contains(position)) return;
            try
            {
                var value=DisplayedGraphValue();if(value?.Polygon==null) return;
                var hit=FindCutPathHit(position);
                if(hit==null) return;
                cutHoverHit=hit;var edge=hit.Location.Edge;
                if(cutHoverEdge==null) cutHoverEdge=new BoundaryHighlightProjection(stage.transform,new Color(1,.85f,.2f),false);
                cutHoverEdge.Show(value.Polygon,value.Transform,new[]{edge.A,edge.B});
                var a=value.Polygon.Vertices[edge.A].Position;var b=value.Polygon.Vertices[edge.B].Position;
                var world=value.Transform.ToAvatarPoint(a*(1-hit.Location.Fraction)+b*hit.Location.Fraction);
                var local=view.WorldToLocal(VertexPanelPoint(new Vector3(world.X,world.Y,world.Z)));
                cutHoverPoint.style.left=local.x-5;cutHoverPoint.style.top=local.y-5;cutHoverPoint.style.display=DisplayStyle.Flex;
                bool duplicate=ReadCutPath().Any(p=>p.Edge.Equals(edge));
                cutHoverInfo.text="候補 "+edge.A+"–"+edge.B+" / "+(hit.Location.Fraction*100).ToString("0.0",CultureInfo.InvariantCulture)+"%"+(duplicate ? "（登録済み）" : "：クリックで追加");
            }
            catch(Exception e) { ClearCutPathHover();cutHoverInfo.text=e.Message; }
        }
    }
}

