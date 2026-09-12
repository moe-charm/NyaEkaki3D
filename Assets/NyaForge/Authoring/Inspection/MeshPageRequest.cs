using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Inspection
{
    public sealed class MeshPageRequest
    {
        public string DocumentId { get; private set; }
        public long Revision { get; private set; }
        public string NodeId { get; private set; }
        public bool Input { get; private set; }
        public string SnapshotHash { get; private set; }
        public int Offset { get; private set; }
        public int Count { get; private set; }

        public static MeshPageRequest Parse(JObject value, bool faces=false)
        {
            Checks.Require(value != null && value.Properties().Select(p=>p.Name).OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(new[]{"count","documentId","nodeId","offset","port","revision","snapshotHash"}), "INVALID_REQUEST", "Unexpected vertex page fields.");
            foreach(var field in new[]{"documentId","nodeId","port","snapshotHash"})
                Checks.Require(value[field].Type==JTokenType.String,"INVALID_REQUEST","Expected string: "+field);
            foreach(var field in new[]{"revision","offset","count"})
                Checks.Require(value[field].Type==JTokenType.Integer,"INVALID_REQUEST","Expected integer: "+field);
            Checks.Require(Guid.TryParseExact((string)value["documentId"],"D",out _) && Guid.TryParseExact((string)value["nodeId"],"D",out _),"INVALID_REQUEST","Invalid document or node ID.");
            string port=(string)value["port"];
            Checks.Require(port=="input" || port=="output","INVALID_REQUEST","Expected input or output port.");
            Checks.Require(long.TryParse(value["revision"].ToString(),out var revision) && revision>=0 && int.TryParse(value["offset"].ToString(),out var offset) && offset>=0 && int.TryParse(value["count"].ToString(),out var count) && count>=1 && count<=AuthoringVertexReader.MaxPageSize,"INVALID_PAGE","Invalid revision or page range.");
            Checks.Require(!faces || (int)value["count"]<=AuthoringFaceReader.MaxPageSize,"INVALID_PAGE","Face page exceeds limit.");
            return new MeshPageRequest { DocumentId=(string)value["documentId"],Revision=(long)value["revision"],NodeId=(string)value["nodeId"],Input=port=="input",SnapshotHash=(string)value["snapshotHash"],Offset=(int)value["offset"],Count=(int)value["count"] };
        }

        public JObject Read(AuthoringWorkspace workspace,string instance)=>AuthoringVertexReader.Read(workspace,instance,DocumentId,Revision,NodeId,Input,SnapshotHash,Offset,Count);
        public JObject ReadFaces(AuthoringWorkspace workspace,string instance)=>AuthoringFaceReader.Read(workspace,instance,DocumentId,Revision,NodeId,Input,SnapshotHash,Offset,Count);
    }
}

