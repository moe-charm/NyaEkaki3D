using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyFaceSplit(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();faceMode.value=false;Select(Array.Empty<int>());Frame();
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                if(failure==null) try
                {
                    PointerProbe.ClickAt(view,VertexPanelPoint(projection.Points[0]));
                    var position=VertexPanelPoint(projection.Points[2]);
                    PointerProbe.Down(view,position,0,EventModifiers.Shift);PointerProbe.Up(view,position,0,EventModifiers.Shift);
                    Check(selection.SetEquals(new[]{0,2}) && splitFaceButton.enabledSelf,"Pointer selection did not enable face splitting");
                    controls.ScrollTo(splitFaceButton);
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    PointerProbe.Click(splitFaceButton);
                    var polygon=DisplayedGraphValue().Polygon;
                    Check(polygon.Faces.Count==2 && polygon.Faces.All(f=>f.Corners.Count==3) && workspace.Evaluate().TriangleCount==2,"GUI face split did not create two triangles");
                    Check(selection.Count==0 && !splitFaceButton.enabledSelf,"Split retained render-index selection");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Face split Undo differs");
                    Select(new[]{0,1});string invalidState=workspace.Document.StateHash;
                    PointerProbe.Click(splitFaceButton);
                    Check(workspace.Document.StateHash==invalidState && selection.SetEquals(new[]{0,1}),"Invalid split changed document or selection");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Face split Redo differs");
                    string directory=Path.Combine(output,"split-face-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after,"Face split native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","split"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Face split Bake differs");
                    SelectEditStage(1);faceMode.value=true;selectedFaces.Add(workspace.Preview.Output.Polygon.Faces[0].Id);Refresh();Frame();controls.ScrollTo(splitFaceButton);
                    SetStatus("面分割: 対角線で2面。無効選択の保持・Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"split-face.png"),error=>failure=error);
                completed(failure);
            }
            finally { ReplaceWorkspace(previous,previousPath); }
        }
    }
}
