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
        // Public synthetic diamond plate: modeling, UV, paint, save and surface export through GUI actions.
        IEnumerator VerifyItemWorkflow(string output,Action<string> completed)
        {
            var previous = workspace; string previousPath = savedDirectory, failure = null;
            string expectedState = null, meshHash = null, imageHash = null;
            IEnumerator Step(VisualElement target,Action action)
            {
                if (failure != null) yield break;
                try { controls.ScrollTo(target); } catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                if (failure == null) try { action(); } catch (Exception e) { failure = e.ToString(); }
            }
            try
            {
                try { ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null); }
                catch (Exception e) { failure = e.ToString(); }
                var create = root.Q<Button>("graph-create-polygon");
                yield return Step(create,()=>PointerProbe.Click(create));
                var deltas = new[]{new Vector2(100,-20),new Vector2(-50,50),new Vector2(-100,20),new Vector2(50,-50)};
                for (int i = 0; i < deltas.Length; i++)
                {
                    int index = i;
                    yield return Step(moveButton,()=>
                    {
                        var map = DisplayedGraphValue().PolygonRendering.RenderVertexMap;
                        int renderIndex = Enumerable.Range(0,map.Count).First(i=>map[i].VertexId == (ulong)index+1);
                        Select(new[]{renderIndex}); moveX.SetValueWithoutNotify(deltas[index].x);
                        moveY.SetValueWithoutNotify(deltas[index].y); moveZ.SetValueWithoutNotify(0);
                        PointerProbe.Click(moveButton);
                    });
                }
                yield return Step(solidifyButton,()=>
                {
                    shellThickness.SetValueWithoutNotify(4); PointerProbe.Click(solidifyButton);
                    Check(workspace.Evaluate().TriangleCount == 12,"Item shell topology differs");
                    uvPanel.value = true;
                });
                yield return Step(uvProject,()=> { PointerProbe.Click(uvProject); paintPanel.value = true; });
                yield return Step(addPaint,()=>PointerProbe.Click(addPaint));
                yield return Step(paintCanvas,()=>
                {
                    paintPalette.SetValueWithoutNotify(paintPalette.choices[2]);
                    paintRadius.SetValueWithoutNotify(512); paintOpacity.SetValueWithoutNotify(1);
                    PointerProbe.Click(paintCanvas);
                    paintPalette.SetValueWithoutNotify(paintPalette.choices[0]); paintRadius.SetValueWithoutNotify(10);
                    var a = paintCanvas.LocalToWorld(new Vector2(paintCanvas.contentRect.width*.05f,paintCanvas.contentRect.height*(5f/6)));
                    var b = paintCanvas.LocalToWorld(new Vector2(paintCanvas.contentRect.width*.28f,paintCanvas.contentRect.height*(5f/6)));
                    PointerProbe.Down(paintCanvas,a); PointerProbe.Move(paintCanvas,b); PointerProbe.Up(paintCanvas,b);
                    Check(workspace.Preview.IsComplete,"Item paint did not resolve");
                    var positions = workspace.Evaluate().Positions;
                    Check(Math.Abs(positions.Max(p=>p.X)-positions.Min(p=>p.X)-.1f)<.00001f &&
                        Math.Abs(positions.Max(p=>p.Y)-positions.Min(p=>p.Y)-.14f)<.00001f &&
                        Math.Abs(positions.Max(p=>p.Z)-positions.Min(p=>p.Z)-.004f)<.00001f,"Item dimensions differ");
                    expectedState = workspace.Document.StateHash; meshHash = workspace.Evaluate().ContentHash;
                    imageHash = workspace.Preview.Output.BaseColor.ImageHash;
                    projectPath.SetValueWithoutNotify(Path.Combine(output,"item-project"));
                });
                var save = root.Q<Button>("authoring-save");
                yield return Step(save,()=> { PointerProbe.Click(save); Check(!workspace.IsDirty,"Item save failed"); });
                var open = root.Q<Button>("authoring-open");
                yield return Step(open,()=> { PointerProbe.Click(open); Check(workspace.Document.StateHash == expectedState,"Item reopen differs"); });
                var export = root.Q<Button>("authoring-export");
                yield return Step(export,()=>
                {
                    PointerProbe.Click(export);
                    string manifest = Directory.GetFiles(Path.Combine(projectPath.value,"exports"),SurfaceBakeStore.ManifestName,SearchOption.AllDirectories).Single();
                    var surface = SurfaceBakeStore.Read(manifest);
                    Check(surface.Geometry.MeshContentHash == meshHash && surface.BaseColor.CopyRgba().SequenceEqual(workspace.Preview.Output.BaseColor.Image.CopyRgba()),"Item surface export differs");
                    Check(workspace.Preview.Output.BaseColor.ImageHash == imageHash && workspace.Document.StateHash == expectedState,"Item export changed source");
                    Frame(); orbit = Quaternion.Euler(15,-25,0); UpdateCamera();
                    SetStatus("小物のGUI往復: 100×140×4mm / 12三角形。形状・UV・色・保存・Surface出力を確認。");
                });
                if (failure == null)
                    yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"item-workflow.png"),error=>failure=error);
                completed(failure);
            }
            finally { uvPanel.value = false; paintPanel.value = false; ReplaceWorkspace(previous,previousPath); }
        }
    }
}


