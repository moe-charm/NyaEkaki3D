using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static void VerifyOwnership(BakeImportResult result,NyaForge.Authoring.BakeDocument source)
        {
            var first=ImportOwnership.Inspect(result.AssetDirectory);
            Require(first.IsUnchanged && first.DocumentId==source.DocumentId && first.ObjectId==source.ObjectId && first.Revision==source.DocumentRevision,"Initial ownership snapshot differs");
            string materialPath=result.MaterialPaths[0];byte[] original=File.ReadAllBytes(materialPath);
            var material=AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            string oldName=material.name;
            try
            {
                material.name="User edit probe";EditorUtility.SetDirty(material);
                Require(ImportOwnership.Inspect(result.AssetDirectory).Conflicts.Any(s=>s.StartsWith("Unsaved asset changes:")),"Unsaved material edit was not detected");
                AssetDatabase.SaveAssetIfDirty(material);
                Require(ImportOwnership.Inspect(result.AssetDirectory).Conflicts.Any(s=>s.StartsWith("Asset or importer settings changed:")),"Saved material edit was not detected");
            }
            finally
            {
                material.name=oldName;File.WriteAllBytes(materialPath,original);
                AssetDatabase.ImportAsset(materialPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            }
            Require(ImportOwnership.Inspect(result.AssetDirectory).IsUnchanged,"Restored asset did not match its ownership snapshot");
            string receiptPath=result.AssetDirectory+"/"+ImportOwnership.ReceiptName;string receipt=File.ReadAllText(receiptPath);
            try
            {
                var record=JsonUtility.FromJson<ImportOwnership.Receipt>(receipt);record.assets[0].relativePath="../unrelated.asset";
                File.WriteAllText(receiptPath,JsonUtility.ToJson(record));bool rejected=false;
                try { ImportOwnership.Inspect(result.AssetDirectory); } catch(InvalidDataException) { rejected=true; }
                Require(rejected,"Receipt traversal was not rejected");
            }
            finally { File.WriteAllText(receiptPath,receipt);AssetDatabase.ImportAsset(receiptPath,ImportAssetOptions.ForceSynchronousImport); }
        }
    }
}
