using System;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
internal static partial class Program
{
    static void RunEvidenceReaderTests()
    {
        Test("evidence reader rejects incompatible claims and malformed structure",()=>
        {
            var snapshot=EvaluatedSnapshot.Acquire(AuthoringWorkspace.CreateFixture());var bytes=EvidenceManifestCodec.Write(snapshot);
            var record=EvidenceManifestReader.Read(bytes);Equal(snapshot.SnapshotId,record.SnapshotId);var copy=record.CopyMetadata();copy["state"]="Empty";Equal("Ready",record.State);
            var parsed=JObject.Parse(Encoding.UTF8.GetString(bytes));
            void Reject(Action<JObject> change) { var j=(JObject)parsed.DeepClone();change(j);Expect("INVALID_EVIDENCE",()=>EvidenceManifestReader.Read(Encoding.UTF8.GetBytes(j.ToString()))); }
            Reject(j=>j["captureStatus"]="captured");Reject(j=>j["validation"]["fit"]="pass");Reject(j=>j["schemaVersion"]="1");Reject(j=>j["extra"]=1);
            Reject(j=>j["metrics"]["triangleCount"]=-1);Reject(j=>j["metrics"]["boundsMin"]=new JArray(999,999,999));
            Reject(j=>j["meshHash"]=JValue.CreateNull());Reject(j=>j["state"]="Empty");Reject(j=>j["finalEvaluationComplete"]=false);
            Expect("INVALID_EVIDENCE",()=>EvidenceManifestReader.Read(Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(bytes)+" {}")));
            Expect("INVALID_EVIDENCE",()=>EvidenceManifestReader.Read(Encoding.UTF8.GetBytes("{\"schemaVersion\":1,\"schemaVersion\":1}")));
            Expect("INVALID_EVIDENCE",()=>EvidenceManifestReader.Read(new byte[]{255}));
        });
        Test("evidence reader accepts empty and diagnosed incomplete manifests",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();Equal("Empty",EvidenceManifestReader.Read(EvidenceManifestCodec.Write(EvaluatedSnapshot.Acquire(w))).State);
            string plane,edit;var graph=PlaneGraph(out plane,out edit);Ok(Execute(w,AuthoringOperation.AddGraph(graph)));Ok(Execute(w,AuthoringOperation.Disconnect(edit,"mesh")));
            Equal("Incomplete",EvidenceManifestReader.Read(EvidenceManifestCodec.Write(EvaluatedSnapshot.Acquire(w))).State);
        });
    }
}
