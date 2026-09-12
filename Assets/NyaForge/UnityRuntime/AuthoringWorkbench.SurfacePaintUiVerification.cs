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
        IEnumerator VerifySurfacePaintUi(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null,before=null,after=null;byte[] colorPixels=null;
            Vector2 A()=>VertexPanelPoint(new Vector3(-.04f,0,0));Vector2 B()=>VertexPanelPoint(new Vector3(.04f,0,0));
            IEnumerator Step(VisualElement element,Action action)
            {
                if(failure!=null) yield break;
                try { if(element!=view) controls.ScrollTo(element); } catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                if(failure==null && element==view) yield return WaitSurfaceReady(error=>failure=error);
                if(failure==null) try { action(); } catch(Exception e) { failure=e.ToString(); }
            }
            try
            {
                try { ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();paintPanel.value=true; }
                catch(Exception e) { failure=e.ToString(); }
                yield return Step(addPaint,()=>PointerProbe.Click(addPaint));
                yield return Step(migratePaint,()=>PointerProbe.Click(migratePaint));
                yield return Step(root.Q<Button>("paint-layer-add"),()=>PointerProbe.Click(root.Q<Button>("paint-layer-add")));
                yield return Step(surfacePaintMode,()=> { PointerProbe.Click(surfacePaintMode);Check(surfacePaintMode.value,"3D paint mode failed");Frame(); });
                yield return Step(view,()=>
                {
                    paintRadius.SetValueWithoutNotify(10);paintOpacity.SetValueWithoutNotify(1);paintPalette.SetValueWithoutNotify(paintPalette.choices[0]);
                    before=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;var orientation=orbit;
                    PointerProbe.Down(view,A());PointerProbe.Move(view,B());
                    Check(surfaceStroke!=null && surfaceStroke.PointCount>1 && projection.HasPaintPreview,"3D temporary preview missing");
                    Check(workspace.Document.StateHash==before && workspace.Document.DocumentRevision==revision && orbit==orientation,"3D pending stroke modified document or camera");
                    PointerProbe.Up(view,B());after=workspace.Document.StateHash;
                    Check(after!=before && workspace.Document.DocumentRevision==revision+1 && !projection.HasPaintPreview,"3D stroke did not commit once");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"3D stroke Undo failed");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"3D stroke Redo failed");
                });
                yield return Step(view,()=>
                {
                    PointerProbe.Down(view,A());Check(surfaceStroke!=null,"3D cancel stroke did not start");
                    using(var key=KeyDownEvent.GetPooled(new Event { type=EventType.KeyDown,keyCode=KeyCode.Escape })) view.SendEvent(key);
                    Check(surfaceStroke==null && !projection.HasPaintPreview && workspace.Document.StateHash==after,"3D Escape failed");
                    PointerProbe.Down(view,A());
                });
                yield return null;
                if(failure==null) try { view.ReleasePointer(PointerId.mousePointerId); } catch(Exception e) { failure=e.ToString(); }
                yield return new WaitForSecondsRealtime(.05f);
                if(failure==null) try { Check(surfaceStroke==null && !projection.HasPaintPreview && workspace.Document.StateHash==after,"3D capture loss persisted paint");layerDetails.value=true; } catch(Exception e) { failure=e.ToString(); }
                yield return Step(root.Q<Button>("paint-mask-add"),()=> { PointerProbe.Click(root.Q<Button>("paint-mask-add"));colorPixels=CurrentPaintLayer().Image.CopyRgba(); });
                yield return Step(view,()=>
                {
                    before=workspace.Document.StateHash;paintOpacity.SetValueWithoutNotify(.5f);maskBrush.SetValueWithoutNotify(maskBrush.choices[0]);
                    PointerProbe.Down(view,A());PointerProbe.Move(view,B());PointerProbe.Up(view,B());after=workspace.Document.StateHash;
                    Check(after!=before && CurrentPaintLayer().Mask.CopyCoverage().Any(v=>v<255),"3D mask stroke missing");
                    Check(CurrentPaintLayer().Image.CopyRgba().SequenceEqual(colorPixels),"3D mask changed color pixels");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"3D mask Undo failed");Execute(AuthoringOperation.Redo());
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"surface-paint-project"));
                });
                yield return Step(root.Q<Button>("authoring-save"),()=> { PointerProbe.Click(root.Q<Button>("authoring-save"));Check(!workspace.IsDirty,"3D paint save failed"); });
                if(failure==null) yield return VerifySurfaceLifetimeSteps(output,error=>failure=error);
                if(failure==null) yield return VerifySurfaceViewport(error=>failure=error);
                if(failure==null) yield return VerifySurfaceSteps(VerifySurfacePreparationRaces(),error=>failure=error);
                yield return Step(root.Q<Button>("authoring-open"),()=>
                {
                    PointerProbe.Click(root.Q<Button>("authoring-open"));Check(workspace.Document.StateHash==after,"3D paint reopen differs");
                    var bake=SurfaceBakeStore.Read(SurfaceBakeStore.Export(Path.Combine(output,"surface-paint-export"),workspace));
                    Check(bake.BaseColor.CopyRgba().SequenceEqual(workspace.Preview.Output.BaseColor.Image.CopyRgba()),"3D paint Surface differs");
                    layerDetails.value=false;Frame();controls.ScrollTo(surfacePaintMode);
                });
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"surface-paint.png"),error=>failure=error);
                completed(failure);
            }
            finally { surfacePaintMode.SetValueWithoutNotify(false);CancelSurfaceStroke();paintPanel.value=false;layerDetails.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
