using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
internal static partial class Program
{
    static void RunEvidenceManifestTests()
    {
        Test("evidence metadata is deterministic explicit and preserves existing publications",()=>
        {
            var w=AuthoringWorkspace.CreateFixture();var s=EvaluatedSnapshot.Acquire(w);byte[] bytes=EvidenceManifestCodec.Write(s);
            True(bytes.SequenceEqual(EvidenceManifestCodec.Write(s)));var json=JObject.Parse(Encoding.UTF8.GetString(bytes));
            Equal(s.SnapshotId,(string)json["snapshotId"]);Equal(s.OutputContentHash,(string)json["outputContentHash"]);
            Equal("not_requested",(string)json["captureStatus"]);Equal("not_run",(string)json["validation"]["fit"]);Equal(0,((JArray)json["artifacts"]).Count);
            Equal(JTokenType.Null,json["sourceEpoch"].Type);Equal(JTokenType.Null,json["metrics"]["logicalVertexCount"].Type);
            string dir=Dir("evidence-metadata"),path=EvidenceStore.SaveMetadata(dir,s);True(bytes.SequenceEqual(File.ReadAllBytes(path)));Equal(path,EvidenceStore.SaveMetadata(dir,s));
            Ok(Execute(w,AuthoringOperation.TranslateVertices(new[]{0},new Vec3(.1f,0,0))));
            var next=EvaluatedSnapshot.Acquire(w);string other=EvidenceStore.SaveMetadata(dir,next);True(other!=path);True(bytes.SequenceEqual(File.ReadAllBytes(path)));
            File.WriteAllText(path,"existing different evidence");Expect("EVIDENCE_CONFLICT",()=>EvidenceStore.SaveMetadata(dir,s));Equal("existing different evidence",File.ReadAllText(path));
            True(!Directory.GetFiles(dir,"*.tmp").Any());
        });
        Test("empty and incomplete evidence manifests do not fabricate geometry or success",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();var empty=JObject.Parse(Encoding.UTF8.GetString(EvidenceManifestCodec.Write(EvaluatedSnapshot.Acquire(w))));
            Equal("Empty",(string)empty["state"]);Equal(JTokenType.Null,empty["metrics"].Type);Equal(JTokenType.Null,empty["objectId"].Type);
            string plane,edit;var graph=PlaneGraph(out plane,out edit);Ok(Execute(w,AuthoringOperation.AddGraph(graph)));Ok(Execute(w,AuthoringOperation.Disconnect(edit,"mesh")));
            var json=JObject.Parse(Encoding.UTF8.GetString(EvidenceManifestCodec.Write(EvaluatedSnapshot.Acquire(w))));
            Equal("Incomplete",(string)json["state"]);Equal(JTokenType.Null,json["meshHash"].Type);True((long)json["stalePreviewRevision"]>0);True(((JArray)json["diagnostics"]).Count>0);
        });
    }
}
