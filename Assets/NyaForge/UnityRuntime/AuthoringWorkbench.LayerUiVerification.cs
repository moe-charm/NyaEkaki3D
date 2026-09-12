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
        IEnumerator VerifyLayerUi(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null,before=null,after=null,background=null,top=null;
            PaintLayers Stack()=>workspace.Document.Objects[0].Graph.Nodes[selectedPaint].LayerStack;
            Button Control(string name)=>root.Q<Button>(name);
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
                yield return Step(addPaint,()=> { PointerProbe.Click(addPaint);before=workspace.Document.StateHash; });
                yield return Step(migratePaint,()=>
                {
                    PointerProbe.Click(migratePaint);Check(Stack().Layers.Count==1,"GUI migration failed");
                    after=workspace.Document.StateHash;Execute(AuthoringOperation.Undo());
                    Check(workspace.Document.StateHash==before,"Migration Undo differs");Execute(AuthoringOperation.Redo());
                    Check(workspace.Document.StateHash==after,"Migration Redo differs");
                    background=Convert.ToBase64String(Stack().Layers[0].Image.CopyRgba());
                });
                yield return Step(Control("paint-layer-add"),()=>
                {
                    PointerProbe.Click(Control("paint-layer-add"));
                    Check(Stack().Layers.Count==2 && selectedPaintLayer==Stack().Layers[1].Id,"GUI add did not select new layer");top=selectedPaintLayer;
                });
                yield return Step(paintCanvas,()=>
                {
                    paintPalette.SetValueWithoutNotify(paintPalette.choices[0]);paintRadius.SetValueWithoutNotify(40);paintOpacity.SetValueWithoutNotify(1);
                    before=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;
                    PointerProbe.Click(paintCanvas);after=workspace.Document.StateHash;
                    Check(after!=before && workspace.Document.DocumentRevision==revision+1,"Layer stroke did not commit once");
                    Check(Convert.ToBase64String(Stack().Layers[0].Image.CopyRgba())==background,"Stroke changed background layer");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Layer stroke Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Layer stroke Redo differs");
                });
                yield return Step(Control("paint-layer-appearance"),()=>
                {
                    layerOpacity.SetValueWithoutNotify(.5f);layerVisible.SetValueWithoutNotify(false);PointerProbe.Click(Control("paint-layer-appearance"));
                    Check(!Stack().Layers[1].Visible && Stack().Layers[1].Opacity==.5f,"Layer appearance GUI failed");
                    Check(Convert.ToBase64String(workspace.Preview.Output.BaseColor.Image.CopyRgba())==background,"Hidden layer affected composite");
                    Execute(AuthoringOperation.Undo());
                });
                yield return Step(Control("paint-layer-down"),()=> { PointerProbe.Click(Control("paint-layer-down"));Check(Stack().Layers[0].Id==top,"Layer reorder failed");Execute(AuthoringOperation.Undo()); });
                yield return Step(Control("paint-layer-remove"),()=>
                {
                    PointerProbe.Click(Control("paint-layer-remove"));Check(Stack().Layers.Count==1,"Layer remove failed");
                    PointerProbe.Click(Control("paint-layer-remove"));Check(Stack().Layers.Count==0,"Last layer remove failed");
                    Check(!paintCanvas.enabledInHierarchy && !Control("paint-layer-remove").enabledInHierarchy &&
                        !Control("paint-layer-appearance").enabledInHierarchy && Control("paint-layer-add").enabledInHierarchy,"Empty stack controls invalid");
                    Execute(AuthoringOperation.Undo());Execute(AuthoringOperation.Undo());
                    Check(workspace.Document.StateHash==after,"Remove Undo lost layers");
                    paintLayerChoice.value=paintLayerChoice.choices[1];Check(selectedPaintLayer==top,"Layer selection failed");
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"layer-ui-project"));
                });
                yield return Step(Control("authoring-save"),()=> { PointerProbe.Click(Control("authoring-save"));Check(!workspace.IsDirty,"Layer GUI save failed"); });
                yield return Step(Control("authoring-open"),()=> { PointerProbe.Click(Control("authoring-open"));Check(workspace.Document.StateHash==after && Stack().Layers.Count==2,"Layer GUI reopen differs"); });
                yield return Step(exportPaintPng,()=>
                {
                    PointerProbe.Click(exportPaintPng);
                    string png=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),"*.png",SearchOption.AllDirectories).Single();
                    Check(File.ReadAllBytes(png).SequenceEqual(PaintPng.Encode(Stack().Composite())),"Layer GUI PNG is not composite");
                });
                yield return Step(Control("authoring-export"),()=>
                {
                    PointerProbe.Click(Control("authoring-export"));
                    string manifest=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),SurfaceBakeStore.ManifestName,SearchOption.AllDirectories).Single();
                    Check(SurfaceBakeStore.Read(manifest).BaseColor.CopyRgba().SequenceEqual(Stack().Composite().CopyRgba()),"Layer GUI Surface differs");
                    paintLayerChoice.value=paintLayerChoice.choices[1];Frame();controls.ScrollTo(layerPanel);
                });
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"layer-ui.png"),error=>failure=error);
                completed(failure);
            }
            finally { paintPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
