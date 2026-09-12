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
        IEnumerator VerifyUv(string output, Action<string> completed)
        {
            var previous = workspace;
            string path = savedDirectory, failure = null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                    CreatePolygonGraph();
                    Execute(AuthoringOperation.SolidifyPolygon(activeEditContext, .02f));
                    uvPanel.value = true; controls.ScrollTo(uvProject);
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                controls.ScrollTo(uvProject);
                yield return null; yield return null;
                if (failure == null)
                    try
                    {
                        string original = workspace.Document.StateHash;
                        PointerProbe.Click(uvProject);
                        string state = workspace.Document.StateHash;
                        Check(state != original && uvPreview.VisibleFaceCount == 6, "UV projection or preview failed");
                        Check(workspace.Preview.Output.Polygon.Faces.SelectMany(f => f.Corners).All(c => c.Uv0.HasValue && c.Uv0.Value.X > 0 && c.Uv0.Value.X < 1 && c.Uv0.Value.Y > 0 && c.Uv0.Value.Y < 1), "UVs outside packed tile");
                        Execute(AuthoringOperation.Undo()); Check(workspace.Document.StateHash == original, "UV Undo failed");
                        Execute(AuthoringOperation.Redo()); Check(workspace.Document.StateHash == state, "UV Redo failed");
                        string dir = Path.Combine(output, "uv-project"); projectPath.SetValueWithoutNotify(dir); SaveProject(); OpenProject();
                        Check(workspace.Document.StateHash == state, "UV native reopen changed document");
                        var bake = BakeStore.Read(BakeStore.Export(Path.Combine(dir, "exports", "uv"), workspace));
                        Check(bake.Mesh.ContentHash == workspace.Evaluate().ContentHash, "UV Bake differs");
                        SelectEditStage(1); faceMode.value = true; selectedFaces.Add(1); Refresh();
                        Frame(); orbit = Quaternion.Euler(20, -30, 0); UpdateCamera(); controls.ScrollTo(uvPreview);
                        SetStatus("UV: 6面を個別投影・格子配置。位置と面IDを保持し、保存・Undo・出力を確認。");
                    }
                    catch (Exception e) { failure = e.ToString(); }
                if (failure == null)
                    yield return WorkbenchCapture.Write(GetComponent<UIDocument>(), camera, Path.Combine(output, "uv.png"), error => failure = error);
                completed(failure);
            }
            finally { uvPanel.value = false; ReplaceWorkspace(previous, path); }
        }
    }
}
