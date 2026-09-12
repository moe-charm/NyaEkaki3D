using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyMaskUi(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null,before=null,after=null;byte[] pixels=null;
            Button Control(string name)=>root.Q<Button>(name);
            byte Center()=>CurrentPaintLayer().Mask.GetCoverage(CurrentPaintLayer().Image.Width/2,CurrentPaintLayer().Image.Height/2);
            byte DisplayCenter()
            {
                var texture=(Texture2D)paintCanvas.Q<Image>().image;
                return texture.GetRawTextureData<byte>()[(texture.height/2*texture.width+texture.width/2)*4];
            }
            IEnumerator Step(VisualElement target,Action action)
            {
                if(failure!=null) yield break;
                try { controls.ScrollTo(target); } catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                if(failure==null) try { action(); } catch(Exception e) { failure=e.ToString(); }
            }
            try
            {
                try { ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();paintPanel.value=true; }
                catch(Exception e) { failure=e.ToString(); }
                yield return Step(addPaint,()=>PointerProbe.Click(addPaint));
                yield return Step(migratePaint,()=>PointerProbe.Click(migratePaint));
                yield return Step(Control("paint-layer-add"),()=>PointerProbe.Click(Control("paint-layer-add")));
                yield return Step(paintCanvas,()=>
                {
                    paintRadius.SetValueWithoutNotify(512);paintOpacity.SetValueWithoutNotify(1);paintPalette.SetValueWithoutNotify(paintPalette.choices[0]);
                    PointerProbe.Click(paintCanvas);pixels=CurrentPaintLayer().Image.CopyRgba();layerDetails.value=true;
                });
                yield return Step(Control("paint-layer-rename"),()=>
                {
                    before=workspace.Document.StateHash;layerName.SetValueWithoutNotify("マスク付き模様");PointerProbe.Click(Control("paint-layer-rename"));
                    Check(CurrentPaintLayer().Name=="マスク付き模様","Layer rename GUI failed");after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Rename Undo failed");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Rename Redo failed");
                });
                yield return Step(Control("paint-mask-add"),()=>
                {
                    PointerProbe.Click(Control("paint-mask-add"));Check(EditingMask && Center()==255,"Mask add did not select white mask");
                    Check(!Control("paint-mask-add").enabledInHierarchy && Control("paint-mask-remove").enabledInHierarchy,"Mask controls incorrect");
                });
                yield return Step(paintCanvas,()=>
                {
                    paintRadius.SetValueWithoutNotify(36);paintOpacity.SetValueWithoutNotify(.5f);maskBrush.SetValueWithoutNotify(maskBrush.choices[0]);
                    before=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;
                    PointerProbe.Down(paintCanvas,paintCanvas.worldBound.center);
                    Check(paintCanvas.IsDrawing && workspace.Document.StateHash==before && Center()==255 && DisplayCenter()==128,"Mask preview changed document or used wrong blend");
                    PointerProbe.Up(paintCanvas,paintCanvas.worldBound.center);after=workspace.Document.StateHash;
                    Check(workspace.Document.DocumentRevision==revision+1 && Center()==128 && DisplayCenter()==128,"Mask commit differs from preview");
                    Check(CurrentPaintLayer().Image.CopyRgba().SequenceEqual(pixels),"Mask stroke changed color pixels");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before && Center()==255,"Mask stroke Undo failed");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after && Center()==128,"Mask stroke Redo failed");
                    PointerProbe.Down(paintCanvas,paintCanvas.worldBound.center);
                    using(var key=KeyDownEvent.GetPooled(new Event { type=EventType.KeyDown,keyCode=KeyCode.Escape })) paintCanvas.SendEvent(key);
                    Check(!paintCanvas.IsDrawing && workspace.Document.StateHash==after && DisplayCenter()==128,"Mask Escape did not restore preview");
                    PointerProbe.Down(paintCanvas,paintCanvas.worldBound.center);paintTarget.value=paintTarget.choices[0];
                    Check(!paintCanvas.IsDrawing && workspace.Document.StateHash==after && !EditingMask,"Target change committed pending mask");
                    paintTarget.value=paintTarget.choices[1];
                    maskBrush.value=maskBrush.choices[1];paintOpacity.SetValueWithoutNotify(1);PointerProbe.Click(paintCanvas);
                    Check(Center()==255,"White mask brush failed");Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==after,"White brush Undo failed");
                });
                yield return Step(Control("paint-mask-remove"),()=>
                {
                    PointerProbe.Click(Control("paint-mask-remove"));Check(CurrentPaintLayer().Mask==null && !EditingMask,"Mask removal failed");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==after && Center()==128,"Mask removal Undo lost data");
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"mask-ui-project"));
                });
                yield return Step(Control("authoring-save"),()=> { PointerProbe.Click(Control("authoring-save"));Check(!workspace.IsDirty,"Mask save failed"); });
                yield return Step(Control("authoring-open"),()=>
                {
                    PointerProbe.Click(Control("authoring-open"));paintLayerChoice.value=paintLayerChoice.choices[1];
                    Check(workspace.Document.StateHash==after && Center()==128 && CurrentPaintLayer().Name=="マスク付き模様","Mask native reopen differs");
                });
                yield return Step(exportPaintPng,()=>
                {
                    PointerProbe.Click(exportPaintPng);string file=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),"*.png",SearchOption.AllDirectories).Single();
                    Check(File.ReadAllBytes(file).SequenceEqual(PaintPng.Encode(workspace.Preview.Output.BaseColor.Image)),"Masked PNG differs");
                    var surface=SurfaceBakeStore.Read(SurfaceBakeStore.Export(Path.Combine(output,"mask-surface"),workspace));
                    Check(surface.BaseColor.CopyRgba().SequenceEqual(workspace.Preview.Output.BaseColor.Image.CopyRgba()),"Masked Surface differs");
                    layerDetails.value=false;paintTarget.value=paintTarget.choices[1];Frame();controls.ScrollTo(paintCanvas);
                });
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"mask-ui.png"),error=>failure=error);
                completed(failure);
            }
            finally { layerDetails.value=false;paintPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
