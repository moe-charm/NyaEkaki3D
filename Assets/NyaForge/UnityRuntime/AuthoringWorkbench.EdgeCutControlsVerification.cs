using System;
using System.Collections;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyEdgeCutControls(Action<string> completed)
        {
            string failure=null,before=workspace.Document.StateHash;
            edgeCutA.value="";edgeCutB.value="";
            foreach(var item in new[]{(name:"cut-register-a",ids:new ulong[]{1,4}),(name:"cut-register-b",ids:new ulong[]{2,3})})
            {
                Select(PolygonEditPoints.VertexIds(DisplayedGraphValue().Polygon).Select((id,index)=>(id,index)).Where(p=>item.ids.Contains(p.id)).Select(p=>p.index));
                var button=root.Q<Button>(item.name);
                yield return null;yield return null;controls.ScrollTo(button);yield return null;yield return null;
                try { PointerProbe.Click(button); }
                catch(Exception e) { failure=e.ToString();break; }
            }
            if(failure==null) try
            {
                Check(ReadCutEdges().SequenceEqual(new ulong[]{1,4,2,3}),"Registered edge IDs differ");
                edgeCutFirst.value=0;edgeCutSecond.value=100;
                Check(edgeCutButton.enabledSelf && edgeCutLine.VertexCount==2,"Endpoint cut preview missing");
                Check(workspace.Document.StateHash==before,"Register/endpoint preview modified document");
                edgeCutPanel.value=false;Check(edgeCutLine.VertexCount==0,"Closed cut panel retained line");
                edgeCutPanel.value=true;Check(edgeCutLine.VertexCount==2,"Reopened cut panel lost preview");
            }
            catch(Exception e) { failure=e.ToString(); }
            controls.ScrollTo(edgeCutButton);yield return null;yield return null;
            if(failure==null) try
            {
                PointerProbe.Click(edgeCutButton);
                Check(DisplayedGraphValue().Polygon.Vertices.Count==4 && workspace.Evaluate().TriangleCount==2 && DisplayedGraphValue().Polygon.Faces.Count==2,"Endpoint cut created extra points");
                Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Endpoint cut Undo differs");
                edgeCutA.value="1, 4";edgeCutB.value="2, 3";
            }
            catch(Exception e) { failure=e.ToString(); }
            var clear=root.Q<Button>("clear-edge-cut");controls.ScrollTo(clear);yield return null;yield return null;
            if(failure==null) try
            {
                PointerProbe.Click(clear);
                Check(edgeCutA.value=="" && edgeCutB.value=="" && edgeCutLine.VertexCount==0 && workspace.Document.StateHash==before,"Clear changed document or left draft");
                edgeCutA.value="1, 4";edgeCutB.value="2, 3";SelectEditStage(0);
                Check(edgeCutA.value=="" && edgeCutB.value=="" && edgeCutLine.VertexCount==0,"Stage change retained cut draft");
                SelectEditStage(1);edgeCutA.value="1, 4";edgeCutB.value="2, 3";edgeCutFirst.value=25;edgeCutSecond.value=75;
            }
            catch(Exception e) { failure=e.ToString(); }
            completed(failure);
        }
    }
}
