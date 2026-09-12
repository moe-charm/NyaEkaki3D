using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifySolidify(string output, Action<string> completed)
        {
            var previous = workspace; string path = savedDirectory, failure = null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null); CreatePolygonGraph();
                    shellThickness.SetValueWithoutNotify(20); controls.ScrollTo(solidifyButton);
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                if (failure == null)
                    try
                    {
                        PointerProbe.Click(solidifyButton);
                        var shell = workspace.Preview.Output.Polygon;
                        Check(shell.Faces.Count == 6 && shell.EdgeFaces.Values.All(e => e.Count == 2) && workspace.Evaluate().TriangleCount == 12, "Solidify did not close the quad");
                        string state = workspace.Document.StateHash;
                        Execute(AuthoringOperation.Undo()); Check(workspace.Preview.Output.Polygon.Faces.Count == 1, "Solidify Undo failed");
                        Execute(AuthoringOperation.Redo()); Check(workspace.Document.StateHash == state, "Solidify Redo failed");
                        string dir = Path.Combine(output, "solidify-project"); projectPath.SetValueWithoutNotify(dir); SaveProject(); OpenProject();
                        Check(workspace.Document.StateHash == state, "Solidify reopen failed");
                        var bake = BakeStore.Read(BakeStore.Export(Path.Combine(dir,"exports","shell"),workspace));
                        Check(bake.Mesh.ContentHash == workspace.Evaluate().ContentHash && bake.Mesh.TriangleCount == 12, "Solidify Bake mismatch");
                        SelectEditStage(1); faceMode.value = true; selectedFaces.Add(1); Refresh();
                        Frame(); orbit = Quaternion.Euler(20, -30, 0); UpdateCamera(); controls.ScrollTo(shellThickness);
                        SetStatus("厚み20mm: 閉じた6面 / 12三角形。Undo・保存・出力を確認。");
                    }
                    catch (Exception e) { failure = e.ToString(); }
                if (failure == null)
                    yield return WorkbenchCapture.Write(GetComponent<UnityEngine.UIElements.UIDocument>(), camera, Path.Combine(output,"solidify.png"), error => failure = error);
                completed(failure);
            }
            finally { ReplaceWorkspace(previous,path); }
        }
    }
}
