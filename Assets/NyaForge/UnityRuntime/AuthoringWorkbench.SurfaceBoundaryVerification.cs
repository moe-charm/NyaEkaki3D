using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifySurfaceBoundaries(string directory,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null;
            try
            {
                foreach(string kind in new[]{"seam","gap","occluder","thin-occluder"})
                {
                    if(failure!=null) break;
                    try
                    {
                        string source=Guid.NewGuid().ToString("D"),paint=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                        var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{
                            GraphNode.Polygon(source,SurfacePaintFixtures.Create(kind),new RestTransform(1,new Vec3())),
                            GraphNode.Paint(paint),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",paint,"mesh"),
                            new GraphEdge(source,"mesh",output,"mesh"),new GraphEdge(paint,"image",output,"baseColor")},output);
                        var context=PaintEditing.Context(graph,paint);
                        var layer=new PaintLayer(Guid.NewGuid().ToString("D"),"Boundary paint",new PaintImage(256,256,new Rgba32(0,0,0,0)));
                        graph=graph.ReplaceNode(GraphNode.LayeredPaint(paint,new PaintLayers(256,256,new[]{layer}),context.UvHash,context.MeshDomain));
                        ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);Execute(AuthoringOperation.AddGraph(graph));
                        paintPanel.value=true;surfacePaintMode.value=true;Frame();
                        paintRadius.SetValueWithoutNotify(4);paintOpacity.SetValueWithoutNotify(1);
                        paintPalette.SetValueWithoutNotify(paintPalette.choices[0]);
                    }
                    catch(Exception e) { failure=kind+": "+e; }
                    yield return null;yield return null;
                    if(failure==null) yield return WaitSurfaceReady(error=>failure=error);
                    if(failure==null) try
                    {
                        Check(surfacePaintMode.value,"Boundary paint mode unavailable");
                        string before=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;
                        if(kind=="occluder" || kind=="thin-occluder")
                        {
                            var center=VertexPanelPoint(new Vector3(kind=="thin-occluder" ? .00124f : 0,0,.01f));
                            PointerProbe.Down(view,center);
                            Check(surfaceStroke!=null && surfaceStroke.PointCount==0 && !projection.HasPaintPreview,"Unpaintable foreground painted through to rear");
                            PointerProbe.Up(view,center);
                            Check(workspace.Document.StateHash==before && workspace.Document.DocumentRevision==revision,"Unpaintable click committed a command");
                        }
                        var a=VertexPanelPoint(new Vector3(-.07f,0,0));var b=VertexPanelPoint(new Vector3(.07f,0,0));
                        PointerProbe.Down(view,a);PointerProbe.Move(view,b);
                        Check(surfaceStroke!=null && surfaceStroke.Snapshot()?.Sections.Count==2,"Boundary drag must produce exactly two disconnected sections");
                        Check(projection.HasPaintPreview && workspace.Document.StateHash==before,"Boundary preview missing or committed early");
                        PointerProbe.Up(view,b);
                        string after=workspace.Document.StateHash;
                        Check(after!=before && workspace.Document.DocumentRevision==revision+1,"Boundary stroke must commit once");
                        var image=CurrentPaintLayer().Image;
                        // A sub-texel occluder splits the path; finite brush footprints can still overlap it.
                        if(kind!="thin-occluder") Check(image.GetPixel(128,128).A==0,"Boundary stroke bridged the unpainted center");
                        Check(Enumerable.Range(0,100).Any(x=>image.GetPixel(x,128).A>0) &&
                            Enumerable.Range(156,100).Any(x=>image.GetPixel(x,128).A>0),"Boundary stroke lost one side");
                        Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Boundary Undo differs");
                        Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Boundary Redo differs");
                        string path=Path.Combine(directory,"surface-boundary-"+kind);
                        ProjectStore.Save(path,workspace,0);
                        Check(ProjectStore.Open(path).Document.StateHash==after,"Boundary native roundtrip differs");
                        var baked=SurfaceBakeStore.Read(SurfaceBakeStore.Export(path+"-export",workspace));
                        Check(baked.BaseColor.CopyRgba().SequenceEqual(image.CopyRgba()),"Boundary export differs");
                        if(kind=="occluder") VerifyOpaqueSurfacePixel();
                    }
                    catch(Exception e) { failure=kind+": "+e; }
                    if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,
                        Path.Combine(directory,"surface-boundary-"+kind+".png"),error=>failure=error);
                }
                completed(failure);
            }
            finally { surfacePaintMode.SetValueWithoutNotify(false);CancelSurfaceStroke();paintPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }

        void VerifyOpaqueSurfacePixel()
        {
            camera.Render();var previous=RenderTexture.active;Texture2D pixel=null;
            try
            {
                RenderTexture.active=camera.targetTexture;pixel=new Texture2D(1,1,TextureFormat.RGBA32,false);
                pixel.ReadPixels(new Rect(camera.targetTexture.width/2,camera.targetTexture.height/2,1,1),0,0);pixel.Apply();
                var value=pixel.GetPixels32()[0];
                Check(value.a==255 && value.r<3 && value.g<3 && value.b<3,"Opaque preview leaked authored alpha or old background into viewport");
            }
            finally { RenderTexture.active=previous;if(pixel) UnityEngine.Object.Destroy(pixel); }
        }
    }
}
