using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyFaceEditing(string output, Action<string> completed)
        {
            var previous = workspace; string previousPath = savedDirectory; string failure = null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                    CreatePolygonGraph(); faceMode.value = true; Frame();
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                if (failure == null)
                    try
                    {
                        string selectionState = workspace.Document.StateHash;
                        PointerProbe.ClickAt(view, VertexPanelPoint(Vector3.zero));
                        Check(selectedFaces.SetEquals(new ulong[] { 1 }), "Face pointer did not select the quad");
                        Check(projection.HighlightedTriangleCount == 2 && !moveX.enabledSelf, "Face highlight or mode controls missing");
                        Check(workspace.Document.StateHash == selectionState, "Face selection changed document");
                        extrusionDepth.SetValueWithoutNotify(20); controls.ScrollTo(extrudeButton);
                    }
                    catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                if (failure == null)
                    try
                    {
                        PointerProbe.Click(extrudeButton);
                        Check(DisplayedGraphValue().Polygon.Faces.Count == 5 && workspace.Evaluate().TriangleCount == 10, "Extrusion did not produce a cap and four walls");
                        string state = workspace.Document.StateHash;
                        Execute(AuthoringOperation.Undo());
                        Check(DisplayedGraphValue().Polygon.Faces.Count == 1, "Extrusion Undo failed");
                        Execute(AuthoringOperation.Redo());
                        Check(workspace.Document.StateHash == state, "Extrusion Redo changed identity");
                        string directory = Path.Combine(output, "extruded-project");
                        projectPath.SetValueWithoutNotify(directory); SaveProject(); OpenProject();
                        Check(workspace.Document.StateHash == state && workspace.Preview.Output.Polygon.Faces.Count == 5, "Extruded project reopen lost faces");
                        string manifest = BakeStore.Export(Path.Combine(directory, "exports", "extruded"), workspace);
                        Check(BakeStore.Read(manifest).Mesh.ContentHash == workspace.Evaluate().ContentHash, "Extruded Bake differs");
                        SelectEditStage(1); faceMode.value = true;
                        selectedFaces.Add(1); Refresh();
                        Check(projection.HighlightedTriangleCount == 2 && workspace.Document.StateHash == state && !workspace.IsDirty, "Reopened face highlight changed document");
                        faceMode.value = false;
                        Check(projection.HighlightedTriangleCount == 0 && moveX.enabledSelf, "Vertex mode retained face overlay");
                        faceMode.value = true; selectedFaces.Add(1); Refresh();
                        Frame(); orbit = Quaternion.Euler(20, -30, 0); UpdateCamera();
                        controls.ScrollTo(faceMode);
                        SetStatus("面の押出し: 5面 / 10三角形。Undo・保存・出力を確認。");
                    }
                    catch (Exception e) { failure = e.ToString(); }
                if (failure == null)
                    yield return WorkbenchCapture.Write(GetComponent<UnityEngine.UIElements.UIDocument>(), camera, Path.Combine(output, "extruded.png"), error => failure = error);
                if(failure==null)
                {
                    controls.ScrollTo(deleteFacesButton);
                    yield return null;yield return null;
                    try
                    {
                        string beforeDeletion=workspace.Document.StateHash;
                        Check(deleteFacesButton.enabledSelf,"Face deletion control is disabled for a partial selection");
                        PointerProbe.Click(deleteFacesButton);
                        Check(DisplayedGraphValue().Polygon.Faces.Count==4 && workspace.Evaluate().TriangleCount==8,"Face deletion did not remove the cap");
                        Check(selectedFaces.Count==0 && projection.HighlightedTriangleCount==0,"Face deletion retained stale selection");
                        string deletedState=workspace.Document.StateHash;
                        Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==beforeDeletion,"Face deletion Undo differs");
                        Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==deletedState,"Face deletion Redo differs");
                        string directory=Path.Combine(output,"deleted-faces-project");
                        projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                        Check(workspace.Document.StateHash==deletedState && workspace.Preview.Output.Polygon.Faces.Count==4,"Deleted faces returned after reopen");
                        string bake=BakeStore.Export(Path.Combine(directory,"exports","deleted"),workspace);
                        Check(BakeStore.Read(bake).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Face deletion Bake differs");
                        SelectEditStage(1);faceMode.value=true;SelectElements(true);
                        Check(deleteFacesButton.enabledSelf,"All-face deletion should be available");
                        SelectElements(false);Frame();orbit=Quaternion.Euler(20,-30,0);UpdateCamera();
                        SetStatus("面削除: 4面 / 8三角形。Undo・保存・出力を確認。");
                    }
                    catch(Exception e) { failure=e.ToString(); }
                }
                if(failure==null)
                    yield return WorkbenchCapture.Write(GetComponent<UnityEngine.UIElements.UIDocument>(),camera,Path.Combine(output,"deleted-faces.png"),error=>failure=error);
                if(failure==null)
                {
                    controls.ScrollTo(fillBoundaryButton);
                    boundaryPanel.value=true;
                    yield return null;yield return null;
                    controls.ScrollTo(fillBoundaryButton);
                    yield return null;yield return null;
                    try { VerifyBoundaryHighlight(); }
                    catch(Exception e) { failure=e.ToString(); }
                    if(failure==null)
                        yield return WorkbenchCapture.Write(GetComponent<UnityEngine.UIElements.UIDocument>(),camera,Path.Combine(output,"boundary-highlight.png"),error=>failure=error);
                    if(failure!=null) { completed(failure);yield break; }
                    try
                    {
                        Check(boundaryLoops.Length==2,"Deleted cap fixture should have two open boundaries");
                        boundaryChoice.index=0;PointerProbe.Click(fillBoundaryButton);
                        Check(DisplayedGraphValue().Polygon.Faces.Count==5 && boundaryLoops.Length==1,"First boundary cap differs");
                        string once=workspace.Document.StateHash;
                        PointerProbe.Click(fillBoundaryButton);
                        Check(DisplayedGraphValue().Polygon.Faces.Count==6 && workspace.Evaluate().TriangleCount==12 && boundaryLoops.Length==0 && !fillBoundaryButton.enabledSelf,"Second boundary cap did not close the shape");
                        Check(boundaryHighlight.VertexCount==0,"Closed shape retained a boundary highlight");
                        string closed=workspace.Document.StateHash;
                        Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==once && boundaryLoops.Length==1,"Boundary cap Undo differs");
                        Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==closed,"Boundary cap Redo differs");
                        string directory=Path.Combine(output,"capped-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                        Check(workspace.Document.StateHash==closed,"Boundary cap native reopen differs");
                        Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","closed"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"Boundary cap Bake differs");
                        SelectEditStage(1);controls.ScrollTo(fillBoundaryButton);Frame();orbit=Quaternion.Euler(20,-30,0);UpdateCamera();
                        SetStatus("境界を閉じた形: 6面 / 12三角形。Undo・保存・出力を確認。");
                    }
                    catch(Exception e) { failure=e.ToString(); }
                }
                if(failure==null)
                    yield return WorkbenchCapture.Write(GetComponent<UnityEngine.UIElements.UIDocument>(),camera,Path.Combine(output,"capped.png"),error=>failure=error);
                completed(failure);
            }
            finally { boundaryPanel.value=false;ReplaceWorkspace(previous, previousPath); }
        }
    }
}

