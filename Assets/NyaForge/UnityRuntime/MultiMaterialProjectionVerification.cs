using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    internal static class MultiMaterialProjectionVerification
    {
        public static void Verify(string directory,List<string> checks)
        {
            void Require(bool valid,string message) { if(!valid) throw new InvalidOperationException(message); }
            var root=new GameObject("Multiple material fixture");root.transform.position=new Vector3(20,0,0);
            var projection=new OwnedMeshProjection(root.transform);var cameraObject=new GameObject("Multiple material camera");
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(20,0,-.4f);
            camera.nearClipPlane=.01f;camera.farClipPlane=2;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.black;camera.cullingMask=1<<OwnedMeshProjection.PreviewLayer;
            var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);target.Create();camera.targetTexture=target;
            Color Capture(string name)
            {
                camera.Render();var previous=RenderTexture.active;var pixels=new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);
                try
                {
                    RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();
                    var color=pixels.GetPixel(32,32);var png=new Texture2D(64,64,TextureFormat.RGBA32,false);
                    try { png.SetPixels(pixels.GetPixels().Select(p=>p.gamma).ToArray());png.Apply();File.WriteAllBytes(Path.Combine(directory,"multi-material-"+name+".png"),png.EncodeToPNG()); }
                    finally { UnityEngine.Object.Destroy(png); }
                    return color;
                }
                finally { RenderTexture.active=previous;UnityEngine.Object.Destroy(pixels); }
            }
            try
            {
                var original=TriangleMeshAdapter.Import(Guid.NewGuid().ToString("D"),AuthoringFixtures.Panel(1));
                // Offset the back surface so the two-sided shaders do not overlap at identical depth.
                var polygon=new PolygonMesh(original.DomainId,original.Vertices.Values.Select(v=>new CageVertex(v.Id,new Vec3(v.Position.X,v.Position.Y,v.Position.Z+(v.Id>4 ? .01f : 0)))),
                    original.Faces.Select(f=>new CageFace(f.Id,f.Material==0 ? 3 : 9,f.Corners)));
                string source=Guid.NewGuid().ToString("D"),a=Guid.NewGuid().ToString("D"),b=Guid.NewGuid().ToString("D"),assign=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                MaterialParameters Emission(Vec3 color)=>new MaterialParameters(new Vec4(0,0,0,1),1,1,color);
                var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.StandardMaterial(a,Emission(new Vec3(.6f,0,0))),GraphNode.StandardMaterial(b,Emission(new Vec3(0,.6f,0))),GraphNode.AssignMaterials(assign,new[]{9,3}),GraphNode.Output(output)},
                    new[]{new GraphEdge(source,"mesh",assign,"mesh"),new GraphEdge(a,"material",assign,"material-3"),new GraphEdge(b,"material",assign,"material-9"),new GraphEdge(assign,"mesh",output,"mesh")},output);
                var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
                Require(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)),projection).Success,"Multiple material projection failed");
                var mesh=projection.DisplayMesh;var display=projection.DisplayObject;var renderer=display.transform.Find("Evaluated mesh").GetComponent<MeshRenderer>();
                Require(renderer.sharedMaterials.Length==2 && renderer.sharedMaterials[0]!=renderer.sharedMaterials[1],"Distinct material slots collapsed");
                var front=Capture("front");Require(front.r>.5f && front.g<.01f,"Front slot did not render red: "+front);
                display.transform.localRotation=Quaternion.Euler(0,180,0);var back=Capture("back");Require(back.g>.5f && back.r<.01f,"Back slot did not render green: "+back);display.transform.localRotation=Quaternion.identity;
                var before=renderer.sharedMaterials;var otherTexture=before[1].mainTexture;
                projection.ShowPaintPreview(new PaintImage(4,4,new Rgba32(30,80,120,255)),3);var pending=before[0].mainTexture;
                Require(projection.HasPaintPreview && before[1].mainTexture==otherTexture,"Slot preview changed another material");
                using(var prepared=projection.PrepareGraph(workspace.Document,workspace.Preview))
                { prepared.Commit();prepared.Rollback();Require(renderer.sharedMaterials[0]==before[0] && before[0].mainTexture==pending && projection.HasPaintPreview,"Multiple material rollback lost pending texture"); }
                projection.ShowPaintPreview(null);
                projection.ShowPaintPreviews(new PaintImage(4,4,new Rgba32(40,50,60,255)),new[]{3,9});
                var sharedPreview=renderer.sharedMaterials.Select(m=>m.mainTexture).ToArray();
                Require(sharedPreview[0]!=pending && sharedPreview[1]!=otherTexture,"Shared Paint did not preview both slots");
                bool invalid=false;
                try { projection.ShowPaintPreviews(new PaintImage(4,4,new Rgba32(1,2,3,255)),new[]{3,99}); }
                catch(InvalidOperationException) { invalid=true; }
                Require(invalid && renderer.sharedMaterials.Select(m=>m.mainTexture).SequenceEqual(sharedPreview),"Invalid multi-slot target partially changed textures");
                using(var prepared=projection.PrepareGraph(workspace.Document,workspace.Preview))
                { prepared.Commit();prepared.Rollback();Require(renderer.sharedMaterials.Select(m=>m.mainTexture).SequenceEqual(sharedPreview),"Rollback lost shared slot previews"); }
                projection.ShowPaintPreview(null);Require(!projection.HasPaintPreview,"Shared preview cancellation left a texture active");
                Require(commands.Execute(workspace.NewCommand(AuthoringOperation.UpdateNode(GraphNode.StandardMaterial(a,Emission(new Vec3(0,0,.6f))))),projection).Success,"Slot material edit failed");
                Require(projection.DisplayMesh==mesh && projection.DisplayObject==display,"Slot color edit rebuilt geometry");
                var blue=Capture("edited");Require(blue.b>.5f && blue.r<.01f,"Edited slot did not render blue");
                Require(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo()),projection).Success,"Slot Undo failed");
                Require(Capture("undo").r>.5f,"Slot Undo did not restore red");
                checks.Add("Multiple material projection: sparse 3/9 render red front/green back; blue edit preserves mesh/root; Undo and rollback preserve slot texture ownership.");
            }
            finally { projection.Dispose();camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(cameraObject);UnityEngine.Object.Destroy(root); }
        }
    }
}
