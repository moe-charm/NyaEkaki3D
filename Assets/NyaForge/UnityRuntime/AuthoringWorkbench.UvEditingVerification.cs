using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyUvEditing(string output, Action<string> completed)
        {
            var previous = workspace; string path = savedDirectory, failure = null, original = null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null); CreatePolygonGraph();
                    Execute(AuthoringOperation.ProjectPolygonUv(activeEditContext));
                    original = workspace.Document.StateHash;
                    uvPanel.value = true; controls.ScrollTo(uvPreview);
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                controls.ScrollTo(uvPreview);yield return null;yield return null;
                if (failure == null) try
                {
                    UvNavigationVerification.Verify(uvPreview);
                    Check(selectedFaces.SetEquals(new ulong[]{1}) && workspace.Document.StateHash==original,"Navigated UV picking missed island or changed document");
                    float size = Mathf.Min(uvPreview.contentRect.width, uvPreview.contentRect.height) - 24;
                    PointerProbe.ClickAt(uvPreview, uvPreview.LocalToWorld(new Vector2(12+size*.5f,12+size*.5f)));
                    Check(selectedFaces.SetEquals(new ulong[]{1}) && workspace.Document.StateHash == original, "UV pointer selection changed document or missed island: faces=" + string.Join(",", selectedFaces) + ", unchanged=" + (workspace.Document.StateHash == original));
                    uvMoveU.SetValueWithoutNotify(.05f); uvMoveV.SetValueWithoutNotify(0);
                    uvRotation.SetValueWithoutNotify(30); uvScale.SetValueWithoutNotify(.7f);
                    controls.ScrollTo(uvTransformButton);
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return null; yield return null;
                if (failure == null) try
                {
                    PointerProbe.Click(uvTransformButton); string state = workspace.Document.StateHash;
                    Check(state != original, "UV transform button did not change document");
                    Execute(AuthoringOperation.Undo()); Check(workspace.Document.StateHash == original, "UV transform Undo failed");
                    Execute(AuthoringOperation.Redo()); Check(workspace.Document.StateHash == state, "UV transform Redo failed");
                    string dir = Path.Combine(output,"uv-edited-project"); projectPath.SetValueWithoutNotify(dir); SaveProject(); OpenProject();
                    Check(workspace.Document.StateHash == state, "UV transform reopen mismatch");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(dir,"exports","uv"),workspace)).Mesh.ContentHash == workspace.Evaluate().ContentHash, "UV transform Bake mismatch");
                    SelectEditStage(1); faceMode.value = true; selectedFaces.Add(1); Refresh();
                    Frame(); controls.ScrollTo(uvPreview); SetStatus("UV島: クリック選択・移動・30度回転・0.7倍。Undo・保存・出力を確認。");
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure == null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"uv-edited.png"),error=>failure=error);
                completed(failure);
            }
            finally { uvPanel.value = false; ReplaceWorkspace(previous,path); }
        }
    }
}
