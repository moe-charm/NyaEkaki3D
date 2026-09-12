using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring.Evidence
{
    public static class EvidenceCaptureCodec
    {
        static JArray Point(Vec3 p)=>new JArray(p.X,p.Y,p.Z);
        public static byte[] Write(EvidenceCaptureSet capture)
        {
            if(capture==null) throw new ArgumentNullException(nameof(capture));
            var metadata=EvidenceManifestCodec.Write(capture.Snapshot);
            var root=new JObject
            {
                ["schemaVersion"]=1,["kind"]="nyaforge.evidence.capture",["captureId"]=capture.CaptureId,
                ["snapshotId"]=capture.Snapshot.SnapshotId,["metadataFile"]=capture.Snapshot.SnapshotId+".evidence.json",["metadataHash"]=Checks.Hash(metadata),
                ["captureStatus"]="complete",
                ["images"]=new JArray(capture.Images.Select((image,index)=>new JObject
                {
                    ["index"]=index,["file"]=image.PngHash+".png",["sha256"]=image.PngHash,["renderProfile"]=image.RenderProfile,
                    ["width"]=image.View.Width,["height"]=image.View.Height,
                    ["camera"]=new JObject { ["projection"]="orthographic",["position"]=Point(image.View.Position),["target"]=Point(image.View.Target),["up"]=Point(image.View.Up),["size"]=image.View.OrthographicSize,["near"]=image.View.Near,["far"]=image.View.Far },
                    ["backgroundRgba"]=new JArray(.04f,.06f,.08f,1),["lighting"]="authoring-surface-view-shading-v1"
                }))
            };
            return new UTF8Encoding(false,true).GetBytes(root.ToString(Formatting.Indented));
        }
    }
}
