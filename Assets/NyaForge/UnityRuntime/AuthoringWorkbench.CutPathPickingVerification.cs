using System;
using System.Collections;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyCutPathPicking(Action<string> completed)
        {
            string failure=null,before=workspace.Document.StateHash;
            cutPathText.value="";cutPathPickMode.value=true;Select(new[]{0});
            yield return null;yield return null;
            try
            {
                Check(CutPathPicking,"Cut path click mode not enabled");
                var value=DisplayedGraphValue();
                foreach(var pair in new[]{new ulong[]{1,4},new ulong[]{2,5},new ulong[]{3,6}})
                {
                    var local=(value.Polygon.Vertices[pair[0]].Position+value.Polygon.Vertices[pair[1]].Position)*.5f;
                    var p=value.Transform.ToAvatarPoint(local);
                    var screen=VertexPanelPoint(new Vector3(p.X,p.Y,p.Z));
                    PointerProbe.Move(view,screen);
                    Check(cutHoverHit!=null && cutHoverHit.Location.Edge.Equals(new NyaForge.Authoring.Topology.CageEdgeId(pair[0],pair[1])) && cutHoverEdge.VertexCount==2 && cutHoverPoint.style.display.value==DisplayStyle.Flex,"Hover candidate missing");
                    Check(workspace.Document.StateHash==before && selection.SetEquals(new[]{0}),"Hover modified document/selection");
                    PointerProbe.ClickAt(view,screen);
                    Check(cutHoverHit==null && cutHoverEdge.VertexCount==0,"Click retained hover candidate");
                }
                PointerProbe.Move(view,view.worldBound.position-new Vector2(10,10));
                Check(cutHoverHit==null && cutHoverPoint.style.display.value==DisplayStyle.None,"Outside view retained hover");
                var path=ReadCutPath();Check(path.Length==3,"Viewport clicks did not register three edges");
                Check(Math.Abs(path[0].Fraction-.5f)<.001f && Math.Abs(path[1].Fraction-.5f)<.001f && Math.Abs(path[2].Fraction-.5f)<.001f,"Viewport midpoint fractions differ");
                Check(cutPathLine.VertexCount==3 && cutPathCommit.enabledSelf && selection.SetEquals(new[]{0}) && workspace.Document.StateHash==before,"Viewport clicks modified selection/document or lost preview");
                using(var key=KeyDownEvent.GetPooled(Event.KeyboardEvent("escape"))) { key.target=view;view.SendEvent(key); }
                Check(!CutPathPicking && ReadCutPath().Length==3,"Escape did not exit mode preserving draft");
                PointerProbe.ClickAt(view,VertexPanelPoint(projection.Points[1]));
                Check(selection.SetEquals(new[]{1}) && ReadCutPath().Length==3,"Normal point selection did not resume");
                cutPathPickMode.value=true;cutPathPanel.value=false;Check(!cutPathPickMode.value,"Closed path panel retained click mode");
                cutPathPanel.value=true;Check(!CutPathPicking && cutPathLine.VertexCount==3,"Reopened panel enabled click mode or lost draft");
            }
            catch(Exception e) { failure=e.ToString(); }
            completed(failure);
        }
    }
}

