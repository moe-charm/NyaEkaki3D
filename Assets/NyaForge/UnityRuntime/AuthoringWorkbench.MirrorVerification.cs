using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyMirror(string output,Action<string> completed)
        {
            var previous=workspace;string path=savedDirectory,failure=null;
            try
            {
                try {ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);controls.ScrollTo(root.Q<Button>("graph-create-mirror"));}
                catch(Exception e){failure=e.ToString();}
                yield return null;yield return null;
                if(failure==null) try
                {
                    PointerProbe.Click(root.Q<Button>("graph-create-mirror"));
                    Check(workspace.Preview.Output.Polygon.Faces.Count==2,"Mirror starter did not duplicate source");
                    SelectEditStage(1); Frame();
                    Check(projection.FinalMesh != null && projection.FinalMesh.vertexCount == 8 && projection.Points.Length == 4, "Final backdrop contaminated cage vertices");
                    string displayState = workspace.Document.StateHash;
                    long displayRevision = workspace.Document.DocumentRevision;
                    finalPreview.value = false;
                    Check(projection.FinalMesh == null, "Final preview toggle did not hide backdrop");
                    finalPreview.value = true;
                    Check(projection.FinalMesh != null && workspace.Document.StateHash == displayState && workspace.Document.DocumentRevision == displayRevision, "Display toggle changed authoring state");
                    var editablePoint = projection.Points[0];
                    PointerProbe.ClickAt(view, VertexPanelPoint(new Vector3(-editablePoint.x, editablePoint.y, editablePoint.z)));
                    Check(selection.Count == 0, "Reflected result was incorrectly selectable");
                    PointerProbe.ClickAt(view, VertexPanelPoint(editablePoint));
                    Check(selection.SetEquals(new[] { 0 }), "Editable cage point was not selected through the backdrop");
                    moveX.SetValueWithoutNotify(10);moveY.SetValueWithoutNotify(0);moveZ.SetValueWithoutNotify(0);MoveSelection();
                    var p=workspace.Preview.Output.Polygon;
                    Check(Math.Abs(p.Vertices[1].Position.X+p.Vertices[5].Position.X)<1e-6,"Mirror did not follow upstream edit");
                    string state=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==state,"Mirror Undo/Redo mismatch");
                    string mirrorId = workspace.Document.Objects[0].Graph.Nodes.Values.Single(n => n.TypeId == BuiltinNodes.Mirror).NodeId;
                    Execute(AuthoringOperation.Disconnect(mirrorId, "mesh"));
                    Check(!workspace.Preview.IsComplete && projection.FinalMesh == null && projection.Points.Length == 4, "Incomplete graph retained a stale backdrop or lost the editable cage");
                    Execute(AuthoringOperation.Undo());
                    Check(projection.FinalMesh != null && workspace.Document.StateHash == state, "Undo did not restore final backdrop");
                    string dir=Path.Combine(output,"mirror-project");projectPath.SetValueWithoutNotify(dir);SaveProject();OpenProject();Check(workspace.Document.StateHash==state,"Mirror native roundtrip changed state");
                    var bake=BakeStore.Read(BakeStore.Export(Path.Combine(dir,"exports","mirror"),workspace));Check(bake.Mesh.ContentHash==workspace.Evaluate().ContentHash,"Mirror Bake mismatch");
                    SelectEditStage(1);Frame();controls.ScrollTo(editStage);SetStatus("青緑＋点: 編集対象 / 灰色: 最終結果。対称側を確認しながら編集できます。");
                }
                catch(Exception e){failure=e.ToString();}
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"mirror.png"),error=>failure=error);
                completed(failure);
            }
            finally{ReplaceWorkspace(previous,path);}
        }
    }
}
