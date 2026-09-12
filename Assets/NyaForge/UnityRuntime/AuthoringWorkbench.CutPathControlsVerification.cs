using System;
using System.Collections;
using System.Linq;
using NyaForge.Authoring.Topology;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyCutPathControls(Action<string> completed)
        {
            string failure=null,before=workspace.Document.StateHash;
            cutPathText.value="";cutPathPercent.value=50;
            var add=root.Q<Button>("cut-path-add");
            foreach(var ids in new[]{new ulong[]{1,4},new ulong[]{2,5},new ulong[]{3,6}})
            {
                Select(PolygonEditPoints.VertexIds(DisplayedGraphValue().Polygon).Select((id,index)=>(id,index)).Where(p=>ids.Contains(p.id)).Select(p=>p.index));
                yield return null;yield return null;controls.ScrollTo(add);yield return null;yield return null;
                try { PointerProbe.Click(add); }
                catch(Exception e) { failure=e.ToString();break; }
            }
            if(failure==null) try
            {
                Check(ReadCutPath().Select(p=>p.Edge).SequenceEqual(new[]{new CageEdgeId(1,4),new CageEdgeId(2,5),new CageEdgeId(3,6)}),"Path registration order differs");
                Check(cutPathLine.VertexCount==3 && cutPathCommit.enabledSelf,"Registered path preview missing");
                string draft=cutPathText.value;PointerProbe.Click(add);
                Check(cutPathText.value==draft && workspace.Document.StateHash==before,"Repeated edge changed draft/document");
                Select(PolygonEditPoints.VertexIds(DisplayedGraphValue().Polygon).Select((id,index)=>(id,index)).Where(p=>p.id==1 || p.id==6).Select(p=>p.index));
                PointerProbe.Click(add);Check(cutPathText.value==draft,"Non-edge selection was registered");
                cutPathText.value=draft+"\nwrong row";Check(!cutPathCommit.enabledSelf && cutPathLine.VertexCount==0,"Malformed row retained preview");
            }
            catch(Exception e) { failure=e.ToString(); }
            var remove=root.Q<Button>("cut-path-remove");yield return null;yield return null;controls.ScrollTo(remove);yield return null;yield return null;
            if(failure==null) try
            {
                PointerProbe.Click(remove);Check(ReadCutPath().Length==3 && cutPathCommit.enabledSelf && cutPathLine.VertexCount==3,"Malformed tail could not be removed");
                PointerProbe.Click(remove);Check(ReadCutPath().Length==2 && cutPathLine.VertexCount==2,"Last edge removal differs");
                PointerProbe.Click(remove);Check(ReadCutPath().Length==1 && !cutPathCommit.enabledSelf && cutPathLine.VertexCount==0,"One-point path remained actionable");
            }
            catch(Exception e) { failure=e.ToString(); }
            var clear=root.Q<Button>("cut-path-clear");yield return null;yield return null;controls.ScrollTo(clear);yield return null;yield return null;
            if(failure==null) try
            {
                PointerProbe.Click(clear);Check(cutPathText.value=="" && workspace.Document.StateHash==before,"Clear changed document or retained draft");
                cutPathText.value="1 4 50\n2 5 50\n3 6 50";SelectEditStage(0);
                Check(cutPathText.value=="" && cutPathLine.VertexCount==0,"Stage change retained path draft");
                SelectEditStage(1);cutPathText.value="1 4 50\n2 5 50\n3 6 50";
                Check(workspace.Document.StateHash==before,"Path controls changed document");
            }
            catch(Exception e) { failure=e.ToString(); }
            completed(failure);
        }
    }
}
