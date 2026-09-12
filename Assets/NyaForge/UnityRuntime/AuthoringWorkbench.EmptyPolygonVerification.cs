using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyEmptyPolygon(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);
                var start=root.Q<Button>("graph-create-empty-polygon");
                yield return null;yield return null;controls.ScrollTo(start);yield return null;yield return null;
                try
                {
                    PointerProbe.Click(start);
                    Check(workspace.Preview.IsComplete && DisplayedGraphValue().Polygon.Faces.Count==0 && projection.Points.Length==0,"Empty polygon start failed");
                    Check(!uvProject.enabledSelf && !solidifyButton.enabledSelf && !addPaint.enabledSelf,"Faceless tools enabled");
                    vertexCreatePanel.value=true;faceMode.value=false;
                }
                catch(Exception e) { failure=e.ToString(); }
                for(int i=0;i<3 && failure==null;i++)
                {
                    vertexCreateX.value=i==1 ? 100 : 0;vertexCreateY.value=i==2 ? 100 : 0;vertexCreateZ.value=0;
                    yield return null;yield return null;controls.ScrollTo(vertexCreateButton);yield return null;yield return null;
                    try
                    {
                        PointerProbe.Click(vertexCreateButton);
                        Check(projection.Points.Length==i+1 && workspace.Preview.Output.Mesh==null,"Faceless point addition failed");
                        string hash=workspace.Document.StateHash;
                        projectPath.SetValueWithoutNotify(Path.Combine(output,"empty-stage-"+i));SaveProject();OpenProject();
                        Check(workspace.Document.StateHash==hash,"Faceless GUI save differs");SelectEditStage(1);
                        Check(projection.Points.Length==i+1,"Faceless points missing after reopen");
                    }
                    catch(Exception e) { failure=e.ToString(); }
                }
                if(failure==null) { faceCreatePanel.value=true;facePerimeter.value="1, 2, 3"; }
                yield return null;yield return null;controls.ScrollTo(faceCreateButton);yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;PointerProbe.Click(faceCreateButton);
                    Check(workspace.Evaluate()?.TriangleCount==1 && projection.DisplayMesh!=null,"First face not rendered");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before && projection.DisplayMesh==null && projection.Points.Length==3,"First face Undo did not restore points");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"First face Redo differs");
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"first-face-project"));SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after,"First face reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(output,"first-face-bake"),workspace)).Mesh.TriangleCount==1,"First face Bake differs");
                    SelectEditStage(1);Frame();SetStatus("空の形状から3点を置き、最初の面を作成。保存・Undo・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"first-face.png"),error=>failure=error);
                if(failure==null) yield return VerifyDeleteAllFaces(output,error=>failure=error);
                completed(failure);
            }
            finally { faceCreatePanel.value=false;vertexCreatePanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
