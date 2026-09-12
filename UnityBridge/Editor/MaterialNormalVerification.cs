using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static void VerifyUntexturedMaterials(List<string> checks,List<string> folders)
        {
            string root=Path.GetDirectoryName(RequiredArgument(Environment.GetCommandLineArgs(),"--nyaforge-material-image"));
            var geometry=new MeshData(new[]{new Vec3(-.1f,-.1f,0),new Vec3(-.1f,.1f,0),new Vec3(.1f,.1f,0),new Vec3(.1f,-.1f,0)},
                Array.Empty<Vec3>(),Array.Empty<Vec4>(),Array.Empty<Vec2>(),new[]{new[]{0,1,2,0,2,3}});
            string originalHash=geometry.ContentHash;
            foreach(MaterialAlphaMode mode in Enum.GetValues(typeof(MaterialAlphaMode)))
            {
                string sourceId=Guid.NewGuid().ToString("D"),materialId=Guid.NewGuid().ToString("D"),assignId=Guid.NewGuid().ToString("D"),outputId=Guid.NewGuid().ToString("D");
                var parameters=new MaterialParameters(new Vec4(.65f,.3f,.5f,.8f),0,.65f,new Vec3(),mode,.4f);
                var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{
                    GraphNode.Source(sourceId,geometry,new RestTransform(1,new Vec3())),GraphNode.StandardMaterial(materialId,parameters),GraphNode.AssignMaterial(assignId),GraphNode.Output(outputId)},
                    new[]{new GraphEdge(sourceId,"mesh",assignId,"mesh"),new GraphEdge(materialId,"material",assignId,"material"),new GraphEdge(assignId,"mesh",outputId,"mesh")},outputId);
                var workspace=AuthoringWorkspace.CreateEmpty();
                new AuthoringCommandService(workspace).Execute(workspace.NewCommand(AuthoringOperation.AddGraph(graph)));
                string manifest=MaterialBakeStore.Export(Path.Combine(root,"no-image-"+mode),workspace);
                var bake=MaterialBakeStore.Read(manifest);
                Require(bake.BaseColor==null && bake.Geometry.Mesh.Normals.Count==0,"Normal fixture unexpectedly contains image or authored normals");
                var result=BakeImporter.ImportMaterial(manifest);folders.Add(result.AssetDirectory);
                var mesh=VerifyAssets(result,bake.Geometry,checks,"no-image-"+mode,true);
                foreach(var normal in mesh.normals) Near(normal,Vector3.back,"Generated planar normal");
                foreach(string path in result.MaterialPaths)
                {
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    Require(material.mainTexture==null,"Untextured material gained an image asset");
                    Require(material.shader.name==(mode==MaterialAlphaMode.Blend ? "NyaForge/AuthoringPbrBlend" : "NyaForge/AuthoringPbrOpaque"),"Untextured alpha shader differs");
                    if(mode==MaterialAlphaMode.Blend) Near(material.GetFloat("_ColorMask"),15,"Imported blend must write RGBA");
                    else Near(material.GetFloat("_Cutoff"),mode==MaterialAlphaMode.Cutout ? .4f : 0,"Untextured cutoff");
                }
                Require(MaterialBakeStore.Read(manifest).Geometry.Mesh.ContentHash==originalHash && geometry.Normals.Count==0,"Derived normal generation mutated source");
                SurfaceRenderVerification.Write(AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath),Path.Combine(root,"no-image-"+mode+".png"),false);
            }
            checks.Add("No-image Opaque/Cutout/Blend: serialized derived normals match known plane winding; source hash/absent normals preserved; material without texture renders under lighting.");
        }
    }
}
