using System;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Inspection
{
    public static class AuthoringFaceReader
    {
        // At most 64 * 256 corners; prevents large n-gons from making an unbounded page.
        public const int MaxPageSize=64;
        public static JObject Read(AuthoringWorkspace workspace,string instance,string documentId,long revision,
            string nodeId,bool input,string snapshotHash,int offset,int count)
        {
            if(workspace==null) throw new ArgumentNullException(nameof(workspace));
            lock(workspace.Gate)
            {
                var value=InspectionMesh.Resolve(workspace,instance,documentId,revision,nodeId,input,snapshotHash);
                Checks.Require(value.Polygon!=null,"POLYGON_REQUIRED","This node has no polygon face identities.");
                var polygon=value.Polygon;
                Checks.Require(offset>=0 && offset<=polygon.Faces.Count && count>=1 && count<=MaxPageSize,"INVALID_PAGE","Invalid face page range.");
                var faces=new JArray(polygon.Faces.OrderBy(f=>f.Id).Skip(offset).Take(count).Select(f=>new JObject
                {
                    ["id"]=Id(f.Id),["materialSlot"]=f.Material,
                    ["corners"]=new JArray(f.Corners.Select(c=>new JObject
                    {
                        ["id"]=Id(c.Id),["vertexId"]=Id(c.VertexId),
                        ["uv0"]=c.Uv0.HasValue ? (JToken)new JArray(c.Uv0.Value.X,c.Uv0.Value.Y) : JValue.CreateNull()
                    }))
                }));
                int next=offset+faces.Count;
                return new JObject
                {
                    ["instanceId"]=instance,["documentId"]=documentId,["revision"]=revision,["stateHash"]=workspace.Document.StateHash,
                    ["graphId"]=workspace.Document.Objects[0].Graph.GraphId,["nodeId"]=nodeId,["port"]=input ? "input" : "output",
                    ["snapshotHash"]=value.SnapshotHash,["domainId"]=value.DomainId,["idKind"]="polygonFaceId",
                    ["total"]=polygon.Faces.Count,["offset"]=offset,["nextOffset"]=next<polygon.Faces.Count ? new JValue(next) : JValue.CreateNull(),["faces"]=faces
                };
            }
        }
        static string Id(ulong id)=>id.ToString(CultureInfo.InvariantCulture);
    }
}
