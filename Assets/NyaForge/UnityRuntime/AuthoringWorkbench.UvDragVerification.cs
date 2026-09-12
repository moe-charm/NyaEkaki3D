using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyUvDrag(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                try
                {
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);CreatePolygonGraph();
                    Execute(AuthoringOperation.ProjectPolygonUv(activeEditContext));
                    uvPanel.value=true;controls.ScrollTo(uvPreview);
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;
                if(failure==null) try
                {
                    var center=new Vector2(.5f,.5f);var start=uvPreview.LocalToWorld(uvPreview.UvToLocal(center));
                    PointerProbe.ClickAt(uvPreview,start);
                    string original=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;
                    var delta=new Vector2(1.25f,.15f);var end=uvPreview.LocalToWorld(uvPreview.UvToLocal(center+delta));
                    PointerProbe.Down(uvPreview,start);PointerProbe.Move(uvPreview,Vector2.Lerp(start,end,.3f));PointerProbe.Move(uvPreview,end);
                    Check(uvPreview.IsDragging && Vector2.Distance(uvPreview.DragOffset,delta)<.00001f,"UV drag preview differs from inverse mapping");
                    Check(workspace.Document.StateHash==original && workspace.Document.DocumentRevision==revision,"UV drag preview committed early");
                    PointerProbe.Up(uvPreview,end);
                    string moved=workspace.Document.StateHash;
                    Check(!uvPreview.IsDragging && moved!=original && workspace.Document.DocumentRevision==revision+1,"UV drag did not commit exactly once");
                    var corner=workspace.Preview.Output.Polygon.Faces[0].Corners[0].Uv0.Value;
                    Check(corner.X>1 && uvPreview.VisibleFaceCount==1,"Outside-tile UV disappeared");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==original,"UV drag is not one Undo");
                    Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==moved,"UV drag Redo differs");
                    // Pan the moved island back into the canvas, then pick and cancel a drag there.
                    var shift=uvPreview.UvToLocal(center)-uvPreview.UvToLocal(center+delta);
                    PointerProbe.Down(uvPreview,start,1);PointerProbe.Move(uvPreview,start+shift,1);PointerProbe.Up(uvPreview,start+shift,1);
                    var movedCenter=uvPreview.LocalToWorld(uvPreview.UvToLocal(center+delta));
                    selectedFaces.Clear();Refresh();PointerProbe.ClickAt(uvPreview,movedCenter);
                    Check(selectedFaces.SetEquals(new ulong[]{1}),"Outside-tile island was not selectable after pan");
                    PointerProbe.Down(uvPreview,movedCenter);PointerProbe.Move(uvPreview,movedCenter+new Vector2(20,10));
                    using(var escape=KeyDownEvent.GetPooled(new Event { type=EventType.KeyDown,keyCode=KeyCode.Escape })) uvPreview.SendEvent(escape);
                    PointerProbe.Up(uvPreview,movedCenter+new Vector2(20,10));
                    Check(!uvPreview.IsDragging && workspace.Document.StateHash==moved,"Esc committed a UV drag");
                    PointerProbe.Down(uvPreview,movedCenter);PointerProbe.Move(uvPreview,movedCenter+new Vector2(10,10));
                    uvPreview.ReleasePointer(PointerId.mousePointerId);
                    PointerProbe.Up(uvPreview,movedCenter+new Vector2(10,10));
                    Check(!uvPreview.IsDragging && workspace.Document.StateHash==moved,"Capture loss committed a UV drag");
                    var stale=BeginUvIslandDrag();
                    Execute(AuthoringOperation.TransformUvIslands(activeEditContext,new ulong[]{1},new UvTransformSettings(new Vec2(.1f,0),0,1)));
                    string changed=workspace.Document.StateHash;long changedRevision=workspace.Document.DocumentRevision;
                    stale(new Vector2(.2f,0));
                    Check(workspace.Document.StateHash==changed && workspace.Document.DocumentRevision==changedRevision,"Stale UV gesture overwrote a newer edit");
                    string directory=Path.Combine(output,"uv-drag-project");projectPath.SetValueWithoutNotify(directory);SaveProject();OpenProject();
                    Check(workspace.Document.StateHash==changed,"UV drag native reopen differs");
                    Check(BakeStore.Read(BakeStore.Export(Path.Combine(directory,"exports","uv-drag"),workspace)).Mesh.ContentHash==workspace.Evaluate().ContentHash,"UV drag Bake differs");
                    SelectEditStage(1);Refresh();SetStatus("UVドラッグ: 範囲外移動・取消・Undo・保存・出力を確認。");
                }
                catch(Exception e) { failure=e.ToString(); }
                completed(failure);
            }
            finally { uvPreview.CancelIslandDrag();uvPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
