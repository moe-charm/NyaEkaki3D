using System;
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
        static void VerifyMaterialUpdate(string original,BakeImportResult imported)
        {
            string directory=Path.Combine(Path.GetDirectoryName(RequiredArgument(Environment.GetCommandLineArgs(),"--nyaforge-report")),"updated-bake");Directory.CreateDirectory(directory);
            foreach(string file in Directory.GetFiles(Path.GetDirectoryName(original),"*",SearchOption.AllDirectories))
            {
                string target=Path.Combine(directory,file.Substring(Path.GetDirectoryName(original).Length+1));Directory.CreateDirectory(Path.GetDirectoryName(target));File.Copy(file,target);
            }
            string manifest=Path.Combine(directory,MultiMaterialBakeStore.ManifestName);
            var json=JObject.Parse(File.ReadAllText(manifest));var slots=(JArray)json["slots"];
            Require(slots.Count==2,"Update fixture requires two slots");
            var firstHash=slots[0]["materialHash"].DeepClone();slots[0]["materialHash"]=slots[1]["materialHash"].DeepClone();slots[1]["materialHash"]=firstHash;
            var firstImage=slots[0]["baseColor"].DeepClone();slots[0]["baseColor"]=slots[1]["baseColor"].DeepClone();slots[1]["baseColor"]=firstImage;
            json["mesh"]["documentRevision"]=json["mesh"]["documentRevision"].Value<long>()+1;File.WriteAllText(manifest,json.ToString());
            string identityPath=manifest+BakeOutputIdentity.Suffix;var identity=JObject.Parse(File.ReadAllText(identityPath));
            identity["revision"]=json["mesh"]["documentRevision"].DeepClone();
            using(var hash=SHA256.Create()) identity["manifestHash"]=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(manifest))).Replace("-","").ToLowerInvariant();
            File.WriteAllText(identityPath,identity.ToString());
            string savedRootName=PrepareUserPrefab(imported);
            string[] files=Directory.GetFiles(imported.AssetDirectory).OrderBy(p=>p).ToArray();
            var before=files.ToDictionary(p=>p,File.ReadAllBytes);
            string meshGuid=AssetDatabase.AssetPathToGUID(imported.MeshPath),prefabGuid=AssetDatabase.AssetPathToGUID(imported.PrefabPath);
            var materialGuids=imported.MaterialPaths.Select(AssetDatabase.AssetPathToGUID).ToArray();
            var textureGuids=imported.MaterialPaths.Select(p=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<Material>(p).mainTexture))).ToArray();
            foreach(string failurePoint in new[]{"mesh","materials","prefab","receipt"})
            {
                bool rejected=false;
                try { BakeImporter.UpdateMaterialsCore(manifest,imported.AssetDirectory,phase=> { if(phase==failurePoint) throw new InvalidOperationException("update rollback probe"); }); }
                catch(InvalidOperationException e) { rejected=e.Message=="update rollback probe"; }
                Require(rejected,"Update failure checkpoint was not reached: "+failurePoint);
                Require(files.SequenceEqual(Directory.GetFiles(imported.AssetDirectory).OrderBy(p=>p)),"Rollback changed file set");
                foreach(var pair in before) Require(File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value),"Rollback bytes differ: "+pair.Key);
                Require(ImportOwnership.Inspect(imported.AssetDirectory).IsUnchanged,"Rollback ownership differs");
            }
            var updated=BakeImporter.UpdateMaterials(manifest,imported.AssetDirectory);
            Require(AssetDatabase.AssetPathToGUID(updated.MeshPath)==meshGuid && AssetDatabase.AssetPathToGUID(updated.PrefabPath)==prefabGuid,"Update replaced mesh/prefab GUID");
            Require(updated.MaterialPaths.Select(AssetDatabase.AssetPathToGUID).SequenceEqual(materialGuids),"Update replaced material GUID");
            Require(updated.MaterialPaths.Select(p=>AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<Material>(p).mainTexture))).SequenceEqual(textureGuids),"Update replaced texture GUID");
            var source=MultiMaterialBakeStore.Read(manifest);var materials=AssetDatabase.LoadAssetAtPath<GameObject>(updated.PrefabPath).GetComponent<MeshRenderer>().sharedMaterials;
            for(int i=0;i<materials.Length;i++)
            {
                var slot=source.Slots.Single(s=>s.Slot==source.SubmeshSlots[i]);
                Require(File.ReadAllBytes(AssetDatabase.GetAssetPath(materials[i].mainTexture)).SequenceEqual(slot.Surface.CopyPng()),"Updated image bytes differ");
                var expected=NyaForge.Rendering.StandardMaterialAdapter.Create(slot.Surface.Material);
                try { Require(materials[i].GetVector("_TintLinear")==expected.GetVector("_TintLinear"),"Updated material differs"); }
                finally { UnityEngine.Object.DestroyImmediate(expected); }
            }
            Require(BakeUpdatePreview.Materials(manifest,updated.AssetDirectory).SameContent && ImportOwnership.Inspect(updated.AssetDirectory).IsUnchanged,"Update receipt was not committed");
            VerifyUserPrefab(updated,savedRootName);
            // Replace one material identity: new material/texture are planned, old assets remain for external references.
            var changed=JObject.Parse(File.ReadAllText(manifest));changed["slots"][1]["materialNodeId"]=Guid.NewGuid().ToString("D");
            changed["mesh"]["documentRevision"]=changed["mesh"]["documentRevision"].Value<long>()+1;File.WriteAllText(manifest,changed.ToString());
            var nextIdentity=JObject.Parse(File.ReadAllText(identityPath));nextIdentity["revision"]=changed["mesh"]["documentRevision"].DeepClone();
            using(var hash=SHA256.Create()) nextIdentity["manifestHash"]=BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(manifest))).Replace("-","").ToLowerInvariant();
            File.WriteAllText(identityPath,nextIdentity.ToString());
            var plan=BakeUpdatePreview.Materials(manifest,updated.AssetDirectory).Plan;
            Require(plan.Entries.Count(e=>e.Action=="追加")==2 && plan.Entries.Any(e=>e.Path==updated.MaterialPaths[1] && e.Action=="保持"),"Material replacement plan lost additions/retention");
            var baseline=Directory.GetFiles(updated.AssetDirectory).ToDictionary(p=>p,File.ReadAllBytes);bool rolledBack=false;
            try { BakeImporter.UpdateMaterialsCore(manifest,updated.AssetDirectory,phase=> { if(phase=="materials") throw new InvalidOperationException("new assets rollback"); }); }
            catch(InvalidOperationException e) { rolledBack=e.Message=="new assets rollback"; }
            Require(rolledBack && baseline.Count==Directory.GetFiles(updated.AssetDirectory).Length,"New asset rollback left files");
            foreach(var pair in baseline) Require(File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value),"New asset rollback changed previous asset");
            var replacement=BakeImporter.UpdateMaterials(manifest,updated.AssetDirectory);
            Require(replacement.MaterialPaths.SequenceEqual(plan.Submeshes.Select(b=>b.MaterialPath)),"Applied material paths differ from preview");
            Require(File.Exists(updated.MaterialPaths[1]) && AssetDatabase.AssetPathToGUID(updated.MaterialPaths[1])==materialGuids[1],"Unused prior material was removed");
            Require(plan.Submeshes.Select(b=>b.TexturePath).SequenceEqual(replacement.MaterialPaths.Select(p=>AssetDatabase.GetAssetPath(AssetDatabase.LoadAssetAtPath<Material>(p).mainTexture))),"Applied image paths differ from preview");
            VerifyUserPrefab(replacement,savedRootName);
        }
    }
}
