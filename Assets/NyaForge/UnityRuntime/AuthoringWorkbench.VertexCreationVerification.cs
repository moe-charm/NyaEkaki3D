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
        IEnumerator VerifyVertexCreation(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();faceMode.value=false;Select(new[]{0,1});Frame();
                    vertexCreatePanel.value=true;vertexCreateX.value=300;vertexCreateY.value=200;vertexCreateZ.value=0;
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;controls.ScrollTo(vertexCreateButton);yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    PointerProbe.Click(vertexCreateButton);
                    var polygon=DisplayedGraphValue().Polygon;
                    Check(polygon.Vertices.Count==5 && projection.Points.Length==5 && workspace.Evaluate().VertexCount==4,"Loose vertex leaked into render mesh or missing from edit points");
                    Check(SelectedPolygonVertices().SequenceEqual(new ulong[]{5}),"New loose vertex not selected");
                    Frame();Select(Array.Empty<int>());PointerProbe.ClickAt(view,VertexPanelPoint(projection.Points[4]));
                    Check(SelectedPolygonVertices().SequenceEqual(new ulong[]{5}),"Loose point click did not select stable ID");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Vertex creation Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Vertex creation Redo differs");
                    string directory=Path.Combine(output,"loose-vertex-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after,"Vertex creation native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","loose-vertex"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Vertex creation Bake differs");
                    SelectEditStage(1);Check(projection.Points.Length==5,"Reopened loose point missing");Frame();controls.ScrollTo(vertexCreateButton);SetStatus("未接続頂点を追加。クリック選択・Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"loose-vertex.png"),error=>failure=error);
                if(failure==null) yield return VerifyLooseVertexWorkflow(output,error=>failure=error);
                completed(failure);
            }
            finally { vertexCreatePanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}



