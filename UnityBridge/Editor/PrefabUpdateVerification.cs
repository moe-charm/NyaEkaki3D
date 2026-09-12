using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static string PrepareUserPrefab(BakeImportResult imported)
        {
            var root=PrefabUtility.LoadPrefabContents(imported.PrefabPath);
            try
            {
                root.name="User configured item";root.transform.localPosition=new Vector3(.3f,.4f,.5f);
                root.transform.localRotation=Quaternion.Euler(10,20,30);root.transform.localScale=Vector3.one*2;
                root.GetComponent<MeshRenderer>().enabled=false;
                var child=new GameObject("User attachment");child.transform.SetParent(root.transform,false);child.transform.localPosition=new Vector3(1,2,3);
                var light=child.AddComponent<Light>();light.range=7;light.intensity=.35f;
                PrefabUtility.SaveAsPrefabAsset(root,imported.PrefabPath,out bool success);Require(success,"User prefab save failed");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Require(ImportOwnership.Inspect(imported.AssetDirectory).IsUnchanged,"Unmanaged prefab additions were treated as managed changes");
            string receiptPath=imported.AssetDirectory+"/"+ImportOwnership.ReceiptName,text=File.ReadAllText(receiptPath);
            try
            {
                var receipt=JsonUtility.FromJson<ImportOwnership.Receipt>(text);receipt.version=2;File.WriteAllText(receiptPath,JsonUtility.ToJson(receipt));
                Require(!ImportOwnership.Inspect(imported.AssetDirectory).IsUnchanged,"Old receipt inferred selective prefab ownership");
            }
            finally { File.WriteAllText(receiptPath,text);AssetDatabase.ImportAsset(receiptPath,ImportAssetOptions.ForceSynchronousImport); }
            byte[] saved=File.ReadAllBytes(imported.PrefabPath);
            root=PrefabUtility.LoadPrefabContents(imported.PrefabPath);
            try { root.GetComponent<MeshFilter>().sharedMesh=null;PrefabUtility.SaveAsPrefabAsset(root,imported.PrefabPath); }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            Require(!ImportOwnership.Inspect(imported.AssetDirectory).IsUnchanged,"Managed mesh replacement was not rejected");
            File.WriteAllBytes(imported.PrefabPath,saved);AssetDatabase.ImportAsset(imported.PrefabPath,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
            Require(ImportOwnership.Inspect(imported.AssetDirectory).IsUnchanged,"Restored managed bindings differ");
            string note=imported.AssetDirectory+"/UserNotes.txt";File.WriteAllText(note,"preserve this user file");AssetDatabase.ImportAsset(note,ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<GameObject>(imported.PrefabPath).name;
        }
        static void VerifyUserPrefab(BakeImportResult imported,string savedRootName)
        {
            var root=AssetDatabase.LoadAssetAtPath<GameObject>(imported.PrefabPath);
            Require(root.name==savedRootName && root.transform.localPosition==new Vector3(.3f,.4f,.5f) && root.transform.localScale==Vector3.one*2,"User root settings changed");
            Require(Quaternion.Angle(root.transform.localRotation,Quaternion.Euler(10,20,30))<.01f && !root.GetComponent<MeshRenderer>().enabled,"User rotation/renderer settings changed");
            var child=root.transform.Find("User attachment");Require(child!=null && child.localPosition==new Vector3(1,2,3),"User child was lost");
            Require(child.GetComponent<Light>()!=null && child.GetComponent<Light>().range==7 && child.GetComponent<Light>().intensity==.35f,"User component settings changed");
            string note=imported.AssetDirectory+"/UserNotes.txt";
            Require(File.ReadAllText(note)=="preserve this user file" && !ImportOwnership.ManagedPaths(imported.AssetDirectory).Contains(note),"User file was changed or adopted");
        }
    }
}
