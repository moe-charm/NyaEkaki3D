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
        IEnumerator VerifyPaintUi(string output, Action<string> completed)
        {
            var previous = workspace; string previousPath = savedDirectory, failure = null, after = null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null); CreatePolygonGraph();
                    paintPanel.value = true; controls.ScrollTo(addPaint);
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                if (failure == null) try
                {
                    PointerProbe.Click(addPaint);
                    Check(workspace.Preview.Output.BaseColor != null,"Paint GUI attachment failed");
                    paintPalette.SetValueWithoutNotify(paintPalette.choices[0]); paintRadius.SetValueWithoutNotify(12); paintOpacity.SetValueWithoutNotify(1);
                    controls.ScrollTo(paintCanvas);
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                if (failure == null) try
                {
                    Vector2 Point(float u,float v) => paintCanvas.LocalToWorld(new Vector2(u*paintCanvas.contentRect.width,(1-v)*paintCanvas.contentRect.height));
                    string before = workspace.Document.StateHash; long revision = workspace.Document.DocumentRevision;
                    PointerProbe.Down(paintCanvas,Point(.2f,.25f)); PointerProbe.Move(paintCanvas,Point(.8f,.75f));
                    Check(paintCanvas.IsDrawing && workspace.Document.StateHash == before && workspace.Document.DocumentRevision == revision,"Uncommitted paint changed the document");
                    PointerProbe.Up(paintCanvas,Point(.8f,.75f));
                    after = workspace.Document.StateHash;
                    Check(!paintCanvas.IsDrawing && after != before && workspace.Document.DocumentRevision == revision+1,"Stroke was not one command");
                    Execute(AuthoringOperation.Undo()); Check(workspace.Document.StateHash == before,"Paint GUI Undo failed");
                    Execute(AuthoringOperation.Redo()); Check(workspace.Document.StateHash == after,"Paint GUI Redo failed");
                    PointerProbe.Down(paintCanvas,Point(.3f,.2f)); PointerProbe.Move(paintCanvas,Point(.6f,.8f));
                    paintCanvas.CancelStroke(); Check(!paintCanvas.IsDrawing && workspace.Document.StateHash == after,"Cancelled paint persisted");
                    PointerProbe.Down(paintCanvas,Point(.4f,.4f));
                    Check(paintCanvas.IsDrawing,"Capture-loss stroke did not start");
                }
                catch (Exception e) { failure = e.ToString(); }
                // Capture transitions are processed by the panel, not synchronously by ReleasePointer.
                yield return null;
                if (failure == null) try
                {
                    Check(paintCanvas.HasPointerCapture(PointerId.mousePointerId),"Paint did not capture the pointer");
                    paintCanvas.ReleasePointer(PointerId.mousePointerId);
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return new WaitForSecondsRealtime(.05f);
                if (failure == null) try
                {
                    Check(!paintCanvas.IsDrawing,"Capture loss did not cancel the pending stroke");
                    Check(workspace.Document.StateHash == after,"Capture loss changed the document");
                    PointerProbe.Down(paintCanvas,paintCanvas.worldBound.center);
                    Check(paintCanvas.IsDrawing,"Escape stroke did not start");
                    using (var key = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape })) paintCanvas.SendEvent(key);
                    Check(!paintCanvas.IsDrawing && workspace.Document.StateHash == after,"Escape did not discard the stroke");
                    string dir = Path.Combine(output,"paint-ui-project"); projectPath.SetValueWithoutNotify(dir); SaveProject(); OpenProject();
                    Check(workspace.Document.StateHash == after && !workspace.IsDirty,"Paint UI save/reopen differs");
                    Frame(); controls.ScrollTo(paintCanvas);
                    SetStatus("ブラシGUI: 描きかけは未保存、離すと1 Undo。取消・保存・再読込を確認。");
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure == null)
                    yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"paint-ui.png"),error=>failure=error);
                if (failure == null) try
                {
                    string beforeWarning = workspace.Document.StateHash;
                    SelectEditStage(1); uvPanel.value = true; Refresh();
                    Check(uvPaintDependencies.style.display.value == DisplayStyle.Flex && uvPaintDependencies.text.Contains(selectedPaint.Substring(0,8)),"UV editor omitted retained Paint dependency");
                    Check(workspace.Document.StateHash == beforeWarning,"Dependency display modified the document");
                    controls.ScrollTo(uvPaintDependencies);
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure == null)
                    yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"uv-paint-dependencies.png"),error=>failure=error);
                if (failure == null) { controls.ScrollTo(exportPaintPng); }
                yield return null; yield return null;
                if (failure == null) try
                {
                    string beforeExport = workspace.Document.StateHash;
                    PointerProbe.Click(exportPaintPng);
                    string folder = Path.Combine(projectPath.value,"exports");
                    string png = Directory.GetFiles(folder,"*.png",SearchOption.AllDirectories).Single();
                    var expected = NyaForge.Authoring.Paint.PaintPng.Encode(workspace.Preview.Evaluation.ImageOutputs[selectedPaint].Image);
                    Check(File.ReadAllBytes(png).SequenceEqual(expected) && workspace.Document.StateHash == beforeExport,"GUI PNG export differs from committed image");
                }
                catch (Exception e) { failure = e.ToString(); }
                completed(failure);
            }
            finally { uvPanel.value = false; paintPanel.value = false; ReplaceWorkspace(previous,previousPath); }
        }
    }
}
