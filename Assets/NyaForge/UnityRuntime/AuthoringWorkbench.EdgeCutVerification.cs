using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyEdgeCut(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();faceMode.value=false;Select(new[]{0,1});Frame();
                    edgeCutPanel.value=true;edgeCutA.value="1, 4";edgeCutB.value="2, 3";edgeCutFirst.value=25;edgeCutSecond.value=75;
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return VerifyEdgeCutControls(error=>failure=error);
                yield return null;yield return null;controls.ScrollTo(edgeCutButton);yield return null;yield return null;
                if(failure==null) try
                {
                    string before=workspace.Document.StateHash;
                    Check(edgeCutLine.VertexCount==2 && edgeCutButton.enabledSelf,"Cut preview missing");PointerProbe.Click(edgeCutButton);
                    var polygon=DisplayedGraphValue().Polygon;
                    Check(polygon.Vertices.Count==6 && polygon.Faces.Count==2 && workspace.Evaluate().TriangleCount==4,"Edge cut GUI geometry differs");
                    Check(selection.Count==0 && edgeCutLine.VertexCount==0,"Cut selection/preview not cleared");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Edge cut Undo differs");
                    Select(new[]{0,1});edgeCutA.value="1, 4";edgeCutB.value="1, 4";
                    Check(!edgeCutButton.enabledSelf && edgeCutLine.VertexCount==0 && workspace.Document.StateHash==before && selection.SetEquals(new[]{0,1}),"Invalid cut did not preserve state/selection");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Edge cut Redo differs");
                    string directory=Path.Combine(output,"edge-cut-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==after,"Edge cut native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","inserted"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Edge cut Bake differs");
                    SelectEditStage(1);Frame();controls.ScrollTo(edgeCutPanel);SetStatus("辺の25%と75%位置を結んで切断。Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"edge-cut.png"),error=>failure=error);
                completed(failure);
            }
            finally { edgeCutPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}

