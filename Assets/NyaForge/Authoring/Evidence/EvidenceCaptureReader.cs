using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring.Paint;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring.Evidence
{
    public sealed class EvidenceCaptureImageRecord
    {
        readonly byte[] png;
        public EvidenceView View { get; }
        public string PngHash { get; }
        internal EvidenceCaptureImageRecord(EvidenceView view,string hash,byte[] bytes) { View=view;PngHash=hash;png=(byte[])bytes.Clone(); }
        public byte[] CopyPng()=>(byte[])png.Clone();
    }
    public sealed class EvidenceCaptureRecord
    {
        public string CaptureId { get; }
        public EvidenceRecord Metadata { get; }
        public IReadOnlyList<EvidenceCaptureImageRecord> Images { get; }
        internal EvidenceCaptureRecord(string id,EvidenceRecord metadata,EvidenceCaptureImageRecord[] images) { CaptureId=id;Metadata=metadata;Images=Array.AsReadOnly(images); }
    }
    public static class EvidenceCaptureReader
    {
        static void Require(bool condition)=>Checks.Require(condition,"INVALID_CAPTURE_MANIFEST","Capture manifest is malformed or inconsistent.");
        static void Shape(JToken t,string names) { Require(t is JObject);Require(((JObject)t).Properties().Select(p=>p.Name).OrderBy(n=>n).SequenceEqual(names.Split(' ').OrderBy(n=>n))); }
        static string Text(JToken t) { Require(t!=null && t.Type==JTokenType.String);return (string)t; }
        static int Integer(JToken t) { Require(t!=null && t.Type==JTokenType.Integer && int.TryParse(t.ToString(),out _));return (int)t; }
        static float Float(JToken t) { Require(t!=null && (t.Type==JTokenType.Float || t.Type==JTokenType.Integer));float f=(float)t;Checks.Finite(f);return f; }
        static Vec3 Point(JToken t) { Require(t is JArray && t.Count()==3);return new Vec3(Float(t[0]),Float(t[1]),Float(t[2])); }
        public static EvidenceCaptureRecord Read(string path)
        {
            path=Path.GetFullPath(path);string directory=Path.GetDirectoryName(path);var r=Storage.ReadObject(path);
            Shape(r,"schemaVersion kind captureId snapshotId metadataFile metadataHash captureStatus images");
            Require(Integer(r["schemaVersion"])==1 && Text(r["kind"])=="nyaforge.evidence.capture" && Text(r["captureStatus"])=="complete");
            string id=Text(r["captureId"]),snapshot=Text(r["snapshotId"]);Checks.Id(id);Checks.Id(snapshot);
            string metadataFile=Text(r["metadataFile"]),metadataHash=Text(r["metadataHash"]);Checks.HashText(metadataHash);Require(metadataFile==snapshot+".evidence.json");
            byte[] metadataBytes=Storage.ReadBounded(Path.Combine(directory,metadataFile),AuthoringLimits.MaxManifestBytes);
            Checks.Require(Checks.Hash(metadataBytes)==metadataHash,"HASH_MISMATCH","Capture metadata hash differs.");
            var metadata=EvidenceManifestReader.Read(metadataBytes);Require(metadata.SnapshotId==snapshot && metadata.State=="Ready");
            Require(r["images"] is JArray && r["images"].Count()>=1 && r["images"].Count()<=8);
            var images=new List<EvidenceCaptureImageRecord>();
            foreach(var entry in r["images"])
            {
                Shape(entry,"index file sha256 renderProfile width height camera backgroundRgba lighting");Require(Integer(entry["index"])==images.Count);
                string hash=Text(entry["sha256"]);Checks.HashText(hash);string file=Text(entry["file"]);
                Require(file==hash+".png" || file==Storage.CompactHashName(hash)+".png");
                Require(Text(entry["renderProfile"])=="authoring-surface-orthographic-v1" && Text(entry["lighting"])=="authoring-surface-view-shading-v1");
                Require(entry["backgroundRgba"] is JArray && entry["backgroundRgba"].Count()==4);
                var background=new[]{.04f,.06f,.08f,1f};for(int i=0;i<4;i++) Require(Float(entry["backgroundRgba"][i])==background[i]);
                var camera=entry["camera"];Shape(camera,"projection position target up size near far");Require(Text(camera["projection"])=="orthographic");
                var view=new EvidenceView(Integer(entry["width"]),Integer(entry["height"]),Point(camera["position"]),Point(camera["target"]),Point(camera["up"]),Float(camera["size"]),Float(camera["near"]),Float(camera["far"]));
                byte[] png=Storage.ReadBounded(Storage.HashFilePath(directory,hash,".png",false),PaintPngInput.MaxFileBytes);
                Checks.Require(Checks.Hash(png)==hash,"HASH_MISMATCH","Capture PNG hash differs.");var input=PaintPngInput.Read(png);Require(input.Width==view.Width && input.Height==view.Height);
                images.Add(new EvidenceCaptureImageRecord(view,hash,png));
            }
            return new EvidenceCaptureRecord(id,metadata,images.ToArray());
        }
    }
}
