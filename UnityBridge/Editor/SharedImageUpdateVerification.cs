using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static void VerifySharedImageUpdates(string original,List<string> folders)
        {
            string directory=Path.Combine(Path.GetDirectoryName(RequiredArgument(Environment.GetCommandLineArgs(),"--nyaforge-report")),"shared-image-bake");
            Directory.CreateDirectory(directory);
            string sourceDirectory=Path.GetDirectoryName(original);
            foreach(string file in Directory.GetFiles(sourceDirectory,"*",SearchOption.AllDirectories))
            {
                string target=Path.Combine(directory,file.Substring(sourceDirectory.Length+1));
                Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target);
            }
            string manifest=Path.Combine(directory,MultiMaterialBakeStore.ManifestName);
            var json=JObject.Parse(File.ReadAllText(manifest));var slots=(JArray)json["slots"];
            Require(slots.Count==2,"Shared image fixture requires two materials");
            var independentImage=slots[1]["baseColor"].DeepClone();
            Require(slots[0]["baseColor"].ToString()!=independentImage.ToString(),"Shared image fixture requires different source images");
            slots[1]["baseColor"]=slots[0]["baseColor"].DeepClone();
            PublishUpdateFixture(manifest,json);
            var imported=BakeImporter.ImportMaterials(manifest);folders.Add(imported.AssetDirectory);
            string[] materials=imported.MaterialPaths.ToArray();
            string[] materialGuids=materials.Select(AssetDatabase.AssetPathToGUID).ToArray();
            string TexturePath(int index)=>AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<Material>(materials[index]).mainTexture);
            string sharedPath=TexturePath(0),sharedGuid=AssetDatabase.AssetPathToGUID(sharedPath);
            Require(sharedPath==TexturePath(1),"Initial shared image was duplicated");

            slots[1]["baseColor"]=independentImage.DeepClone();PublishUpdateFixture(manifest,json);
            var splitPlan=BakeUpdatePreview.Materials(manifest,imported.AssetDirectory).Plan;
            Require(splitPlan.Entries.Count(e=>e.Action=="追加")==1,"Splitting one image must add exactly one texture");
            BakeImporter.UpdateMaterials(manifest,imported.AssetDirectory);
            string splitPath=TexturePath(1),splitGuid=AssetDatabase.AssetPathToGUID(splitPath);
            Require(TexturePath(0)==sharedPath && splitPath!=sharedPath,"Split image bindings differ");
            VerifySharedImagePayloads(manifest,imported);
            Require(splitPlan.Submeshes.Select(b=>b.TexturePath).SequenceEqual(new[]{TexturePath(0),TexturePath(1)}),"Split destinations differ from plan");

            slots[1]["baseColor"]=slots[0]["baseColor"].DeepClone();PublishUpdateFixture(manifest,json);
            var joinPlan=BakeUpdatePreview.Materials(manifest,imported.AssetDirectory).Plan;
            Require(!joinPlan.Entries.Any(e=>e.Action=="追加") && joinPlan.Entries.Any(e=>e.Path==splitPath && e.Action=="保持"),"Rejoining must retain the unused texture");
            BakeImporter.UpdateMaterials(manifest,imported.AssetDirectory);
            Require(TexturePath(0)==sharedPath && TexturePath(1)==sharedPath,"Rejoined image is not shared");
            Require(AssetDatabase.AssetPathToGUID(sharedPath)==sharedGuid && AssetDatabase.AssetPathToGUID(splitPath)==splitGuid,"Image transition replaced or deleted a GUID");
            Require(materials.Select(AssetDatabase.AssetPathToGUID).SequenceEqual(materialGuids),"Image transition replaced material GUIDs");
            VerifySharedImagePayloads(manifest,imported);
        }

        static void PublishUpdateFixture(string manifest,JObject json)
        {
            json["mesh"]["documentRevision"]=json["mesh"]["documentRevision"].Value<long>()+1;
            File.WriteAllText(manifest,json.ToString());
            string identityPath=manifest+BakeOutputIdentity.Suffix;
            var identity=JObject.Parse(File.ReadAllText(identityPath));identity["revision"]=json["mesh"]["documentRevision"].DeepClone();
            using(var hash=SHA256.Create()) identity["manifestHash"]=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(manifest))).Replace("-","").ToLowerInvariant();
            File.WriteAllText(identityPath,identity.ToString());
        }

        static void VerifySharedImagePayloads(string manifest,BakeImportResult imported)
        {
            var source=MultiMaterialBakeStore.Read(manifest);
            var materials=AssetDatabase.LoadAssetAtPath<GameObject>(imported.PrefabPath).GetComponent<MeshRenderer>().sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var slot=source.Slots.Single(s=>s.Slot==source.SubmeshSlots[i]);
                Require(File.ReadAllBytes(AssetDatabase.GetAssetPath(materials[i].mainTexture)).SequenceEqual(slot.Surface.CopyPng()),"Shared/split image payload differs");
            }
            Require(ImportOwnership.Inspect(imported.AssetDirectory).IsUnchanged && BakeUpdatePreview.Materials(manifest,imported.AssetDirectory).SameContent,"Shared image update receipt differs");
        }
    }
}
