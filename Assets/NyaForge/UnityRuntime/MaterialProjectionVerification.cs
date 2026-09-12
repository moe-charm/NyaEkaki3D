using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    internal static class MaterialProjectionVerification
    {
        public static void Verify(string directory,List<string> checks)
        {
            var root=new GameObject("Material projection fixture");root.transform.position=new Vector3(10,0,0);
            var projection=new OwnedMeshProjection(root.transform);var cameraObject=new GameObject("Material test camera");
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.transform.position=new Vector3(10,0,-.4f);
            camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(0,0,0,1);camera.cullingMask=1<<OwnedMeshProjection.PreviewLayer;
            camera.nearClipPlane=.01f;camera.farClipPlane=2;camera.renderingPath=RenderingPath.Forward;
            var target=new RenderTexture(64,64,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);target.Create();camera.targetTexture=target;
            void Require(bool condition,string message) { if(!condition) throw new InvalidOperationException(message); }
            Color Render(string name)
            {
                camera.Render();var previous=RenderTexture.active;var pixels=new Texture2D(64,64,TextureFormat.RGBAFloat,false,true);
                try
                {
                    RenderTexture.active=target;pixels.ReadPixels(new Rect(0,0,64,64),0,0);pixels.Apply();
                    var center=pixels.GetPixel(32,32);var png=new Texture2D(64,64,TextureFormat.RGBA32,false);
                    try
                    {
                        var colors=pixels.GetPixels();for(int i=0;i<colors.Length;i++) colors[i]=colors[i].gamma;
                        png.SetPixels(colors);png.Apply();File.WriteAllBytes(Path.Combine(directory,"material-"+name+".png"),png.EncodeToPNG());
                    }
                    finally { UnityEngine.Object.Destroy(png); }
                    return center;
                }
                finally { RenderTexture.active=previous;UnityEngine.Object.Destroy(pixels); }
            }
            try
            {
                string source=Guid.NewGuid().ToString("D"),paint=Guid.NewGuid().ToString("D"),material=Guid.NewGuid().ToString("D"),assign=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                var nodes=new[]{GraphNode.Polygon(source,PolygonPrimitives.Plane(Guid.NewGuid().ToString("D")),new RestTransform(1,new Vec3())),
                    GraphNode.Paint(paint,4,4),GraphNode.StandardMaterial(material),GraphNode.AssignMaterial(assign),GraphNode.Output(output)};
                var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),nodes,new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(paint,"image",material,"baseColor"),
                    new GraphEdge(source,"mesh",assign,"mesh"),new GraphEdge(material,"material",assign,"material"),new GraphEdge(assign,"mesh",output,"mesh")},output);
                var context=PaintEditing.Context(graph,paint);
                graph=graph.ReplaceNode(GraphNode.Paint(paint,4,4,new PaintImage(4,4,new Rgba32(40,80,160,128)),context.UvHash,context.MeshDomain));
                MaterialParameters Parameters(MaterialAlphaMode mode,float cutoff=.5f)=>new MaterialParameters(new Vec4(0,0,0,.5f),.7f,.3f,new Vec3(.6f,.2f,.1f),mode,cutoff);
                graph=graph.ReplaceNode(GraphNode.StandardMaterial(material,Parameters(MaterialAlphaMode.Opaque)));
                var workspace=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(workspace);
                Require(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)),projection).Success,"Material projection creation failed");
                var mesh=projection.DisplayMesh;var display=projection.DisplayObject;var renderer=display.transform.Find("Evaluated mesh").GetComponent<MeshRenderer>();
                var original=renderer.sharedMaterial;var opaque=Render("opaque");
                Require(opaque.r>.5f && opaque.a>.99f,"Opaque PBR did not render emission with full target alpha");
                Require(Math.Abs(original.GetFloat("_Smoothness")-.7f)<.0001f && Math.Abs(original.GetFloat("_Metallic")-.7f)<.0001f,"PBR scalar mapping differs");
                void Set(MaterialAlphaMode mode,float cutoff=.5f)
                {
                    var result=commands.Execute(workspace.NewCommand(AuthoringOperation.UpdateNode(GraphNode.StandardMaterial(material,Parameters(mode,cutoff)))),projection);
                    Require(result.Success,"Material update failed: "+result.Code);
                    Require(projection.DisplayMesh==mesh && projection.DisplayObject==display,"Material update rebuilt geometry");
                }
                Set(MaterialAlphaMode.Cutout,.4f);var hidden=Render("cutout-hidden");Require(hidden.r<.001f && hidden.a>.99f,"Cutout did not discard texture-times-tint alpha");
                Set(MaterialAlphaMode.Cutout,.2f);var visible=Render("cutout-visible");Require(Math.Abs(visible.r-opaque.r)<.01f,"Cutout visible pixel differs from opaque");
                Set(MaterialAlphaMode.Blend);var blended=Render("blend");float alpha=.5f*128/255;
                Require(Math.Abs(blended.r-opaque.r*alpha)<.02f && blended.a>.99f,"Blend coverage or viewport target alpha differs");
                Require(commands.Execute(workspace.NewCommand(AuthoringOperation.Undo()),projection).Success,"Material Undo failed");
                Require(renderer.sharedMaterial.shader.name.EndsWith("Opaque"),"Material Undo did not restore alpha shader");
                Require(commands.Execute(workspace.NewCommand(AuthoringOperation.Redo()),projection).Success,"Material Redo failed");
                var current=renderer.sharedMaterial;projection.ShowPaintPreview(new PaintImage(4,4,new Rgba32(1,2,3,255)));var pending=current.mainTexture;
                using(var prepared=projection.PrepareGraph(workspace.Document,workspace.Preview))
                {
                    Require(renderer.sharedMaterial==current && current.mainTexture==pending,"Material prepare mutated pending preview");
                    prepared.Commit();Require(renderer.sharedMaterial!=current && !projection.HasPaintPreview,"Material commit retained old preview");
                    prepared.Rollback();Require(renderer.sharedMaterial==current && current.mainTexture==pending && projection.HasPaintPreview,"Material rollback lost pending image");
                }
                projection.ShowPaintPreview(null);Require(renderer.sharedMaterial.GetFloat("_Metallic")==.7f,"Temporary image overwrote material parameters");
                Color Lit(float metallic,float roughness,string name)
                {
                    var parameters=new MaterialParameters(new Vec4(1,.01f,.01f,1),metallic,roughness,new Vec3());
                    Require(commands.Execute(workspace.NewCommand(AuthoringOperation.UpdateNode(GraphNode.StandardMaterial(material,parameters))),projection).Success,"Lit material update failed");
                    return Render(name);
                }
                var red=Lit(0,.2f,"lit-dielectric");var rough=Lit(0,.9f,"lit-rough");var metal=Lit(1,.2f,"lit-metal");
                Require(red.r>red.g*1.5f && red.r>red.b*1.5f,"Texture and tint did not produce red lighting");
                Require(Math.Abs(red.r-rough.r)+Math.Abs(red.g-rough.g)+Math.Abs(red.b-rough.b)>.0001f,"Roughness had no visible lighting effect");
                Require(Math.Abs(red.r-metal.r)+Math.Abs(red.g-metal.g)+Math.Abs(red.b-metal.b)>.0001f,"Metallic had no visible lighting effect");
                var bare=AuthoringFixtures.Panel(1);var bareHash=bare.ContentHash;var displayOnly=OwnedMeshProjection.CreateMesh(bare);
                try { Require(displayOnly.normals.Length==bare.VertexCount && bare.ContentHash==bareHash,"Display normal generation changed the source or left lighting normals missing"); }
                finally { UnityEngine.Object.Destroy(displayOnly); }
                checks.Add("Standard material projection: owned shader/texture, roughness mapping, opaque/cutout/blend GPU pixels, viewport alpha, mesh reuse, Undo/Redo and rollback with pending paint");
            }
            finally { projection.Dispose();camera.targetTexture=null;target.Release();UnityEngine.Object.Destroy(target);UnityEngine.Object.Destroy(cameraObject);UnityEngine.Object.Destroy(root); }
        }
    }
}
