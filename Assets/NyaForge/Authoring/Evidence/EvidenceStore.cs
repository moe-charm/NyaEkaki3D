using System.IO;
using System.Linq;
namespace NyaForge.Authoring.Evidence
{
    public static class EvidenceStore
    {
        /// <summary>Publishes a new snapshot manifest without replacing earlier evidence. Repeating the same bytes is idempotent.</summary>
        public static string SaveMetadata(string directory,EvaluatedSnapshot snapshot)
        {
            var bytes=EvidenceManifestCodec.Write(snapshot);directory=Storage.DirectoryPath(directory);Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,snapshot.SnapshotId+".evidence.json");
            using(Storage.Lock(directory))
            {
                if(File.Exists(path))
                {
                    var info=new FileInfo(path);
                    Checks.Require(info.Length==bytes.Length && File.ReadAllBytes(path).SequenceEqual(bytes),"EVIDENCE_CONFLICT","Existing evidence differs; it was not replaced.");
                }
                else Storage.AtomicWrite(path,bytes,false);
            }
            return path;
        }
    }
}

