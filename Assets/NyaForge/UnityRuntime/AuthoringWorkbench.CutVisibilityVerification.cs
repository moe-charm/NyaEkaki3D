using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyCutVisibility(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null,before=null;
            bool previousOrtho=camera.orthographic;float previousSize=camera.orthographicSize;Vector2 screen=default;
            try
            {
                try
                {
                    var positions=new[]{new Vec3(-.1f,0,0),new Vec3(.1f,0,0),new Vec3(.1f,.1f,0),new Vec3(-.1f,.1f,0),new Vec3(-.12f,-.002f,.1f),new Vec3(.12f,-.002f,.1f),new Vec3(.12f,.12f,.1f),new Vec3(-.12f,.12f,.1f)};
                    ulong corner=0;CageFace Face(ulong id,ulong[] ids)=>new CageFace(id,0,ids.Select(v=>new CageCorner(++corner,v)));
                    var mesh=new PolygonMesh(Guid.NewGuid().ToString("D"),positions.Select((p,i)=>new CageVertex((ulong)i+1,p)),new[]{Face(1,new ulong[]{1,2,3,4}),Face(2,new ulong[]{5,6,7,8})});
                    string source=Guid.NewGuid().ToString("D"),edit=Guid.NewGuid().ToString("D"),end=Guid.NewGuid().ToString("D");
                    var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(end)},new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",end,"mesh")},end);
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);Execute(AuthoringOperation.AddGraph(graph));SelectEditStage(1);faceMode.value=false;Frame();
                    camera.orthographic=true;camera.orthographicSize=.16f;UpdateCamera();cutPathPanel.value=true;cutPathPickMode.value=true;cutVisibleOnly.value=true;
                    before=workspace.Document.StateHash;screen=VertexPanelPoint(Vector3.zero);
                }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;controls.ScrollTo(cutVisibleOnly);yield return null;yield return null;
                if(failure==null) try
                {
                    screen=VertexPanelPoint(Vector3.zero);PointerProbe.Move(view,screen);
                    Check(cutHoverHit!=null && cutHoverHit.Location.Edge.Equals(new CageEdgeId(5,6)),"Visible fallback edge differs");
                    var cached=cutVisibilityGeometry;FindCutPathHit(screen);Check(ReferenceEquals(cached,cutVisibilityGeometry),"Visibility BVH not reused");
                    PointerProbe.Click(cutVisibleOnly);Check(!cutVisibleOnly.value && cutHoverHit==null,"Visibility toggle did not clear hover");
                    PointerProbe.Move(view,screen);Check(cutHoverHit!=null && cutHoverHit.Location.Edge.Equals(new CageEdgeId(1,2)),"Xray did not pick closest hidden edge");
                    PointerProbe.ClickAt(view,screen);Check(ReadCutPath().Single().Edge.Equals(new CageEdgeId(1,2)),"Xray click and hover differ");
                    cutPathText.value="";PointerProbe.Click(cutVisibleOnly);PointerProbe.Move(view,screen);
                    Check(cutHoverHit!=null && cutHoverHit.Location.Edge.Equals(new CageEdgeId(5,6)),"Visible candidate did not return");
                    PointerProbe.ClickAt(view,screen);PointerProbe.Move(view,screen);
                    Check(ReadCutPath().Single().Edge.Equals(new CageEdgeId(5,6)) && cutHoverInfo.text.Contains("登録済み"),"Registered hover label missing");
                    Check(workspace.Document.StateHash==before,"Visibility and draft changed document");
                    UpdateCamera();Check(cutHoverHit==null && cutHoverEdge.VertexCount==0,"Camera update retained hover");
                    PointerProbe.Move(view,screen);SetStatus("可視性ON：手前の辺が黄色。裏の辺は候補から除外。文書は未変更。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"cut-visibility-hover.png"),error=>failure=error);
                if(failure==null) try
                {
                    var cached=cutVisibilityGeometry;Execute(AuthoringOperation.DeletePolygonFaces(activeEditContext,new ulong[]{2}));
                    var hit=FindCutPathHit(screen);Check(hit!=null && hit.Location.Edge.Equals(new CageEdgeId(1,2)) && !ReferenceEquals(cached,cutVisibilityGeometry),"Geometry deletion did not invalidate visibility cache");
                    Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Visibility fixture Undo differs");
                    Check(FindCutPathHit(screen).Location.Edge.Equals(new CageEdgeId(5,6)),"Undo did not restore occlusion");
                }
                catch(Exception e) { failure=e.ToString(); }
                completed(failure);
            }
            finally { camera.orthographic=previousOrtho;camera.orthographicSize=previousSize;cutPathPanel.value=false;cutVisibleOnly.value=true;ReplaceWorkspace(previous,previousPath); }
        }
    }
}
