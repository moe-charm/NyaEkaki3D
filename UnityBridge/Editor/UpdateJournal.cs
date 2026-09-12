using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>Durable backups outside Assets. Recovery validates all backups before writing.</summary>
    public sealed class UpdateJournal : IDisposable
    {
        [Serializable] sealed class Record
        {
            public int version=2;
            public StagingOwnership staging;
            public bool hasStaging;
            public string project,folder,folderGuid,state="pending";
            public Backup[] backups;
            public NewAsset[] created=Array.Empty<NewAsset>();
        }
        [Serializable] sealed class Backup { public string name,guid,hash,metaHash; }
        [Serializable] sealed class NewAsset { public string name,guid=""; }
        public static string Root=>Path.GetFullPath("Library/NyaForgeUpdates");
        public string DirectoryPath { get; }
        readonly Record record;
        FileStream lease;
        UpdateJournal(string directory,Record record,FileStream lease) { DirectoryPath=directory;this.record=record;this.lease=lease; }
        internal static UpdateJournal Create(string folder,IEnumerable<string> paths,string stagingFolder=null)
        {
            ValidateFolder(folder);
            if(Pending(folder).Length!=0) throw new IOException("An unfinished update must be recovered first.");
            string directory=Path.Combine(Root,Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
            var lease=Lock(directory);
            try
            {
                var backups=new List<Backup>();
                foreach(string path in paths.Append(folder+"/"+ImportOwnership.ReceiptName).Distinct())
                {
                    string name=Relative(folder,path);byte[] bytes=File.ReadAllBytes(path),meta=File.ReadAllBytes(path+".meta");
                    string guid=AssetDatabase.AssetPathToGUID(path);
                    if(!Guid.TryParseExact(guid,"N",out _)) throw new IOException("Owned asset has no GUID.");
                    int index=backups.Count;WriteDurable(Path.Combine(directory,index+".bin"),bytes);WriteDurable(Path.Combine(directory,index+".meta.bin"),meta);
                    backups.Add(new Backup { name=name,guid=guid,hash=Hash(bytes),metaHash=Hash(meta) });
                }
                var record=new Record { project=Path.GetFullPath(Application.dataPath),folder=folder,folderGuid=AssetDatabase.AssetPathToGUID(folder),backups=backups.ToArray(),
                    hasStaging=stagingFolder!=null,staging=stagingFolder==null ? null : StagingOwnership.Capture(stagingFolder) };
                var journal=new UpdateJournal(directory,record,lease);journal.Save();return journal;
            }
            catch { lease.Dispose();throw; }
        }
        internal void RegisterNew(string path)
        {
            string name=Relative(record.folder,path);
            if(File.Exists(path) || File.Exists(path+".meta") || record.backups.Any(b=>b.name==name) || record.created.Any(c=>c.name==name)) throw new IOException("New asset path already exists.");
            record.created=record.created.Concat(new[]{new NewAsset { name=name }}).ToArray();Save();
        }
        internal void ConfirmNew(string path)
        {
            var entry=record.created.Single(c=>c.name==Relative(record.folder,path));string guid=AssetDatabase.AssetPathToGUID(path);
            if(!Guid.TryParseExact(guid,"N",out _)) throw new IOException("New asset identity unavailable.");
            entry.guid=guid;Save();
        }
        internal void Commit() { record.state="committed";Save(); }
        void Save()
        {
            string path=Path.Combine(DirectoryPath,"journal.json"),temporary=Path.Combine(DirectoryPath,"journal.tmp");
            WriteDurable(temporary,Encoding.UTF8.GetBytes(JsonUtility.ToJson(record,true)));
            if(File.Exists(path)) ReplaceWithRetry(temporary,path);else File.Move(temporary,path);
        }
        public void Dispose() { lease?.Dispose();lease=null; }
        public static string[] Pending(string folder=null)
        {
            if(!Directory.Exists(Root)) return Array.Empty<string>();
            return Directory.GetDirectories(Root).Where(d=>
            {
                if(!File.Exists(Path.Combine(d,"journal.json"))) return false;
                var record=Read(d);return (record.state=="pending" || record.state=="committed" && record.staging!=null && Directory.Exists(record.staging.path)) && (folder==null || record.folder==folder);
            }).ToArray();
        }
        public static void Recover(string directory)=>Recover(directory,false);
        public static string TargetFolder(string directory)=>Read(directory).folder;
        internal static void Recover(string directory,bool discardCurrentDirty,Action<string> checkpoint=null)
        {
            using(var lease=Lock(ValidateDirectory(directory)))
            {
                var record=Read(directory);
                if(record.project!=Path.GetFullPath(Application.dataPath)) throw new IOException("Recovery project differs.");
                if(record.state=="recovered") return;
                if(record.state=="committed") { record.staging?.Delete(record.folder);return; }
                ValidateFolder(record.folder);
                if(record.project!=Path.GetFullPath(Application.dataPath) || AssetDatabase.AssetPathToGUID(record.folder)!=record.folderGuid) throw new IOException("Recovery project or folder identity differs.");
                var bytes=new List<(string path,byte[] data,byte[] meta)>();var names=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                for(int i=0;i<record.backups.Length;i++)
                {
                    var item=record.backups[i];if(item==null) throw new InvalidDataException("Missing backup entry.");ValidateName(item.name);
                    if(!names.Add(item.name) || !Guid.TryParseExact(item.guid,"N",out _)) throw new InvalidDataException("Duplicate or invalid recovery asset.");
                    string path=record.folder+"/"+item.name;
                    byte[] data=File.ReadAllBytes(Path.Combine(directory,i+".bin")),meta=File.ReadAllBytes(Path.Combine(directory,i+".meta.bin"));
                    if(Hash(data)!=item.hash || Hash(meta)!=item.metaHash) throw new InvalidDataException("Recovery backup hash differs.");
                    if(File.Exists(path+".meta") && AssetDatabase.AssetPathToGUID(path)!=item.guid) throw new IOException("Recovery asset GUID differs: "+path);
                    CheckDirty(path,discardCurrentDirty);bytes.Add((path,data,meta));
                }
                foreach(var item in record.created)
                {
                    if(item==null) throw new InvalidDataException("Missing new asset entry.");ValidateName(item.name);
                    if(!names.Add(item.name)) throw new InvalidDataException("Duplicate new recovery asset.");
                    string path=record.folder+"/"+item.name;
                    if(!File.Exists(path) && !File.Exists(path+".meta")) continue;
                    if(!Guid.TryParseExact(item.guid,"N",out _) || AssetDatabase.AssetPathToGUID(path)!=item.guid) throw new IOException("New asset ownership was not confirmed; review required: "+path);
                    CheckDirty(path,discardCurrentDirty);
                }
                foreach(var item in record.created)
                {
                    string path=record.folder+"/"+item.name;
                    if((File.Exists(path) || File.Exists(path+".meta")) && !AssetDatabase.DeleteAsset(path)) throw new IOException("Cannot remove new recovery asset: "+path);
                }
                foreach(var item in bytes) { RestoreFile(directory,item.path,item.data);RestoreFile(directory,item.path+".meta",item.meta);checkpoint?.Invoke(item.path); }
                foreach(var item in bytes) AssetDatabase.ImportAsset(item.path,ImportAssetOptions.ForceUpdate|ImportAssetOptions.ForceSynchronousImport);
                record.staging?.Delete(record.folder);
                var journal=new UpdateJournal(directory,record,null);record.state="recovered";journal.Save();
            }
        }
        static void CheckDirty(string path,bool discard)
        {
            if(discard) return;var asset=AssetDatabase.LoadMainAssetAtPath(path);
            if(asset!=null && EditorUtility.IsDirty(asset) || path.EndsWith(".prefab") && PrefabManagedBindings.IsDirty(path)) throw new IOException("Save or discard current edits before recovery: "+path);
        }
        static string ValidateDirectory(string directory)
        {
            directory=Path.GetFullPath(directory);
            if(Path.GetDirectoryName(directory)!=Root || !Guid.TryParseExact(Path.GetFileName(directory),"N",out _)) throw new InvalidDataException("Invalid journal directory.");
            return directory;
        }
        static Record Read(string directory)
        {
            directory=ValidateDirectory(directory);string path=Path.Combine(directory,"journal.json");
            if(new FileInfo(path).Length>1024*1024) throw new InvalidDataException("Journal exceeds budget.");
            var record=JsonUtility.FromJson<Record>(File.ReadAllText(path));
            if(record==null || (record.version!=1 && record.version!=2) || record.backups==null || record.backups.Length==0 || record.backups.Length>1024 || record.created==null || record.created.Length>1024 ||
                (record.state!="pending" && record.state!="committed" && record.state!="recovered")) throw new InvalidDataException("Invalid journal record.");
            if(record.version==1 || !record.hasStaging) record.staging=null;
            else if(record.staging==null) throw new InvalidDataException("Missing staging ownership.");
            return record;
        }
        static FileStream Lock(string directory)=>new FileStream(Path.Combine(directory,"active.lock"),FileMode.OpenOrCreate,FileAccess.ReadWrite,FileShare.None);
        static string Relative(string folder,string path)
        {
            if(!path.StartsWith(folder+"/",StringComparison.Ordinal)) throw new InvalidDataException("Asset is outside the import folder.");
            string name=path.Substring(folder.Length+1);ValidateName(name);return name;
        }
        static void ValidateName(string name)
        {
            if(string.IsNullOrEmpty(name) || name=="." || name==".." || name.IndexOfAny(new[]{'/','\\',':'})>=0 || name.EndsWith(".meta",StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Invalid recovery asset name.");
        }
        static void ValidateFolder(string folder)
        {
            if(string.IsNullOrEmpty(folder) || !folder.StartsWith("Assets/",StringComparison.Ordinal) || folder.Contains("\\") || folder.Contains(":") || folder.Split('/').Any(p=>p=="" || p=="." || p=="..") || !AssetDatabase.IsValidFolder(folder)) throw new InvalidDataException("Invalid recovery folder.");
        }
        static string Hash(byte[] bytes) { using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-","").ToLowerInvariant(); }
        static void RestoreFile(string directory,string path,byte[] bytes)
        {
            if(File.Exists(path) && Hash(File.ReadAllBytes(path))==Hash(bytes)) return;
            string temporary=Path.Combine(directory,"restore.tmp");WriteDurable(temporary,bytes);
            if(!File.Exists(path)) { File.Move(temporary,path);return; }
            try { ReplaceWithRetry(temporary,path); }
            catch(IOException) when(!path.EndsWith(".meta",StringComparison.OrdinalIgnoreCase))
            {
                // Unity may keep readers without delete-sharing. The durable journal remains
                // authoritative if this overwrite is interrupted; unchanged GUID metadata is untouched.
                using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.Read))
                { stream.Write(bytes,0,bytes.Length);stream.Flush(true); }
            }
        }
        static void ReplaceWithRetry(string temporary,string path)
        {
            for(int attempt=0;;attempt++)
            {
                try { File.Replace(temporary,path,null);return; }
                catch(IOException) when(attempt<5) { System.Threading.Thread.Sleep(20*(1<<attempt)); }
            }
        }
        static void WriteDurable(string path,byte[] bytes) { using(var stream=new FileStream(path,FileMode.Create,FileAccess.Write,FileShare.None)) { stream.Write(bytes,0,bytes.Length);stream.Flush(true); } }
    }
}
