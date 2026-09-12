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
        [Serializable] sealed class VerifiedSlotMap
        {
            public int[] submeshSlots;
            public string[] materialNodeIds;
            public string[] materialPaths;
        }
        static void VerifyMaterials(string manifest,List<string> checks,List<string> folders)
        {
            var source=MultiMaterialBakeStore.Read(manifest);
            var result=BakeImporter.ImportMaterials(manifest);folders.Add(result.AssetDirectory);
            VerifyAssets(result,source.Geometry,checks,"multiple-materials",true);
            VerifyOwnership(result,source.Geometry);
            var preview=BakeUpdatePreview.Materials(manifest,result.AssetDirectory);
            Require(preview.Conflicts.Count==0 && preview.SameContent && preview.Source.OutputId==preview.Existing.OutputId,"Fresh output identity preview differs");
            string receiptPath=result.AssetDirectory+"/"+ImportOwnership.ReceiptName;string receiptText=File.ReadAllText(receiptPath);
            try
            {
                var receipt=JsonUtility.FromJson<ImportOwnership.Receipt>(receiptText);receipt.outputId=Guid.NewGuid().ToString("D");
                File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt));
                Require(BakeUpdatePreview.Materials(manifest,result.AssetDirectory).Conflicts.Any(c=>c.Contains("different document or output")),"Different output was matched");
                receipt=JsonUtility.FromJson<ImportOwnership.Receipt>(receiptText);receipt.revision++;
                File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt));
                Require(BakeUpdatePreview.Materials(manifest,result.AssetDirectory).Conflicts.Any(c=>c.Contains("older")),"Older revision was matched");
                receipt.version=1;File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt));
                Require(BakeUpdatePreview.Materials(manifest,result.AssetDirectory).Conflicts.Any(c=>c.Contains("identity is missing")),"Legacy identity was inferred");
            }
            finally { File.WriteAllText(receiptPath,receiptText);AssetDatabase.ImportAsset(receiptPath,ImportAssetOptions.ForceSynchronousImport); }
            checks.Add("Output identity: exact exported manifest binding and receipt target, unchanged preview, different output/older revision/legacy matching rejection.");
            checks.Add("Import ownership receipt: document/object/revision, GUID and file snapshots, unsaved/saved user edits, restored baseline and traversal rejection.");
            AssetDatabase.ImportAsset(result.PrefabPath,ImportAssetOptions.ForceUpdate);
            var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath);
            var materials=prefab.GetComponent<MeshRenderer>().sharedMaterials;
            Require(materials.Length==source.SubmeshSlots.Count,"Prefab material count differs");
            for(int i=0;i<materials.Length;i++)
            {
                var slot=source.Slots.Single(s=>s.Slot==source.SubmeshSlots[i]);
                var surface=slot.Surface;var actual=materials[i];var expected=StandardMaterialAdapter.Create(surface.Material);
                try
                {
                    Require(AssetDatabase.GetAssetPath(actual)==result.MaterialPaths[i],"Prefab material reference differs");
                    Require(actual.shader==expected.shader && actual.renderQueue==expected.renderQueue,"Slot shader/queue differs");
                    foreach(string property in new[]{"_TintLinear","_EmissionLinear"}) Require(actual.GetVector(property)==expected.GetVector(property),"Slot vector differs");
                    foreach(string property in new[]{"_Metallic","_Smoothness"}) Near(actual.GetFloat(property),expected.GetFloat(property),"Slot scalar differs");
                    if(surface.BaseColor!=null)
                    {
                        string path=AssetDatabase.GetAssetPath(actual.mainTexture);
                        Require(File.ReadAllBytes(path).SequenceEqual(surface.CopyPng()),"Slot PNG differs");
                        var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                        Require(importer.sRGBTexture && !importer.alphaIsTransparency,"Slot PNG settings differ");
                    }
                    else Require(actual.mainTexture==null,"Unexpected slot texture");
                    for(int j=0;j<i;j++)
                    {
                        var prior=source.Slots.Single(s=>s.Slot==source.SubmeshSlots[j]);
                        Require((materials[j]==actual)==(prior.MaterialNodeId==slot.MaterialNodeId),"Material sharing differs");
                        if(surface.BaseColor!=null && prior.Surface.BaseColor!=null)
                            Require((materials[j].mainTexture==actual.mainTexture)==(prior.Surface.PngHash==surface.PngHash),"Texture sharing differs");
                    }
                }
                finally { UnityEngine.Object.DestroyImmediate(expected); }
            }
            Require(File.Exists(result.AssetDirectory+"/MaterialSlots.json"),"Slot identity map missing");
            var map=JsonUtility.FromJson<VerifiedSlotMap>(File.ReadAllText(result.AssetDirectory+"/MaterialSlots.json"));
            Require(map.submeshSlots.SequenceEqual(source.SubmeshSlots) && map.materialPaths.SequenceEqual(result.MaterialPaths) &&
                map.materialNodeIds.SequenceEqual(source.SubmeshSlots.Select(s=>source.Slots.Single(slot=>slot.Slot==s).MaterialNodeId)),"Persisted slot identity map differs");
            string[] before=Directory.GetDirectories("Assets").OrderBy(p=>p).ToArray();string failed=null;bool rejected=false;
            try { BakeImporter.VerifyMaterialsFailure(manifest,folder=> { failed=folder;throw new InvalidOperationException("slot rollback probe"); }); }
            catch(InvalidOperationException e) { rejected=e.Message=="slot rollback probe"; }
            Require(rejected && failed!=null && !Directory.Exists(failed) && before.SequenceEqual(Directory.GetDirectories("Assets").OrderBy(p=>p)),"Slot rollback changed folder ownership");
            Require(AssetDatabase.LoadAssetAtPath<GameObject>(result.PrefabPath)!=null,"Rollback removed prior prefab");
            SurfaceRenderVerification.Write(prefab,Path.Combine(Path.GetDirectoryName(RequiredArgument(Environment.GetCommandLineArgs(),"--nyaforge-report")),"multi-material.png"),false);
            checks.Add("Multi-material GUI Bake: serialized submesh/material/PNG references and sharing, parameters, visible Prefab render, owned-folder rollback.");
            VerifyMaterialUpdate(manifest,result);
            VerifySharedImageUpdates(manifest,folders);
            VerifyGeometryUpdates(checks,folders);
            checks.Add("Geometry updates: actual Core graph exports grow from one to three submeshes then shrink to two; positions, indices, UVs and prefab bindings match, mesh/prefab GUIDs and unused materials retained.");
            checks.Add("Shared image updates: split into distinct payloads with one planned texture addition, rejoin with retained unused texture, material and texture GUIDs preserved.");
            checks.Add("Resource update plan: new material and texture destinations match application, unused previous assets retain GUIDs, failed additions restore the original file set.");
            VerifyUpdateJournal(result);
            checks.Add("Persistent update journal: live lock exclusion, disk-only reopen, corrupt backup rejected before writes, owned new asset cleanup, exact bytes/meta restoration and repeat recovery.");
            checks.Add("Multi-material update: retained mesh/prefab/material/texture GUIDs, changed material and PNG payloads, receipt commit; mesh/material/prefab/receipt failure checkpoints restore exact file bytes.");
            checks.Add("Selective prefab update: saved user root transform/name, renderer settings, child and Light preserved; managed mesh replacement rejected; legacy receipt stays conservative; user file is not adopted.");
        }
    }
}
