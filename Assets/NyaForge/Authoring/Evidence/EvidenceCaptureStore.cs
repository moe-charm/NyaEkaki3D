using System;
using System.IO;
using System.Linq;
namespace NyaForge.Authoring.Evidence
{
    public static class EvidenceCaptureStore
    {
        public static string Save(string directory,EvidenceCaptureSet capture)
        {
            if(capture==null) throw new ArgumentNullException(nameof(capture));
            byte[] manifest=EvidenceCaptureCodec.Write(capture),metadata=EvidenceManifestCodec.Write(capture.Snapshot);
            directory=Storage.DirectoryPath(directory);Directory.CreateDirectory(directory);
            string path=Path.Combine(directory,capture.CaptureId+".capture.json");
            using(Storage.Lock(directory))
            {
                // The capture manifest is the publication point. Prior content is never replaced.
                Publish(Path.Combine(directory,capture.Snapshot.SnapshotId+".evidence.json"),metadata);
                foreach(var image in capture.Images) Publish(Storage.HashFilePath(directory,image.PngHash,".png",true),image.CopyPng());
                Publish(path,manifest);
            }
            return path;
        }
        static void Publish(string path,byte[] bytes)
        {
            if(File.Exists(path))
            {
                Checks.Require(new FileInfo(path).Length==bytes.Length && File.ReadAllBytes(path).SequenceEqual(bytes),"EVIDENCE_CONFLICT","Existing evidence differs and was preserved.");
            }
            else Storage.AtomicWrite(path,bytes,false);
        }
    }
}
