using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static void VerifyMaterialAlpha(List<string> checks,List<string> folders)
        {
            string directory=Path.GetDirectoryName(RequiredArgument(Environment.GetCommandLineArgs(),"--nyaforge-material-image"));
            string source=Guid.NewGuid().ToString("D"),paint=Guid.NewGuid().ToString("D"),material=Guid.NewGuid().ToString("D"),assign=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
            var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{
                GraphNode.Polygon(source,PolygonPrimitives.Plane(Guid.NewGuid().ToString("D")),new RestTransform(1,new Vec3())),
                GraphNode.Paint(paint,4,4),GraphNode.StandardMaterial(material),GraphNode.AssignMaterial(assign),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(paint,"image",material,"baseColor"),
                    new GraphEdge(source,"mesh",assign,"mesh"),new GraphEdge(material,"material",assign,"material"),new GraphEdge(assign,"mesh",output,"mesh")},output);
            var context=PaintEditing.Context(graph,paint);
            graph=graph.ReplaceNode(GraphNode.Paint(paint,4,4,new PaintImage(4,4,new Rgba32(30,100,200,128)),context.UvHash,context.MeshDomain));
            var emission=new Vector3(.6f,.2f,.1f);
            var background=new Vector3(.04f,.08f,.12f);
            var measurements=new List<string>();
            void CheckCase(string name,MaterialAlphaMode mode,float tintAlpha,float cutoff,float coverage)
            {
                var parameters=new MaterialParameters(new Vec4(0,0,0,tintAlpha),1,1,new Vec3(emission.x,emission.y,emission.z),mode,cutoff);
                var workspace=AuthoringWorkspace.CreateEmpty();
                Require(new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph.ReplaceNode(GraphNode.StandardMaterial(material,parameters))))).Success,"Alpha fixture graph failed");
                var result=BakeImporter.ImportMaterial(MaterialBakeStore.Export(Path.Combine(directory,"alpha-"+name),workspace));
                folders.Add(result.AssetDirectory);
                // Reimport persisted assets before rendering; never substitute an in-memory adapter material.
                foreach(string path in result.MaterialPaths) AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                AssetDatabase.ImportAsset(result.PrefabPath,ImportAssetOptions.ForceUpdate);
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
                var expected=emission*coverage+background*(1-coverage);
                foreach(bool back in new[]{false,true})
                {
                    Color actual=MaterialPixelCapture.Read(prefab,new Color(background.x,background.y,background.z,1),back);
                    var observed=new Vector3(actual.r,actual.g,actual.b);
                    Require((observed-expected).sqrMagnitude<.000025f,"Serialized material alpha pixel differs: "+name+" back="+back+" expected="+expected+" actual="+observed);
                    measurements.Add(name+" back="+back+" expected="+expected.ToString("F6")+" actual="+observed.ToString("F6"));
                }
            }
            CheckCase("opaque-zero",MaterialAlphaMode.Opaque,0,.5f,1);
            CheckCase("cutout-visible",MaterialAlphaMode.Cutout,.5f,.249f,1);
            CheckCase("cutout-hidden",MaterialAlphaMode.Cutout,.5f,.253f,0);
            foreach(float alpha in new[]{0f,.5f,1f}) CheckCase("blend-"+alpha.ToString("F1",System.Globalization.CultureInfo.InvariantCulture),MaterialAlphaMode.Blend,alpha,.5f,(128f/255)*alpha);
            File.WriteAllLines(Path.Combine(directory,"material-alpha-pixels.txt"),measurements);
            checks.Add("Serialized material GPU RGB: Opaque ignores zero alpha; Cutout brackets texture*tint=128/255*0.5; Blend tint 0/0.5/1 matches linear background composition; front/back agree. Measurements: material-alpha-pixels.txt.");
        }
    }

    internal static class MaterialPixelCapture
    {
        internal static Color Read(GameObject prefab,Color background,bool back)
        {
            GameObject instance=null,cameraObject=null;RenderTexture target=null;Texture2D pixels=null;
            var previous=RenderTexture.active;var ambient=RenderSettings.ambientLight;var mode=RenderSettings.ambientMode;float reflection=RenderSettings.reflectionIntensity;
            try
            {
                instance=Object.Instantiate(prefab);
                foreach(var child in instance.GetComponentsInChildren<Transform>(true)) child.gameObject.layer=31;
                if(back) instance.transform.rotation=Quaternion.Euler(0,180,0);
                var bounds=instance.GetComponent<MeshRenderer>().bounds;
                cameraObject=new GameObject("NyaForge material pixel capture");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                camera.orthographic=true;camera.orthographicSize=Mathf.Max(bounds.extents.x,bounds.extents.y)*1.2f;
                camera.transform.position=bounds.center+Vector3.back*(bounds.size.magnitude+1);camera.nearClipPlane=.01f;camera.farClipPlane=bounds.size.magnitude+3;
                // Camera color is interpreted as sRGB even for a linear float target.
                camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=background.gamma;camera.cullingMask=1<<31;camera.renderingPath=RenderingPath.Forward;
                RenderSettings.ambientMode=AmbientMode.Flat;RenderSettings.ambientLight=Color.black;RenderSettings.reflectionIntensity=0;
                target=new RenderTexture(32,32,24,RenderTextureFormat.ARGBFloat,RenderTextureReadWrite.Linear);target.Create();camera.targetTexture=target;camera.Render();
                RenderTexture.active=target;pixels=new Texture2D(32,32,TextureFormat.RGBAFloat,false,true);pixels.ReadPixels(new Rect(0,0,32,32),0,0);pixels.Apply();
                return pixels.GetPixel(16,16);
            }
            finally
            {
                RenderTexture.active=previous;RenderSettings.ambientMode=mode;RenderSettings.ambientLight=ambient;RenderSettings.reflectionIntensity=reflection;
                if(cameraObject) Object.DestroyImmediate(cameraObject);if(instance) Object.DestroyImmediate(instance);if(pixels) Object.DestroyImmediate(pixels);
                if(target) { target.Release();Object.DestroyImmediate(target); }
            }
        }
    }
}
