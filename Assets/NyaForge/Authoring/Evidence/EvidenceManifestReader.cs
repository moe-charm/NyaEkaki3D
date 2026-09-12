using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring.Evidence
{
    public sealed class EvidenceRecord
    {
        readonly JObject data;
        public string SnapshotId=>(string)data["snapshotId"];
        public string State=>(string)data["state"];
        internal EvidenceRecord(JObject value) { data=(JObject)value.DeepClone(); }
        public JObject CopyMetadata()=>(JObject)data.DeepClone();
    }
    public static class EvidenceManifestReader
    {
        static void Require(bool value)=>Checks.Require(value,"INVALID_EVIDENCE","Evidence metadata is inconsistent or malformed.");
        static void Shape(JToken token,string names)
        {
            Require(token is JObject);var actual=((JObject)token).Properties().Select(p=>p.Name).OrderBy(s=>s).ToArray();
            Require(actual.SequenceEqual(names.Split(' ').OrderBy(s=>s)));
        }
        static string Str(JToken token) { Require(token!=null && token.Type==JTokenType.String);return (string)token; }
        static bool Null(JToken token)=>token!=null && token.Type==JTokenType.Null;
        static long Number(JToken token,long max=long.MaxValue)
        { Require(token!=null && token.Type==JTokenType.Integer && long.TryParse(token.ToString(),out _));long n=(long)token;Require(n>=0 && n<=max);return n; }
        static void Id(JToken token,bool nullable=false) { if(nullable && Null(token)) return;Checks.Id(Str(token)); }
        static void Hash(JToken token,bool nullable=false) { if(nullable && Null(token)) return;Checks.HashText(Str(token)); }
        static void Data(JToken token)
        {
            Require(token.Type!=JTokenType.Comment && token.Type!=JTokenType.Undefined && token.Type!=JTokenType.Constructor);
            if(token.Type==JTokenType.Float) { double n=(double)token;Require(!double.IsNaN(n) && !double.IsInfinity(n)); }
            foreach(var child in token.Children()) Data(child);
        }
        public static EvidenceRecord Read(byte[] bytes)
        {
            Checks.Require(bytes!=null && bytes.Length>0 && bytes.Length<=AuthoringLimits.MaxManifestBytes,"BUDGET_EXCEEDED","Evidence manifest byte budget exceeded.");
            try
            {
                using(var text=new StringReader(new UTF8Encoding(false,true).GetString(bytes)))
                using(var reader=new JsonTextReader(text) { MaxDepth=12,DateParseHandling=DateParseHandling.None })
                {
                    var root=JObject.Load(reader,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error,CommentHandling=CommentHandling.Load });
                    Require(!reader.Read());Data(root);Validate(root);return new EvidenceRecord(root);
                }
            }
            catch(JsonException e) { throw new AuthoringException("INVALID_EVIDENCE",e.Message); }
            catch(DecoderFallbackException e) { throw new AuthoringException("INVALID_EVIDENCE",e.Message); }
        }
        static void Validate(JObject r)
        {
            Shape(r,"schemaVersion kind snapshotId instanceId documentId documentRevision stateHash objectId graphId finalOutputNodeId target state finalEvaluationComplete stalePreviewRevision outputContentHash meshHash domainId space units sourceEpoch workspaceRevision pose captureStatus artifacts validation diagnostics metrics");
            Require(Number(r["schemaVersion"])==1 && Str(r["kind"])=="nyaforge.evidence.metadata");
            foreach(string name in new[]{"snapshotId","instanceId","documentId"}) Id(r[name]);
            long revision=Number(r["documentRevision"]);Hash(r["stateHash"]);
            foreach(string name in new[]{"objectId","graphId","finalOutputNodeId"}) Id(r[name],true);
            foreach(string name in new[]{"outputContentHash","meshHash","domainId"}) Hash(r[name],true);
            string state=Str(r["state"]);Require(new[]{"Empty","Ready","Faceless","Incomplete"}.Contains(state));
            Require(r["finalEvaluationComplete"].Type==JTokenType.Boolean);bool complete=(bool)r["finalEvaluationComplete"];
            if(!Null(r["stalePreviewRevision"])) Require(!complete && Number(r["stalePreviewRevision"])<revision);
            Require(Str(r["space"])=="avatar" && Str(r["units"])=="meters");
            foreach(string n in new[]{"sourceEpoch","workspaceRevision","pose"}) Require(Null(r[n]));
            Require(Str(r["captureStatus"])=="not_requested" && r["artifacts"] is JArray && !r["artifacts"].Any());
            Shape(r["validation"],"geometry fit pose");foreach(var p in ((JObject)r["validation"]).Properties()) Require(Str(p.Value)=="not_run");
            Shape(r["target"],"kind nodeId portId");string target=Str(r["target"]["kind"]);
            Require(new[]{"Final","NodeInput","NodeOutput"}.Contains(target));
            if(target=="Final") { Require(Null(r["target"]["nodeId"]) && Null(r["target"]["portId"]));if(state=="Ready" || state=="Faceless") Require(complete); }
            else { Id(r["target"]["nodeId"]);Require(Str(r["target"]["portId"])=="mesh" && state!="Empty"); }
            Require(r["diagnostics"] is JArray);Require(r["diagnostics"].Count()<=65536);
            foreach(var d in r["diagnostics"]) { Shape(d,"nodeId code message");Id(d["nodeId"]);Require(Str(d["code"]).Length>0);Str(d["message"]); }
            if(complete) Require(!r["diagnostics"].Any());
            bool empty=state=="Empty",valued=state=="Ready" || state=="Faceless";
            foreach(string n in new[]{"objectId","graphId","finalOutputNodeId"}) Require(Null(r[n])==empty);
            if(empty) Require(complete && target=="Final"); if(state=="Incomplete" && target=="Final") Require(!complete);
            Require(Null(r["metrics"])==!valued && Null(r["outputContentHash"])==!valued && Null(r["domainId"])==!valued);
            Require(Null(r["meshHash"])==(state!="Ready"));
            if(valued) Metrics(r["metrics"],state);
        }
        static void Metrics(JToken m,string state)
        {
            Shape(m,"renderVertexCount triangleCount renderSubmeshCount logicalVertexCount polygonFaceCount assignedMaterialCount boundsMin boundsMax boundsSource imageHashes");
            long vertices=Number(m["renderVertexCount"],int.MaxValue),triangles=Number(m["triangleCount"],int.MaxValue),submeshes=Number(m["renderSubmeshCount"],int.MaxValue);
            Number(m["assignedMaterialCount"],int.MaxValue);
            Require(Null(m["logicalVertexCount"])==Null(m["polygonFaceCount"]));
            if(!Null(m["logicalVertexCount"])) { Number(m["logicalVertexCount"],int.MaxValue);Number(m["polygonFaceCount"],int.MaxValue); }
            if(state=="Faceless") Require(vertices==0 && triangles==0 && submeshes==0 && !Null(m["logicalVertexCount"]) && Number(m["polygonFaceCount"])==0 && Str(m["boundsSource"])=="loose_points");
            else Require(vertices>=3 && triangles>=1 && submeshes>=1 && Str(m["boundsSource"])=="render_positions");
            bool bounds=state=="Ready" || Number(m["logicalVertexCount"])>0;
            Require(Null(m["boundsMin"])==!bounds && Null(m["boundsMax"])==!bounds);
            if(bounds)
            {
                foreach(string name in new[]{"boundsMin","boundsMax"}) { Require(m[name] is JArray && m[name].Count()==3);foreach(var n in m[name]) Require(n.Type==JTokenType.Float || n.Type==JTokenType.Integer); }
                for(int i=0;i<3;i++) Require((double)m["boundsMin"][i]<=(double)m["boundsMax"][i]);
            }
            Require(m["imageHashes"] is JArray);var hashes=m["imageHashes"].ToArray();Require(hashes.Length<=65536);foreach(var h in hashes) Hash(h);
            Require(hashes.Select(h=>(string)h).Distinct().Count()==hashes.Length);
        }
    }
}

