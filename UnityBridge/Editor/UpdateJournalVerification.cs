using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        static void VerifyUpdateJournal(BakeImportResult imported)
        {
            var paths=ImportOwnership.ManagedPaths(imported.AssetDirectory);
            var before=Directory.GetFiles(imported.AssetDirectory).ToDictionary(p=>p,File.ReadAllBytes);
            string directory,newPath=imported.AssetDirectory+"/JournalProbe.asset";
            using(var journal=UpdateJournal.Create(imported.AssetDirectory,paths))
            {
                directory=journal.DirectoryPath;bool busy=false;
                try { UpdateJournal.Recover(directory); } catch(IOException) { busy=true; }
                Require(busy,"Live journal recovery was not excluded");
                journal.RegisterNew(newPath);AssetDatabase.CreateAsset(new Material(Shader.Find("Standard")),newPath);journal.ConfirmNew(newPath);
                var material=AssetDatabase.LoadAssetAtPath<Material>(imported.MaterialPaths[0]);material.name="Interrupted update";EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);
            } // Release process-local state without rollback; recovery reloads the journal from disk.
            Require(UpdateJournal.Pending(imported.AssetDirectory).Contains(directory),"Pending journal was not discoverable");
            string backup=Path.Combine(directory,"0.bin");byte[] original=File.ReadAllBytes(backup);File.WriteAllBytes(backup,new byte[]{0});
            var changed=Directory.GetFiles(imported.AssetDirectory).ToDictionary(p=>p,File.ReadAllBytes);bool rejected=false;
            try { UpdateJournal.Recover(directory); } catch(InvalidDataException) { rejected=true; }
            Require(rejected,"Corrupted backup was accepted");
            foreach(var pair in changed) Require(File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value),"Failed journal validation mutated an asset");
            File.WriteAllBytes(backup,original);UpdateJournal.Recover(directory);UpdateJournal.Recover(directory);
            Require(!UpdateJournal.Pending(imported.AssetDirectory).Contains(directory) && !File.Exists(newPath),"Recovery did not finish or remove owned new asset");
            Require(before.Keys.OrderBy(p=>p).SequenceEqual(Directory.GetFiles(imported.AssetDirectory).OrderBy(p=>p)),"Recovery file set differs");
            foreach(var pair in before) Require(File.ReadAllBytes(pair.Key).SequenceEqual(pair.Value),"Recovery bytes differ: "+pair.Key);
            Require(ImportOwnership.Inspect(imported.AssetDirectory).IsUnchanged,"Recovered ownership differs");
        }
    }
}
