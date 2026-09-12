using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Rendering;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static void VerifyMaterial(string manifest,List<string> checks,List<string> folders)
        {
            var source=MaterialBakeStore.Read(manifest);
            var result=BakeImporter.ImportMaterial(manifest);folders.Add(result.AssetDirectory);
            VerifyAssets(result,source.Geometry,checks,"standard-material",true);
            Texture2D texture=null;
            if(source.BaseColor!=null)
            {
                string texturePath=result.AssetDirectory+"/BaseColor.png";
                Require(File.ReadAllBytes(texturePath).SequenceEqual(source.CopyPng()),"Material PNG bytes differ");
                var importer=(TextureImporter)AssetImporter.GetAtPath(texturePath);
                Require(importer.sRGBTexture && !importer.alphaIsTransparency && !importer.mipmapEnabled && importer.textureCompression==TextureImporterCompression.Uncompressed,"Material texture settings differ");
                texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                Require(texture.width==source.BaseColor.Width && texture.height==source.BaseColor.Height,"Material texture dimensions differ");
            }
            var expected=StandardMaterialAdapter.Create(source.Material);
            try
            {
                foreach(string path in result.MaterialPaths)
                {
                    AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                    var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                    Require(material!=null && material.shader==expected.shader,"Serialized standard shader reference differs");
                    foreach(string property in new[]{"_TintLinear","_EmissionLinear"}) Require(material.GetVector(property)==expected.GetVector(property),"Material vector differs: "+property);
                    foreach(string property in new[]{"_Metallic","_Smoothness"}) Near(material.GetFloat(property),expected.GetFloat(property),"Material scalar "+property);
                    if(material.HasProperty("_Cutoff")) Near(material.GetFloat("_Cutoff"),expected.GetFloat("_Cutoff"),"Material cutoff");
                    Require(material.renderQueue==expected.renderQueue && material.GetTag("RenderType",false)==expected.GetTag("RenderType",false),"Material alpha mode differs");
                    Require(material.mainTexture==texture,"Material texture reference differs");
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(expected); }
            string[] before=Directory.GetDirectories("Assets").OrderBy(p=>p).ToArray();string failedFolder=null;bool rejected=false;
            try { BakeImporter.VerifyMaterialFailure(manifest,folder=> { failedFolder=folder;throw new InvalidOperationException("Expected material import rollback probe"); }); }
            catch(InvalidOperationException error) { rejected=error.Message=="Expected material import rollback probe"; }
            Require(rejected && failedFolder!=null && !Directory.Exists(failedFolder),"Material import failure did not remove its owned folder");
            Require(before.SequenceEqual(Directory.GetDirectories("Assets").OrderBy(p=>p)),"Material rollback changed unrelated asset folders");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath)!=null,"Rollback removed the successful import");
            checks.Add("Standard material import: validated mesh/material/image, serialized shader/values/texture/prefab, owned-folder rollback preserves previous imports.");
            SurfaceRenderVerification.Write(AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath),RequiredArgument(Environment.GetCommandLineArgs(),"--nyaforge-material-image"),false);
            checks.Add("Serialized standard material prefab rendered to material.png with a visible object.");
            VerifyUntexturedMaterials(checks,folders);
            VerifyMaterialAlpha(checks,folders);
        }
    }
}
