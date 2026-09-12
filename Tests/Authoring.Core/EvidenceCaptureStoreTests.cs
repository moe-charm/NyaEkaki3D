using System;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
using NyaForge.Authoring.Paint;
internal static partial class Program
{
    static void RunEvidenceCaptureStoreTests()
    {
        Test("capture publication binds metadata camera and PNG and preserves conflicting evidence",()=>
        {
            var snapshot=EvaluatedSnapshot.Acquire(AuthoringWorkspace.CreateFixture());
            var view=new EvidenceView(32,32,new Vec3(0,0,1),new Vec3(),new Vec3(0,1,0),.3f);
            var png=PaintPng.Encode(new PaintImage(32,32,new Rgba32(20,30,40)));var image=new EvidenceImage(snapshot,view,png);
            var inputs=new[]{image};var capture=new EvidenceCaptureSet(inputs);inputs[0]=null;Equal(1,capture.Images.Count);
            string directory=Dir("evidence-capture-store"),path=EvidenceCaptureStore.Save(directory,capture);var manifest=JObject.Parse(File.ReadAllText(path));
            var reopened=EvidenceCaptureReader.Read(path);Equal(capture.CaptureId,reopened.CaptureId);True(reopened.Images[0].CopyPng().SequenceEqual(png));
            var copied=reopened.Images[0].CopyPng();copied[0]=0;Equal((byte)137,reopened.Images[0].CopyPng()[0]);
            Equal(snapshot.SnapshotId,(string)manifest["snapshotId"]);Equal(image.PngHash,(string)manifest["images"][0]["sha256"]);
            Equal(snapshot.SnapshotId,EvidenceManifestReader.Read(File.ReadAllBytes(Path.Combine(directory,(string)manifest["metadataFile"]))).SnapshotId);
            Equal(Checks.Hash(File.ReadAllBytes(Path.Combine(directory,(string)manifest["metadataFile"]))),(string)manifest["metadataHash"]);
            True(png.SequenceEqual(File.ReadAllBytes(Path.Combine(directory,image.PngHash+".png"))));Equal(path,EvidenceCaptureStore.Save(directory,capture));
            var wrong=(JObject)manifest.DeepClone();wrong["images"][0]["file"]="../outside.png";File.WriteAllText(path,wrong.ToString());Expect("INVALID_CAPTURE_MANIFEST",()=>EvidenceCaptureReader.Read(path));
            wrong=(JObject)manifest.DeepClone();wrong["images"][0]["width"]=64;File.WriteAllText(path,wrong.ToString());Expect("INVALID_CAPTURE_MANIFEST",()=>EvidenceCaptureReader.Read(path));File.WriteAllText(path,manifest.ToString());
            var next=new EvidenceCaptureSet(new[]{image});File.WriteAllText(Path.Combine(directory,image.PngHash+".png"),"do not replace");
            Expect("HASH_MISMATCH",()=>EvidenceCaptureReader.Read(path));Expect("EVIDENCE_CONFLICT",()=>EvidenceCaptureStore.Save(directory,next));True(!File.Exists(Path.Combine(directory,next.CaptureId+".capture.json")));True(File.Exists(path));
            Equal("do not replace",File.ReadAllText(Path.Combine(directory,image.PngHash+".png")));
            var other=EvaluatedSnapshot.Acquire(AuthoringWorkspace.CreateFixture());
            Expect("CAPTURE_SNAPSHOT_MISMATCH",()=>new EvidenceCaptureSet(new[]{image,new EvidenceImage(other,view,png)}));
            Expect("INVALID_CAPTURE_SET",()=>new EvidenceCaptureSet(Enumerable.Repeat(image,9)));
            Expect("INVALID_CAPTURE_IMAGE",()=>new EvidenceImage(snapshot,new EvidenceView(64,32,new Vec3(0,0,1),new Vec3(),new Vec3(0,1,0),1),png));
        });
    }
}

