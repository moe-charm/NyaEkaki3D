using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyImageImport(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null,before=null,after=null;PaintImage source=null;
            Button Apply()=>root.Q<Button>("paint-import-apply");
            IEnumerator Step(VisualElement target,Action action)
            {
                if(failure!=null) yield break;
                try { controls.ScrollTo(target); } catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                if(failure==null) try { action(); } catch(Exception e) { failure=e.ToString(); }
            }
            try
            {
                try
                {
                    PngVariantVerification.Verify(output);
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();paintPanel.value=true;
                    source=PaintImage.FromRgbaBottomLeft(2,2,new byte[]{255,0,0,255,0,255,0,128,0,0,255,0,255,255,255,255});
                    string input=Path.Combine(output,"import-colors.png");File.WriteAllBytes(input,PaintPng.Encode(source));
                    var decoded=PaintPngImporter.Read(input);Check(decoded.CopyRgba().SequenceEqual(source.CopyRgba()),"PNG decode changed orientation, alpha or RGB bytes");
                    paintImportPath.SetValueWithoutNotify(input);
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return Step(addPaint,()=>PointerProbe.Click(addPaint));
                yield return Step(migratePaint,()=> { PointerProbe.Click(migratePaint);paintImportPanel.value=true; });
                yield return Step(Apply(),()=>
                {
                    before=workspace.Document.StateHash;fitImportedImage.SetValueWithoutNotify(false);PointerProbe.Click(Apply());
                    Check(workspace.Document.StateHash==before,"Mismatched import changed document");
                    string valid=paintImportPath.value;paintImportPath.SetValueWithoutNotify(Path.Combine(output,"missing.png"));PointerProbe.Click(Apply());
                    Check(workspace.Document.StateHash==before,"Missing image changed document");
                    string bad=Path.Combine(output,"broken.png");File.WriteAllBytes(bad,new byte[]{1,2,3});paintImportPath.SetValueWithoutNotify(bad);PointerProbe.Click(Apply());
                    Check(workspace.Document.StateHash==before,"Corrupt image changed document");
                    paintImportPath.SetValueWithoutNotify(valid);fitImportedImage.SetValueWithoutNotify(true);long revision=workspace.Document.DocumentRevision;
                    PointerProbe.Click(Apply());after=workspace.Document.StateHash;
                    Check(after!=before && workspace.Document.DocumentRevision==revision+1,"Image import is not one command");
                    var node=workspace.Document.Objects[0].Graph.Nodes[selectedPaint];
                    Check(node.LayerStack.Layers.Count==2 && selectedPaintLayer==node.LayerStack.Layers[1].Id,"Import did not create/select a new layer");
                    Check(CurrentPaintLayer().Image.CopyRgba().SequenceEqual(PaintImageFit.Apply(source,node.PaintWidth,node.PaintHeight).CopyRgba()),"Imported fitted pixels differ");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Import Undo failed");Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Import Redo failed");
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"image-import-project"));
                });
                yield return Step(root.Q<Button>("authoring-save"),()=>
                {
                    PointerProbe.Click(root.Q<Button>("authoring-save"));Check(!workspace.IsDirty,"Imported image save failed");
                    // Overwrite only this test's source fixture: the saved document must own its image data.
                    File.WriteAllBytes(paintImportPath.value,new byte[]{0});
                });
                yield return Step(root.Q<Button>("authoring-open"),()=>
                {
                    PointerProbe.Click(root.Q<Button>("authoring-open"));Check(workspace.Document.StateHash==after,"Reopen depends on external image source");
                    var surface=SurfaceBakeStore.Read(SurfaceBakeStore.Export(Path.Combine(output,"import-surface"),workspace));
                    Check(surface.BaseColor.CopyRgba().SequenceEqual(workspace.Preview.Output.BaseColor.Image.CopyRgba()),"Imported Surface differs");
                    Frame();controls.ScrollTo(paintImportPanel);
                });
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"image-import.png"),error=>failure=error);
                completed(failure);
            }
            finally { paintImportPanel.value=false;paintPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
