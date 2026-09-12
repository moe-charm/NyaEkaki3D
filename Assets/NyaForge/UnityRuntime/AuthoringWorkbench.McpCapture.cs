using System;
using System.Linq;
using System.Text;
using NyaForge.Authoring.Evidence;
using Newtonsoft.Json.Linq;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        JObject CaptureMcpEvidence()
        {
            var snapshot=EvaluatedSnapshot.Acquire(workspace);
            var images=EvidenceViewPresets.FiveViews(snapshot,256).Select(view=>EvidenceModelCapture.Capture(snapshot,view)).ToArray();
            var set=new EvidenceCaptureSet(images);
            return new JObject
            {
                ["metadata"]=JObject.Parse(Encoding.UTF8.GetString(EvidenceManifestCodec.Write(snapshot))),
                ["capture"]=JObject.Parse(Encoding.UTF8.GetString(EvidenceCaptureCodec.Write(set))),
                ["delivery"]="inline",
                ["images"]=new JArray(images.Select(image=>new JObject { ["mimeType"]="image/png",["sha256"]=image.PngHash,["data"]=Convert.ToBase64String(image.CopyPng()) }))
            };
        }
    }
}
