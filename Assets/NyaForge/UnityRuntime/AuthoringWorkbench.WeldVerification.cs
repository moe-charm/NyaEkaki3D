using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void SelectWeldTestVertices(params ulong[] ids)
        {
            Select(DisplayedGraphValue().PolygonRendering.RenderVertexMap.Select((binding,index)=>(binding,index)).Where(p=>ids.Contains(p.binding.VertexId)).Select(p=>p.index));
        }
        IEnumerator VerifyWeld(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();faceMode.value=false;Select(new[]{0,1});Frame();
                    SelectWeldTestVertices(1,4);
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;controls.ScrollTo(weldButton);yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    PointerProbe.Click(weldButton);
                    var polygon=DisplayedGraphValue().Polygon;
                    Check(polygon.Vertices.Count==3 && polygon.Faces.Count==1 && polygon.Faces[0].Corners.Count==3 && workspace.Evaluate().TriangleCount==1,"Weld GUI geometry differs");
                    Check(SelectedPolygonVertices().Length==1 && SelectedPolygonVertices()[0]==1,"Welded vertex was not selected");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Weld Undo differs");
                    SelectWeldTestVertices(1,3);PointerProbe.Click(weldButton);
                    Check(workspace.Document.StateHash==before && SelectedPolygonVertices().SequenceEqual(new ulong[]{1,3}),"Invalid weld modified document or selection");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Weld Redo differs");
                    string directory=Path.Combine(output,"weld-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after,"Weld native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","welded"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Weld Bake differs");
                    SelectEditStage(1);Frame();controls.ScrollTo(weldButton);SetStatus("頂点を中心に統合。選択保持・Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"weld.png"),error=>failure=error);
                completed(failure);
            }
            finally { ReplaceWorkspace(previous,previousPath); }
        }
    }
}

