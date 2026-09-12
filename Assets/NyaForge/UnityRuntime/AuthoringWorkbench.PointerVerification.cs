using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyCanvasPointer(string output, Action<string> completed)
        {
            var previous = workspace; string path = savedDirectory;
            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null); graphCanvas.style.display = DisplayStyle.Flex;
            try
            {
                yield return null; yield return null;
                string failure = null;
                yield return graphCanvas.VerifyPointerEvents(error => failure = error);
                if (failure != null) { completed(failure); yield break; }
                Frame();
                yield return null; yield return null;
                try
                {
                    PointerProbe.ClickAt(view, VertexPanelPoint(projection.Points[0]));
                    Check(selection.Contains(0) && activeEditContext != null, "Pointer did not select graph vertex");
                    moveX.SetValueWithoutNotify(10); moveY.SetValueWithoutNotify(0); moveZ.SetValueWithoutNotify(0);
                    controls.ScrollTo(moveButton);
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure != null) { completed(failure); yield break; }
                yield return null; yield return null;
                string directory = Path.Combine(output, "pointer-project");
                try
                {
                    PointerProbe.Click(moveButton);
                    Check(Math.Abs(workspace.Evaluate().Positions[0].X + .165f) < .00001f, "Pointer vertex move did not apply 10mm after width change");
                    projectPath.SetValueWithoutNotify(directory); controls.ScrollTo(root.Q<Button>("authoring-save"));
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure != null) { completed(failure); yield break; }
                yield return null; yield return null;
                string state = workspace.Document.StateHash;
                try { PointerProbe.Click(root.Q<Button>("authoring-save")); Check(!workspace.IsDirty, "Pointer Save failed"); }
                catch (Exception e) { failure = e.ToString(); }
                if (failure != null) { completed(failure); yield break; }
                yield return null; yield return null;
                try
                {
                    PointerProbe.Click(root.Q<Button>("authoring-open"));
                    Check(workspace.Document.StateHash == state && !workspace.CanUndo, "Pointer reopen failed");
                    controls.ScrollTo(root.Q<Button>("authoring-export"));
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure != null) { completed(failure); yield break; }
                yield return null; yield return null;
                try
                {
                    PointerProbe.Click(root.Q<Button>("authoring-export"));
                    var manifests = Directory.GetFiles(Path.Combine(directory, "exports"), BakeStore.ManifestName, SearchOption.AllDirectories);
                    Check(manifests.Length == 1 && BakeStore.Read(manifests[0]).MeshContentHash == workspace.Evaluate().ContentHash, "Pointer Export failed");
                }
                catch (Exception e) { failure = e.ToString(); }
                completed(failure);
            }
            finally { graphCanvas.style.display = DisplayStyle.None; ReplaceWorkspace(previous, path); }
        }
    }
}
