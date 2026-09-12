using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BakeImporter
    {
        public static BakeImportResult UpdateMaterials(string manifestPath,string importFolder)
            =>UpdateMaterialsCore(manifestPath,importFolder,null);
        internal static BakeImportResult UpdateMaterialsCore(string manifestPath,string folder,Action<string> checkpoint)
        {
            var preview=BakeUpdatePreview.Materials(manifestPath,folder);
            if(preview.Conflicts.Count!=0) throw new InvalidOperationException(string.Join("\n",preview.Conflicts));
            var managed=new HashSet<string>(ImportOwnership.ManagedPaths(folder),StringComparer.Ordinal);
            string meshPath=folder+"/Mesh.asset",prefabPath=folder+"/StaticMesh.prefab",mapPath=folder+"/MaterialSlots.json";
            if(!managed.Contains(meshPath) || !managed.Contains(prefabPath) || !managed.Contains(mapPath)) throw new InvalidOperationException("This import does not have the multi-material resource layout.");
            var source=MultiMaterialBakeStore.Read(manifestPath);
            var identity=BakeOutputIdentity.Read(manifestPath,source.Geometry);
            if(identity==null || identity.ManifestHash!=preview.Source.ManifestHash) throw new IOException("Source changed during update preparation.");
            BakeImportResult staged=null;
            StagingOwnership stagingOwnership=null;
            try
            {
                staged=ImportMaterials(manifestPath);
                stagingOwnership=StagingOwnership.Capture(staged.AssetDirectory);
                var stagedIdentity=ImportOwnership.Inspect(staged.AssetDirectory);
                if(stagedIdentity.ManifestHash!=identity.ManifestHash) throw new IOException("Source changed while staging.");
                checkpoint?.Invoke("prepared");
                var current=BakeUpdatePreview.Materials(manifestPath,folder);
                if(current.Conflicts.Count!=0 || current.Source.ManifestHash!=identity.ManifestHash || current.Existing.ManifestHash!=preview.Existing.ManifestHash)
                    throw new IOException("Source or destination changed before update commit.");
                if(current.Plan.ComparisonKey!=preview.Plan.ComparisonKey) throw new IOException("Resource destinations changed before commit.");
                using(var transaction=new AssetUpdateTransaction(folder,managed,staged.AssetDirectory))
                {
                    var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
                    if(mesh==null) throw new IOException("Owned mesh is unavailable.");
                    EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<Mesh>(staged.MeshPath),mesh);
                    AssetDatabase.SaveAssetIfDirty(mesh);checkpoint?.Invoke("mesh");
                    var outputMaterials=new Material[source.SubmeshSlots.Count];var paths=new string[outputMaterials.Length];
                    var materialCache=new Dictionary<string,Material>();var textureCache=new Dictionary<string,Texture2D>();
                    for(int i=0;i<outputMaterials.Length;i++)
                    {
                        var slot=source.Slots.Single(s=>s.Slot==source.SubmeshSlots[i]);
                        string path=current.Plan.Submeshes[i].MaterialPath;paths[i]=path;
                        if(materialCache.TryGetValue(slot.MaterialNodeId,out var shared)) { outputMaterials[i]=shared;continue; }
                        Material material;
                        if(managed.Contains(path)) material=AssetDatabase.LoadAssetAtPath<Material>(path);
                        else
                        {
                            transaction.RegisterNew(path);material=new Material(AssetDatabase.LoadAssetAtPath<Material>(staged.MaterialPaths[i]));
                            AssetDatabase.CreateAsset(material,path);
                            transaction.ConfirmNew(path);
                        }
                        if(material==null) throw new IOException("Owned material is unavailable: "+path);
                        Texture2D texture=null;
                        if(slot.Surface.BaseColor!=null && !textureCache.TryGetValue(slot.Surface.PngHash,out texture))
                        {
                            string oldTexture=current.Plan.Submeshes[i].TexturePath;
                            if(!managed.Contains(oldTexture))
                            {
                                transaction.RegisterNew(oldTexture);
                                string stageTexture=AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<Material>(staged.MaterialPaths[i]).mainTexture);
                                if(!AssetDatabase.CopyAsset(stageTexture,oldTexture)) throw new IOException("Cannot create updated texture.");
                                transaction.ConfirmNew(oldTexture);
                            }
                            File.WriteAllBytes(oldTexture,slot.Surface.CopyPng());
                            AssetDatabase.ImportAsset(oldTexture,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                            texture=AssetDatabase.LoadAssetAtPath<Texture2D>(oldTexture);textureCache.Add(slot.Surface.PngHash,texture);
                        }
                        EditorUtility.CopySerialized(AssetDatabase.LoadAssetAtPath<Material>(staged.MaterialPaths[i]),material);
                        material.mainTexture=texture;EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
                        materialCache.Add(slot.MaterialNodeId,material);outputMaterials[i]=material;
                    }
                    checkpoint?.Invoke("materials");
                    var root=PrefabUtility.LoadPrefabContents(prefabPath);
                    try
                    {
                        var filter=root.GetComponent<MeshFilter>();var renderer=root.GetComponent<MeshRenderer>();
                        if(filter==null || renderer==null) throw new IOException("Managed prefab components are missing.");
                        filter.sharedMesh=mesh;renderer.sharedMaterials=outputMaterials;
                        PrefabUtility.SaveAsPrefabAsset(root,prefabPath,out bool success);
                        if(!success) throw new IOException("Updated prefab could not be saved.");
                    }
                    finally { PrefabUtility.UnloadPrefabContents(root); }
                    File.WriteAllText(mapPath,JsonUtility.ToJson(new ImportedSlotMap { submeshSlots=source.SubmeshSlots.ToArray(),
                        materialNodeIds=source.SubmeshSlots.Select(s=>source.Slots.Single(slot=>slot.Slot==s).MaterialNodeId).ToArray(),materialPaths=paths },true));
                    AssetDatabase.ImportAsset(mapPath,ImportAssetOptions.ForceSynchronousImport);
                    checkpoint?.Invoke("prefab");
                    ImportOwnership.Capture(folder,source.Geometry,identity,managed.Concat(transaction.Created));
                    checkpoint?.Invoke("receipt");transaction.Commit();
                    return new BakeImportResult { AssetDirectory=folder,MeshPath=meshPath,PrefabPath=prefabPath,MaterialPaths=paths };
                }
            }
            finally { stagingOwnership?.Delete(folder); }
        }
    }
}
