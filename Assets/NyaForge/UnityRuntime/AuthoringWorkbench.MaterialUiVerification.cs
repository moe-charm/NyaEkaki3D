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
        IEnumerator VerifyMaterialUi(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null,paintHash=null,parametersHash=null,savedHash=null;
            ulong materialFace=0;
            string sharedPaint=null,sharedImageHash=null,normalPath=null,mrPath=null;
            IEnumerator Step(VisualElement target,Action action)
            {
                if(failure!=null) yield break;
                try { if(target!=view) controls.ScrollTo(target); } catch(Exception error) { failure=error.ToString(); }
                yield return null;yield return null;
                if(failure==null && target==view) yield return WaitSurfaceReady(error=>failure=error);
                if(failure==null) try { action(); } catch(Exception error) { failure=error.ToString(); }
            }
            try
            {
                byte[] TinyPng(Color32 color)
                {
                    var image = new Texture2D(1, 1, TextureFormat.RGBA32, false, false);
                    try { image.SetPixels32(new[] { color }); image.Apply(false, false); return image.EncodeToPNG(); }
                    finally { UnityEngine.Object.Destroy(image); }
                }
                try { ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();paintPanel.value=true; }
                catch(Exception error) { failure=error.ToString(); }
                yield return Step(addPaint,()=>PointerProbe.Click(addPaint));
                yield return Step(migratePaint,()=> { PointerProbe.Click(migratePaint);paintHash=workspace.Preview.Output.BaseColor.ImageHash;paintPanel.value=false;materialPanel.value=true; });
                yield return Step(addMaterial,()=>
                {
                    string before=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;
                    PointerProbe.Click(addMaterial);
                    Check(workspace.Preview.Output.Material!=null && workspace.Document.DocumentRevision==revision+1,"Material creation did not commit once");
                    Check(workspace.Preview.Output.BaseColor.ImageHash==paintHash,"Material migration changed paint image");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before && workspace.Preview.Output.Material==null,"Material migration Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Material migration Redo differs");
                });
                yield return Step(root.Q<Button>("material-apply"),()=>
                {
                    string before=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;
                    PointerProbe.Click(root.Q<Button>("material-apply"));Check(workspace.Document.DocumentRevision==revision,"Unchanged material created history");
                    materialRoughness.SetValueWithoutNotify(-1);PointerProbe.Click(root.Q<Button>("material-apply"));Check(workspace.Document.StateHash==before,"Invalid material mutated document");
                    materialRoughness.SetValueWithoutNotify(.25f);materialMetallic.SetValueWithoutNotify(.7f);materialTint.SetValueWithoutNotify("#E0B0FF");
                    materialEmission.SetValueWithoutNotify("#301020");materialEmissionStrength.SetValueWithoutNotify(.2f);materialOpacity.SetValueWithoutNotify(.8f);
                    materialAlphaMode.value=materialAlphaMode.choices[1];materialCutoff.SetValueWithoutNotify(.4f);
                    PointerProbe.Click(root.Q<Button>("material-apply"));
                    var parameters=workspace.Preview.Output.Material.Parameters;
                    Check(workspace.Document.DocumentRevision==revision+1 && parameters.AlphaMode==MaterialAlphaMode.Cutout && parameters.Metallic==.7f && parameters.Roughness==.25f,"Material GUI values did not commit");
                    parametersHash=parameters.ContentHash;string after=workspace.Document.StateHash;
                    PointerProbe.Click(root.Q<Button>("material-apply"));Check(workspace.Document.StateHash==after && workspace.Document.DocumentRevision==revision+1,"Displayed hex roundtrip changed authored precision");
                    normalPath=Path.Combine(output,"gui-normal.png");mrPath=Path.Combine(output,"gui-metallic-roughness.png");
                    File.WriteAllBytes(normalPath,TinyPng(new Color32(128,128,255,255)));File.WriteAllBytes(mrPath,TinyPng(new Color32(10,64,200,255)));
                    normalTexturePath.SetValueWithoutNotify(normalPath);normalTextureScale.SetValueWithoutNotify(.7f);semanticTextureTexCoord.index=1;
                    semanticTexturePanel.value=true;
                });
                yield return Step(root.Q<Button>("semantic-normal-apply"),() =>
                {
                    PointerProbe.Click(root.Q<Button>("semantic-normal-apply"));
                    Check(workspace.Preview.Output.Material.Parameters.Textures==null,"GUI accepted an unsupported UV1 semantic texture");
                    semanticTextureTexCoord.index=0;PointerProbe.Click(root.Q<Button>("semantic-normal-apply"));
                    Check(workspace.Preview.Output.Material.Parameters.Textures.Normal!=null && workspace.Preview.Output.Material.Parameters.Textures.Normal.TexCoord==0 && Math.Abs(workspace.Preview.Output.Material.Parameters.Textures.Normal.NormalScale-.7f)<.0001f,"Normal texture GUI import did not preserve supported semantic settings");
                });
                yield return Step(root.Q<Button>("semantic-mr-apply"),() =>
                {
                    metallicRoughnessTexturePath.SetValueWithoutNotify(mrPath);PointerProbe.Click(root.Q<Button>("semantic-mr-apply"));
                    Check(workspace.Preview.Output.Material.Parameters.Textures.MetallicRoughness!=null,"Metallic-roughness texture GUI import did not commit");
                });
                yield return Step(root.Q<Button>("material-apply"),() =>
                {
                    materialMetallic.SetValueWithoutNotify(.6f);PointerProbe.Click(root.Q<Button>("material-apply"));
                    Check(workspace.Preview.Output.Material.Parameters.Textures.Normal!=null && workspace.Preview.Output.Material.Parameters.Textures.MetallicRoughness!=null,"Scalar material edit dropped GUI semantic textures");
                    string semanticProject=Path.Combine(output,"semantic-texture-ui-project");ProjectStore.Save(semanticProject,workspace,0);
                    var semanticReopened=ProjectStore.Open(semanticProject).Document.Objects[0].Graph.Nodes[selectedMaterial].Material;
                    Check(semanticReopened.Textures!=null && semanticReopened.Textures.Normal!=null && semanticReopened.Textures.MetallicRoughness!=null,"Semantic texture GUI Save/Open dropped image slots");
                    var currentParameters=workspace.Preview.Output.Material.Parameters;
                    var scalarOnly=new MaterialParameters(currentParameters.BaseColor,currentParameters.Metallic,currentParameters.Roughness,currentParameters.Emission,currentParameters.AlphaMode,currentParameters.AlphaCutoff);
                    Execute(AuthoringOperation.UpdateNode(GraphNode.StandardMaterial(selectedMaterial,scalarOnly)));
                    Check(workspace.Preview.Output.Material.Parameters.Textures==null,"Semantic texture GUI export fixture cleanup failed");
                    parametersHash=workspace.Preview.Output.Material.Parameters.ContentHash;
                    materialPanel.value=false;paintPanel.value=true;
                });
                yield return Step(surfacePaintMode,()=> { surfacePaintMode.value=true;Check(surfacePaintMode.value && surfacePaintMode.enabledSelf,"Material path disabled 3D paint");Frame(); });
                yield return Step(view,()=>
                {
                    var a=VertexPanelPoint(new Vector3(-.04f,0,0));var b=VertexPanelPoint(new Vector3(.04f,0,0));
                    var mesh=projection.DisplayMesh;long revision=workspace.Document.DocumentRevision;
                    PointerProbe.Down(view,a);PointerProbe.Move(view,b);Check(surfaceStroke!=null && projection.HasPaintPreview,"Material path paint preview missing");PointerProbe.Up(view,b);
                    Check(workspace.Preview.Output.BaseColor.ImageHash!=paintHash && workspace.Document.DocumentRevision==revision+1,"Material path stroke did not commit once");
                    Check(workspace.Preview.Output.Material.Parameters.ContentHash==parametersHash && projection.DisplayMesh==mesh,"Painting replaced material parameters or mesh");
                    Execute(AuthoringOperation.Undo());Check(workspace.Preview.Output.BaseColor.ImageHash==paintHash && workspace.Preview.Output.Material.Parameters.ContentHash==parametersHash,"Material path stroke Undo differs");
                    Execute(AuthoringOperation.Redo());savedHash=workspace.Document.StateHash;
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"material-ui-project"));
                });
                yield return Step(root.Q<Button>("authoring-save"),()=> { PointerProbe.Click(root.Q<Button>("authoring-save"));Check(!workspace.IsDirty,"Material GUI save failed"); });
                yield return Step(root.Q<Button>("authoring-open"),()=>
                {
                    PointerProbe.Click(root.Q<Button>("authoring-open"));Check(workspace.Document.StateHash==savedHash && workspace.Preview.Output.Material.Parameters.ContentHash==parametersHash,"Material native reopen changed parameters or image");
                });
                yield return Step(root.Q<Button>("authoring-export"),()=>
                {
                    PointerProbe.Click(root.Q<Button>("authoring-export"));
                    string manifest=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),"material.nyaforge-bake.json",SearchOption.AllDirectories).Single();
                    var baked=MaterialBakeStore.Read(manifest);
                    Check(baked.Material.ContentHash==parametersHash && baked.Geometry.Mesh.ContentHash==workspace.Preview.Output.Mesh.ContentHash &&
                        baked.BaseColor.CopyRgba().SequenceEqual(workspace.Preview.Output.BaseColor.Image.CopyRgba()),"Material Bake lost parameters, geometry or image");
                    File.WriteAllText(Path.Combine(output,"material-export-manifest.txt"),manifest);
                    // Keep the receiver fixture convention while testing the actual product export button.
                    string fixture=Path.Combine(output,"material-export");Directory.CreateDirectory(fixture);
                    foreach(string file in Directory.GetFiles(Path.GetDirectoryName(manifest),"*",SearchOption.AllDirectories))
                    {
                        string destination=Path.Combine(fixture,file.Substring(Path.GetDirectoryName(manifest).Length+1));
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(file,destination);
                    }
                    surfacePaintMode.value=false;paintPanel.value=false;materialPanel.value=true;Frame();controls.ScrollTo(materialPanel);
                });
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"material-ui.png"),error=>failure=error);
                if(failure==null) try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();
                    Execute(AuthoringOperation.ExtrudePolygonFaces(activeEditContext,new[]{DisplayedGraphValue().Polygon.Faces[0].Id},new Vec3(0,0,.02f)));
                    materialPanel.value=true;
                }
                catch(Exception error) { failure=error.ToString(); }
                yield return Step(addMaterial,()=> { PointerProbe.Click(addMaterial);Check(workspace.Preview.Output.Material!=null && workspace.Preview.Output.BaseColor==null,"Untextured material failed");materialPanel.value=false;paintPanel.value=true; });
                yield return Step(addPaint,()=> { PointerProbe.Click(addPaint);Check(workspace.Preview.IsComplete && workspace.Preview.Output.Material!=null && workspace.Preview.Output.BaseColor!=null,"Adding Paint after material connected downstream of assignment"); });
                yield return Step(migratePaint,()=> { PointerProbe.Click(migratePaint);Check(workspace.Document.Objects[0].Graph.Nodes[selectedPaint].LayerStack!=null,"Slot fixture layer migration failed");materialPanel.value=true;paintPanel.value=false; });
                yield return Step(enableMaterialSlots,()=>
                {
                    materialPanel.value=true;paintPanel.value=false;
                    paintHash=workspace.Preview.Output.BaseColor.ImageHash;
                    PointerProbe.Click(enableMaterialSlots);
                    Check(workspace.Preview.Output.SlotMaterials!=null,"GUI did not enable per-slot materials");
                });
                yield return Step(independentMaterial,()=>
                {
                    int slot=selectedMaterialSlot;string before=workspace.Document.StateHash;
                    string previousId=workspace.Preview.Output.SlotMaterials[slot].MaterialNodeId;
                    PointerProbe.Click(independentMaterial);
                    var binding=workspace.Preview.Output.SlotMaterials[slot];
                    Check(binding.MaterialNodeId!=previousId && binding.MaterialNodeId==selectedMaterial && binding.Material.BaseColor.ImageHash==paintHash,"Independent material GUI lost identity or image");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Independent material Undo failed");
                    Execute(AuthoringOperation.Redo());Check(workspace.Preview.Output.SlotMaterials[slot].MaterialNodeId==binding.MaterialNodeId,"Independent material Redo failed");
                    SelectEditStage(1);faceMode.value=true;selectedFaces.Add(DisplayedGraphValue().Polygon.Faces[0].Id);Refresh();
                });
                yield return Step(splitMaterialFaces,()=>
                {
                    string before=workspace.Document.StateHash;var original=workspace.Preview.Output.SlotMaterials[0];
                    PointerProbe.Click(splitMaterialFaces);
                    Check(workspace.Preview.IsComplete && workspace.Preview.Output.PolygonRendering.MaterialSlotMap.Count==2,"Face split GUI did not create two used slots");
                    Check(selectedMaterialSlot!=0 && workspace.Preview.Output.SlotMaterials[selectedMaterialSlot].MaterialNodeId!=original.MaterialNodeId,"Face split did not create independent material");
                    Check(workspace.Preview.Output.SlotMaterials[selectedMaterialSlot].Material.BaseColor.ImageHash==paintHash,"Face split GUI changed paint pixels");
                    string after=workspace.Document.StateHash;Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Face split Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Face split Redo differs");
                    materialSlotChoice.value=materialSlotChoice.choices[visibleMaterialSlots.IndexOf(0)];
                    materialFace=selectedFaces.Single();materialPanel.value=false;paintPanel.value=true;
                });
                yield return Step(surfacePaintMode,()=> { surfacePaintMode.value=true;Check(surfacePaintMode.enabledSelf && surfacePaintMode.value,"Shared slot Paint is unavailable");Frame(); });
                yield return Step(view,()=>
                {
                    var a=VertexPanelPoint(new Vector3(-.04f,0,.02f));var b=VertexPanelPoint(new Vector3(.04f,0,.02f));
                    var renderer=projection.DisplayObject.transform.Find("Evaluated mesh").GetComponent<MeshRenderer>();
                    var textures=renderer.sharedMaterials.Select(m=>m.mainTexture).ToArray();
                    string before=workspace.Document.StateHash;
                    PointerProbe.Down(view,a);PointerProbe.Move(view,b);
                    Check(surfaceStroke!=null && projection.HasPaintPreview && renderer.sharedMaterials.Where((m,i)=>m.mainTexture!=textures[i]).Count()==2,"Shared stroke did not preview both materials");
                    PointerProbe.Up(view,b);
                    Check(workspace.Document.StateHash!=before && workspace.Preview.Output.SlotMaterials.Values.All(v=>v.Material.BaseColor.ImageHash!=paintHash),"Shared Paint did not commit to both materials");
                    string after=workspace.Document.StateHash;
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Shared Paint Undo differs");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Shared Paint Redo differs");
                    string directory=Path.Combine(output,"shared-slot-paint");ProjectStore.Save(directory,workspace,0);
                    Check(ProjectStore.Open(directory).Document.StateHash==after,"Shared slot Paint native differs");
                    sharedPaint=selectedPaint;sharedImageHash=workspace.Preview.Evaluation.ImageOutputs[selectedPaint].ImageHash;
                    surfacePaintMode.value=false;SelectEditStage(1);faceMode.value=true;selectedFaces.Add(materialFace);
                    materialSlotChoice.value=materialSlotChoice.choices[visibleMaterialSlots.IndexOf(1)];
                    paintPanel.value=true;materialPanel.value=false;Refresh();
                });
                yield return Step(addSlotPaint,()=>
                {
                    PointerProbe.Click(addSlotPaint);
                    Check(selectedPaint!=sharedPaint && OutputSurfaceConnections.Resolve(workspace.Document.Objects[0].Graph,1).ImageNodeId==selectedPaint,"Blank slot Paint GUI did not isolate selected slot");
                    paintPalette.index=(paintPalette.index+1)%paintPalette.choices.Count;
                });
                yield return Step(migratePaint,()=> { PointerProbe.Click(migratePaint); });
                yield return Step(surfacePaintMode,()=> { surfacePaintMode.value=true;Check(surfacePaintMode.enabledSelf,"Independent slot Paint unavailable");Frame(); });
                yield return Step(view,()=>
                {
                    var a=VertexPanelPoint(new Vector3(-.04f,0,.02f));var b=VertexPanelPoint(new Vector3(.04f,0,.02f));
                    string hash=workspace.Preview.Evaluation.ImageOutputs[selectedPaint].ImageHash;
                    PointerProbe.Down(view,a);PointerProbe.Move(view,b);PointerProbe.Up(view,b);
                    Check(workspace.Preview.Evaluation.ImageOutputs[selectedPaint].ImageHash!=hash,"Independent slot Paint did not paint");
                    Check(workspace.Preview.Evaluation.ImageOutputs[sharedPaint].ImageHash==sharedImageHash,"Independent paint changed the old shared image");
                    Check(workspace.Preview.Evaluation.ImageOutputs[selectedPaint].ImageHash!=sharedImageHash,"Independent fixture must have distinct image pixels");
                    paintStage.value=paintStage.choices[paintIds.IndexOf(sharedPaint)];surfacePaintMode.value=true;
                });
                yield return Step(view,()=>
                {
                    var a=VertexPanelPoint(new Vector3(-.04f,0,.02f));var b=VertexPanelPoint(new Vector3(.04f,0,.02f));string state=workspace.Document.StateHash;
                    PointerProbe.Down(view,a);PointerProbe.Move(view,b);Check(!projection.HasPaintPreview,"Another Paint's face received a preview");PointerProbe.Up(view,b);
                    Check(workspace.Document.StateHash==state,"Painting an excluded front face changed the document");
                    surfacePaintMode.value=false;SelectEditStage(1);faceMode.value=true;selectedFaces.Add(materialFace);
                    materialSlotChoice.value=materialSlotChoice.choices[visibleMaterialSlots.IndexOf(0)];
                    paintPanel.value=false;materialPanel.value=true;Refresh();
                });
                yield return Step(root.Q<Button>("authoring-export"),()=>
                {
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"multi-material-ui-project"));
                    string materialId=workspace.Preview.Output.SlotMaterials[1].MaterialNodeId;
                    Execute(AuthoringOperation.UpdateNode(GraphNode.StandardMaterial(materialId,new MaterialParameters(new Vec4(.2f,.7f,1,1),.4f,.3f,new Vec3(.02f,.03f,.08f)))));
                    PointerProbe.Click(root.Q<Button>("authoring-export"));
                    string manifest=Directory.GetFiles(Path.Combine(projectPath.value,"exports"),MultiMaterialBakeStore.ManifestName,SearchOption.AllDirectories).Single();
                    var baked=MultiMaterialBakeStore.Read(manifest);
                    Check(baked.SubmeshSlots.Count==2 && baked.Geometry.MeshContentHash==workspace.Preview.Output.Mesh.ContentHash,"Multi-material GUI export lost submeshes");
                    foreach(var slot in baked.Slots)
                    {
                        var expected=workspace.Preview.Output.SlotMaterials[slot.Slot];
                        Check(slot.MaterialNodeId==expected.MaterialNodeId && slot.Surface.Material.ContentHash==expected.Material.Parameters.ContentHash && slot.Surface.BaseColor.CopyRgba().SequenceEqual(expected.Material.BaseColor.Image.CopyRgba()),"Multi-material GUI export changed slot appearance");
                    }
                    string fixture=Path.Combine(output,"multi-material-export");Directory.CreateDirectory(fixture);
                    foreach(string file in Directory.GetFiles(Path.GetDirectoryName(manifest),"*",SearchOption.AllDirectories))
                    {
                        string destination=Path.Combine(fixture,file.Substring(Path.GetDirectoryName(manifest).Length+1));
                        Directory.CreateDirectory(Path.GetDirectoryName(destination));File.Copy(file,destination);
                    }
                });
                yield return Step(moveMaterialFaces,()=>
                {
                    string material=workspace.Preview.Output.SlotMaterials[0].MaterialNodeId;
                    PointerProbe.Click(moveMaterialFaces);
                    Check(workspace.Preview.IsComplete && workspace.Preview.Output.PolygonRendering.MaterialSlotMap.SequenceEqual(new[]{0}),"Moving faces back did not restore one used slot");
                    Check(workspace.Preview.Output.SlotMaterials[0].MaterialNodeId==material,"Moving faces changed target material");
                });
                completed(failure);
            }
            finally { surfacePaintMode.SetValueWithoutNotify(false);CancelSurfaceStroke();materialPanel.value=false;paintPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
