using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    /// <summary>Persisted evidence of the assets produced by one import. Inspection never writes assets.</summary>
    public static class ImportOwnership
    {
        public const string ReceiptName="NyaForgeImport.json";
        [Serializable] internal sealed class Receipt
        {
            public int version=3;
            public string outputId;
            public string outputKind;
            public string manifestHash;
            public string documentId;
            public string objectId;
            public long revision;
            public Entry[] assets;
        }
        [Serializable] internal sealed class Entry
        {
            public string relativePath;
            public string guid;
            public string contentHash;
            public string metaHash;
            public string prefabBindings;
        }
        public sealed class Inspection
        {
            public string DocumentId { get; }
            public string ObjectId { get; }
            public long Revision { get; }
            public string OutputId { get; }
            public string OutputKind { get; }
            public string ManifestHash { get; }
            public IReadOnlyList<string> Conflicts { get; }
            public bool IsUnchanged=>Conflicts.Count==0;
            internal Inspection(Receipt receipt,List<string> conflicts)
            { DocumentId=receipt.documentId;ObjectId=receipt.objectId;Revision=receipt.revision;OutputId=receipt.outputId;OutputKind=receipt.outputKind;ManifestHash=receipt.manifestHash;Conflicts=conflicts.AsReadOnly(); }
        }
        internal static void Capture(string folder,NyaForge.Authoring.BakeDocument source,NyaForge.Authoring.BakeOutputIdentity identity,IEnumerable<string> managedPaths=null)
        {
            folder=ValidateFolder(folder);
            string path=folder+"/"+ReceiptName;
            if(File.Exists(path) && managedPaths==null) throw new IOException("An import ownership receipt already exists.");
            var entries=(managedPaths ?? Directory.GetFiles(folder,"*",SearchOption.AllDirectories))
                .Where(p=>!p.EndsWith(".meta",StringComparison.OrdinalIgnoreCase)).OrderBy(p=>p,StringComparer.Ordinal)
                .Select(p=>
                {
                    string asset=p.Replace('\\','/');string guid=AssetDatabase.AssetPathToGUID(asset);
                    if(string.IsNullOrEmpty(guid)) throw new IOException("Generated asset has no GUID: "+asset);
                    return new Entry { relativePath=asset.Substring(folder.Length+1),guid=guid,contentHash=Hash(asset),metaHash=Hash(asset+".meta"),
                        prefabBindings=asset.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase) ? PrefabManagedBindings.Fingerprint(asset) : "" };
                }).ToArray();
            File.WriteAllText(path,JsonUtility.ToJson(new Receipt { documentId=source.DocumentId,objectId=source.ObjectId,revision=source.DocumentRevision,
                outputId=identity?.OutputId ?? "",outputKind=identity?.OutputKind ?? "",manifestHash=identity?.ManifestHash ?? "",assets=entries },true));
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceSynchronousImport);
        }
        internal static string[] ManagedPaths(string folder)
        {
            Inspect(folder);
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(folder+"/"+ReceiptName));
            return receipt.assets.Select(a=>folder+"/"+a.relativePath).ToArray();
        }
        public static Inspection Inspect(string folder)
        {
            folder=ValidateFolder(folder);
            string path=folder+"/"+ReceiptName;
            if(new FileInfo(path).Length>1024*1024) throw new InvalidDataException("Import receipt exceeds budget.");
            var receipt=JsonUtility.FromJson<Receipt>(File.ReadAllText(path));
            if(receipt==null || (receipt.version<1 || receipt.version>3) || !Guid.TryParseExact(receipt.documentId,"D",out _) ||
                !Guid.TryParseExact(receipt.objectId,"D",out _) || receipt.revision<0 || receipt.assets==null || receipt.assets.Length==0 || receipt.assets.Length>1024)
                throw new InvalidDataException("Invalid import ownership receipt.");
            if(receipt.version==1) { receipt.outputId="";receipt.outputKind="";receipt.manifestHash=""; }
            else if(string.IsNullOrEmpty(receipt.outputId))
            {
                if(!string.IsNullOrEmpty(receipt.outputKind) || !string.IsNullOrEmpty(receipt.manifestHash)) throw new InvalidDataException("Incomplete output identity.");
            }
            else if(!Guid.TryParseExact(receipt.outputId,"D",out _) || !IsHash(receipt.manifestHash) ||
                (receipt.outputKind!="graph-output" && receipt.outputKind!="static-object")) throw new InvalidDataException("Invalid output identity.");
            var conflicts=new List<string>();var paths=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var guids=new HashSet<string>();
            foreach(var entry in receipt.assets)
            {
                if(entry==null || string.IsNullOrEmpty(entry.relativePath) || entry.relativePath.Contains("\\") || entry.relativePath.Contains(":") ||
                    entry.relativePath.Split('/').Any(p=>p=="" || p=="." || p=="..") || !paths.Add(entry.relativePath) ||
                    !Guid.TryParseExact(entry.guid,"N",out _) || !guids.Add(entry.guid) || !IsHash(entry.contentHash) || !IsHash(entry.metaHash))
                    throw new InvalidDataException("Invalid owned asset entry.");
                string asset=folder+"/"+entry.relativePath;
                bool selectivePrefab=receipt.version>=3 && asset.EndsWith(".prefab",StringComparison.OrdinalIgnoreCase);
                if(selectivePrefab && !IsHash(entry.prefabBindings)) throw new InvalidDataException("Missing managed prefab binding identity.");
                if(!File.Exists(asset) || !File.Exists(asset+".meta")) { conflicts.Add("Missing asset: "+asset);continue; }
                if(AssetDatabase.GUIDToAssetPath(entry.guid)!=asset || AssetDatabase.AssetPathToGUID(asset)!=entry.guid)
                    conflicts.Add("Asset identity changed: "+asset);
                bool contentChanged=Hash(asset)!=entry.contentHash;
                if(selectivePrefab)
                {
                    try { contentChanged=PrefabManagedBindings.Fingerprint(asset)!=entry.prefabBindings; }
                    catch(InvalidDataException) { contentChanged=true; }
                    if(PrefabManagedBindings.IsDirty(asset)) conflicts.Add("Unsaved prefab changes: "+asset);
                }
                if(contentChanged || Hash(asset+".meta")!=entry.metaHash)
                    conflicts.Add("Asset or importer settings changed: "+asset);
                var loaded=AssetDatabase.LoadMainAssetAtPath(asset);
                if(loaded!=null && EditorUtility.IsDirty(loaded)) conflicts.Add("Unsaved asset changes: "+asset);
            }
            return new Inspection(receipt,conflicts);
        }
        static bool IsHash(string text)=>text!=null && text.Length==64 && text.All(c=>c>='0' && c<='9' || c>='a' && c<='f');
        static string Hash(string path)
        {
            using(var stream=File.OpenRead(path)) using(var hash=SHA256.Create())
                return BitConverter.ToString(hash.ComputeHash(stream)).Replace("-","").ToLowerInvariant();
        }
        static string ValidateFolder(string folder)
        {
            if(string.IsNullOrEmpty(folder) || !folder.StartsWith("Assets/",StringComparison.Ordinal) || folder.Contains("\\") || folder.Contains(":") ||
                folder.Split('/').Any(p=>p=="" || p=="." || p=="..") || !AssetDatabase.IsValidFolder(folder))
                throw new ArgumentException("Select an existing import folder below Assets.");
            return folder;
        }
    }
}
