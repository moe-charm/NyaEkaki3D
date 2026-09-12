using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifySurfaceViewport(Action<string> completed)
        {
            string failure=null,state=workspace.Document.StateHash;
            var width=view.style.width;var height=view.style.height;var grow=view.style.flexGrow;
            var oldBounds=view.worldBound;var oldTexture=previewTexture;
            var output=workspace.Preview.Output;
            yield return WaitSurfaceReady(error=>failure=error);
            if(failure!=null) { completed(failure);yield break; }
            var oldCoverage=ReadySurface().Coverage;
            var world=new Vector3(.07f,.025f,0);var oldPoint=VertexPanelPoint(world);
            try
            {
                try
                {
                    Check(surfacePaintMode.value,"Resize fixture needs 3D paint mode");
                    Check(ReferenceEquals(oldCoverage,ReadySurface().Coverage),"Unchanged projection was rebuilt");
                    PointerProbe.Down(view,oldPoint);
                    Check(surfaceStroke!=null && projection.HasPaintPreview,"Resize fixture did not start a pending stroke");
                    view.style.flexGrow=0;view.style.width=oldBounds.width*.75f;view.style.height=oldBounds.height*.75f;
                }
                catch(Exception e) { failure=e.ToString(); }
                // Allow real layout and the viewport render-target replacement to run.
                for(int i=0;i<4;i++) yield return null;
                if(failure==null) yield return WaitSurfaceReady(error=>failure=error);
                if(failure==null) try
                {
                    Check(view.worldBound.width<oldBounds.width*.9f && view.worldBound.height<oldBounds.height*.9f,"Viewport did not resize");
                    Check(previewTexture!=oldTexture && camera.targetTexture==previewTexture,"Resize retained the old camera texture");
                    var resizedCoverage=ReadySurface().Coverage;
                    Check(!ReferenceEquals(oldCoverage,resizedCoverage),"Resize reused stale coverage");
                    Check(ReferenceEquals(resizedCoverage,ReadySurface().Coverage),"Resized coverage was not cached");
                    Check(surfaceStroke==null && !projection.HasPaintPreview && !view.HasPointerCapture(PointerId.mousePointerId),"Resize retained pending paint or pointer capture");
                    PointerProbe.Up(view,oldPoint);Check(workspace.Document.StateHash==state,"Late pointer up after resize committed paint");
                    var newPoint=VertexPanelPoint(world);Check(Vector2.Distance(oldPoint,newPoint)>10,"Projection did not follow resized viewport");
                    long revision=workspace.Document.DocumentRevision;
                    PointerProbe.Down(view,newPoint);Check(surfaceStroke!=null && surfaceStroke.PointCount>0,"Resized viewport ray missed the model");
                    var hit=surfaceStroke.Snapshot().Sections[0][0];
                    Check(Math.Abs(hit.X-.85f)<.002f && Math.Abs(hit.Y-.75f)<.002f,"Resized pointer mapped to the wrong UV");
                    PointerProbe.Up(view,newPoint);
                    Check(workspace.Document.DocumentRevision==revision+1 && workspace.Document.StateHash!=state,"Resized viewport failed to commit one stroke");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==state,"Resized stroke Undo differs");
                }
                catch(Exception e) { failure=e.ToString(); }
            }
            finally { CancelSurfaceStroke();view.style.width=width;view.style.height=height;view.style.flexGrow=grow; }
            for(int i=0;i<4;i++) yield return null;
            if(failure==null) try
            {
                Check(Math.Abs(view.worldBound.width-oldBounds.width)<1 && Math.Abs(view.worldBound.height-oldBounds.height)<1,"Viewport restoration differs");
                Check(workspace.Document.StateHash==state,"Viewport restoration changed the document");
            }
            catch(Exception e) { failure=e.ToString(); }
            completed(failure);
        }
    }
}
