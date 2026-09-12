using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NyaForge.UnityBridge.Editor
{
    public static partial class BridgeBatch
    {
        [Serializable] sealed class CrashFile { public string name,hash; }
        [Serializable] sealed class CrashMarker
        {
            public string project,folder,journal,phase;
            public int crashedPid;
            public CrashFile[] files;
            public string[] assetFolders;
        }
        static string CrashArgument(string name)=>RequiredArgument(Environment.GetCommandLineArgs(),name);
        static void RequireCrashSandbox()
        {
            string expected=Path.GetFullPath(Application.dataPath);
            Require(File.Exists("crash-test-authorized.txt") && File.ReadAllText("crash-test-authorized.txt")==expected,"Crash test must run in its explicitly authorized disposable receiver");
        }
        static string CrashHash(string path)
        { using(var hash=SHA256.Create()) return BitConverter.ToString(hash.ComputeHash(File.ReadAllBytes(path))).Replace("-","").ToLowerInvariant(); }
        public static void CrashDuringUpdate()
        {
            try
            {
                RequireCrashSandbox();string phase=CrashArgument("--crash-phase");
                Require(phase=="materials" || phase=="prefab" || phase=="receipt","Unsupported crash checkpoint");
                var imported=BakeImporter.ImportMaterials(CrashArgument("--crash-source"));
                var marker=new CrashMarker { project=Path.GetFullPath(Application.dataPath),folder=imported.AssetDirectory,phase=phase,crashedPid=Process.GetCurrentProcess().Id,
                    assetFolders=Directory.GetDirectories("Assets").OrderBy(p=>p).ToArray(),files=Directory.GetFiles(imported.AssetDirectory).Select(p=>new CrashFile { name=Path.GetFileName(p),hash=CrashHash(p) }).ToArray() };
                BakeImporter.UpdateMaterialsCore(CrashArgument("--crash-update"),imported.AssetDirectory,current=>
                {
                    if(current!=phase) return;
                    marker.journal=UpdateJournal.Pending(imported.AssetDirectory).Single();
                    byte[] bytes=Encoding.UTF8.GetBytes(JsonUtility.ToJson(marker,true));
                    using(var stream=new FileStream(CrashArgument("--crash-marker"),FileMode.CreateNew,FileAccess.Write,FileShare.Read)) { stream.Write(bytes,0,bytes.Length);stream.Flush(true); }
                    Process.GetCurrentProcess().Kill(); // Only this explicitly authorized test process; no finally/rollback.
                });
                throw new InvalidOperationException("Crash checkpoint was not reached");
            }
            catch(Exception error) { UnityEngine.Debug.LogException(error);EditorApplication.Exit(1); }
        }
        public static void RecoverAfterCrash()=>RecoverProbe(false);
        public static void CrashDuringRecovery()=>RecoverProbe(true);
        static void RecoverProbe(bool interrupt)
        {
            try
            {
                RequireCrashSandbox();var marker=JsonUtility.FromJson<CrashMarker>(File.ReadAllText(CrashArgument("--crash-marker")));
                Require(marker.project==Path.GetFullPath(Application.dataPath) && marker.crashedPid!=Process.GetCurrentProcess().Id,"Recovery is not a fresh receiver process");
                Require(UpdateJournal.Pending(marker.folder).Contains(marker.journal),"Interrupted update journal missing after restart");
                Require(marker.files.Any(f=>CrashHash(marker.folder+"/"+f.name)!=f.hash),"Crash fixture did not mutate any asset");
                if(interrupt)
                {
                    UpdateJournal.Recover(marker.journal,false,path=>
                    {
                        if(!path.EndsWith(".mat",StringComparison.Ordinal)) return;
                        Require(CrashHash(path)==marker.files.Single(f=>f.name==Path.GetFileName(path)).hash,"Recovery checkpoint did not restore a material");
                        Require(marker.files.Any(f=>CrashHash(marker.folder+"/"+f.name)!=f.hash),"Recovery checkpoint is already complete");
                        byte[] checkpoint=Encoding.UTF8.GetBytes(JsonUtility.ToJson(new CrashResult { phase=marker.phase,crashedPid=Process.GetCurrentProcess().Id,journal=marker.journal },true));
                        using(var stream=new FileStream(CrashArgument("--crash-marker")+".recovery-stop.json",FileMode.CreateNew,FileAccess.Write,FileShare.Read)) { stream.Write(checkpoint,0,checkpoint.Length);stream.Flush(true); }
                        Process.GetCurrentProcess().Kill();
                    });
                    throw new InvalidOperationException("Recovery interruption checkpoint was not reached");
                }
                UpdateJournal.Recover(marker.journal);UpdateJournal.Recover(marker.journal);
                Require(marker.assetFolders.SequenceEqual(Directory.GetDirectories("Assets").OrderBy(p=>p)),"Recovery did not clean staging or changed unrelated folders");
                Require(marker.files.Select(f=>f.name).OrderBy(p=>p).SequenceEqual(Directory.GetFiles(marker.folder).Select(Path.GetFileName).OrderBy(p=>p)),"Restart recovery file set differs");
                foreach(var file in marker.files) Require(CrashHash(marker.folder+"/"+file.name)==file.hash,"Restart recovery bytes/meta differ: "+file.name);
                Require(ImportOwnership.Inspect(marker.folder).IsUnchanged,"Restart recovery ownership differs");
                File.WriteAllText(CrashArgument("--crash-result"),JsonUtility.ToJson(new CrashResult { passed=true,phase=marker.phase,crashedPid=marker.crashedPid,recoveredPid=Process.GetCurrentProcess().Id,journal=marker.journal },true));
                UnityEngine.Debug.Log("NYAFORGE_CRASH_RECOVERY_PASSED");EditorApplication.Exit(0);
            }
            catch(Exception error) { UnityEngine.Debug.LogException(error);EditorApplication.Exit(1); }
        }
        [Serializable] sealed class CrashResult { public bool passed;public string phase,journal;public int crashedPid,recoveredPid; }
    }
}
