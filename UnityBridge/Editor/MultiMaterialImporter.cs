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
    public static partial class BakeImporter
    {
        public static BakeImportResult ImportMaterials(string manifestPath,string assetParent="Assets",Transform sceneParent=null,bool createSceneInstance=false)
        {
            var source=MultiMaterialBakeStore.Read(manifestPath);
            return ImportValidated(source.Geometry,null,assetParent,sceneParent,createSceneInstance,multiple:source,identity:BakeOutputIdentity.Read(manifestPath,source.Geometry));
        }
        internal static BakeImportResult VerifyMaterialsFailure(string manifestPath,Action<string> checkpoint)
        {
            var source=MultiMaterialBakeStore.Read(manifestPath);
            return ImportValidated(source.Geometry,null,"Assets",null,false,verificationCheckpoint:checkpoint,multiple:source,identity:BakeOutputIdentity.Read(manifestPath,source.Geometry));
        }
        [Serializable] sealed class ImportedSlotMap
        {
            public int[] submeshSlots;
            public string[] materialNodeIds;
            public string[] materialPaths;
        }
        static void ImportSlotMaterials(string folder,MultiMaterialBakeDocument source,Material[] owned,string[] paths)
        {
            var slots=source.Slots.ToDictionary(s=>s.Slot);
            var materials=new Dictionary<string,Material>();
            var textures=new Dictionary<string,Texture2D>();
            var ids=new string[owned.Length];
            for(int i=0;i<owned.Length;i++)
            {
                var slot=slots[source.SubmeshSlots[i]];var surface=slot.Surface;ids[i]=slot.MaterialNodeId;
                paths[i]=folder+"/Material-"+slot.MaterialNodeId+".mat";
                if(materials.TryGetValue(slot.MaterialNodeId,out var shared)) { owned[i]=shared;continue; }
                var material=StandardMaterialAdapter.Create(surface.Material);
                owned[i]=material;
                material.name="NyaForge Slot "+slot.Slot;
                if(surface.BaseColor!=null)
                {
                    if(!textures.TryGetValue(surface.PngHash,out var texture))
                    {
                        texture=ImportBaseColor(folder,surface.CopyPng(),surface.BaseColor.Width,surface.BaseColor.Height,"BaseColor-"+surface.PngHash);
                        textures.Add(surface.PngHash,texture);
                    }
                    material.mainTexture=texture;
                }
                AssetDatabase.CreateAsset(material,paths[i]);materials.Add(slot.MaterialNodeId,material);
            }
            string mapPath=folder+"/MaterialSlots.json";
            File.WriteAllText(AbsoluteAssetPath(mapPath),JsonUtility.ToJson(new ImportedSlotMap {
                submeshSlots=source.SubmeshSlots.ToArray(),materialNodeIds=ids,materialPaths=paths },true));
            AssetDatabase.ImportAsset(mapPath,ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
