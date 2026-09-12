using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyEdgeInsertion(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();faceMode.value=false;Select(new[]{0,1});Frame();
                    edgeInsertPanel.value=true;edgeInsertPercent.SetValueWithoutNotify(25);
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;controls.ScrollTo(edgeInsertButton);yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    PointerProbe.Click(edgeInsertButton);
                    var polygon=DisplayedGraphValue().Polygon;
                    Check(polygon.Vertices.Count==5 && polygon.Faces.Count==1 && polygon.Faces[0].Corners.Count==5 && workspace.Evaluate().TriangleCount==3,"Edge insertion GUI geometry differs");
                    Check(SelectedPolygonVertices().Length==1 && SelectedPolygonVertices()[0]==polygon.IdWatermarks.Vertex,"Inserted vertex was not selected");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Edge insertion Undo differs");
                    Select(new[]{0,1});edgeInsertPercent.SetValueWithoutNotify(0);PointerProbe.Click(edgeInsertButton);
                    Check(workspace.Document.StateHash==before && selection.SetEquals(new[]{0,1}),"Invalid insertion modified document or selection");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Edge insertion Redo differs");
                    string directory=Path.Combine(output,"edge-insert-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after,"Edge insertion native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","inserted"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Edge insertion Bake differs");
                    SelectEditStage(1);Frame();controls.ScrollTo(edgeInsertPanel);SetStatus("辺の25%位置に頂点を追加。Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"edge-insert.png"),error=>failure=error);
                completed(failure);
            }
            finally { edgeInsertPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
