using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static void VerifySurface(string manifest,List<string> checks,List<string> folders)
        {
            var source = SurfaceBakeStore.Read(manifest);
            var result = BakeImporter.ImportSurface(manifest); folders.Add(result.AssetDirectory);
            VerifyAssets(result,source.Geometry,checks,"surface");
            string path = result.AssetDirectory+"/BaseColor.png";
            Require(File.ReadAllBytes(path).SequenceEqual(source.CopyPng()),"Imported PNG bytes changed.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            Require(importer.sRGBTexture && !importer.alphaIsTransparency && !importer.mipmapEnabled &&
                importer.textureCompression == TextureImporterCompression.Uncompressed && importer.npotScale == TextureImporterNPOTScale.None,
                "Surface texture import settings differ.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            Require(texture.width == source.BaseColor.Width && texture.height == source.BaseColor.Height,"Surface texture size differs.");
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
            foreach (var material in prefab.GetComponent<MeshRenderer>().sharedMaterials)
            {
                Require(material.mainTexture == texture,"Prefab material lost base-color texture.");
                Require(material.color == Color.white,"Base color is tinted by the material.");
            }
            checks.Add("Surface PNG bytes, sRGB import, dimensions, opaque white material texture references and serialized prefab preserved.");
            var args = Environment.GetCommandLineArgs();
            if (Array.IndexOf(args,"--nyaforge-surface-image") >= 0)
            {
                SurfaceRenderVerification.Write(prefab,RequiredArgument(args,"--nyaforge-surface-image"));
                checks.Add("Serialized surface prefab rendered to surface.png with a visible colored object.");
            }
        }
    }
}
