using System;
using System.IO;
using NyaForge.Authoring;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BakeImporter
    {
        public static BakeImportResult ImportSurface(string manifestPath,string assetParent = "Assets",
            Transform sceneParent = null,bool createSceneInstance = false)
        {
            var surface = SurfaceBakeStore.Read(manifestPath);
            return ImportValidated(surface.Geometry,surface,assetParent,sceneParent,createSceneInstance,identity:BakeOutputIdentity.Read(manifestPath,surface.Geometry));
        }
        static Texture2D ImportBaseColor(string folder,SurfaceBakeDocument surface)
            =>ImportBaseColor(folder,surface.CopyPng(),surface.BaseColor.Width,surface.BaseColor.Height);
        static Texture2D ImportBaseColor(string folder,byte[] png,int width,int height,string name="BaseColor")
        {
            string path = folder+"/"+name+".png";
            File.WriteAllBytes(AbsoluteAssetPath(path),png);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("PNG texture importer unavailable.");
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 1024;
            importer.SaveAndReimport();
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (texture == null || texture.width != width || texture.height != height)
                throw new InvalidOperationException("Imported base-color dimensions changed.");
            return texture;
        }
        static void ApplyBaseColor(Material material,Texture2D texture)
        {
            if (!material.HasProperty("_MainTex") && !material.HasProperty("_BaseMap"))
                throw new InvalidOperationException("Shader does not support base-color textures.");
            if (material.HasProperty("_MainTex")) material.SetTexture("_MainTex",texture);
            if (material.HasProperty("_BaseMap")) material.SetTexture("_BaseMap",texture);
            if (material.HasProperty("_Color")) material.SetColor("_Color",Color.white);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor",Color.white);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic",0);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness",0);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness",0);
        }
    }
}
