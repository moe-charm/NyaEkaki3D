using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring.Evidence
{
    /// <summary>Explicit metadata-only contract; does not serialize engine objects or geometry payloads.</summary>
    public static class EvidenceManifestCodec
    {
        static JToken Text(string value)=>value==null ? JValue.CreateNull() : new JValue(value);
        static JToken Point(Vec3? value)=>value.HasValue ? new JArray(value.Value.X,value.Value.Y,value.Value.Z) : JValue.CreateNull();
        public static byte[] Write(EvaluatedSnapshot snapshot)
        {
            if(snapshot==null) throw new ArgumentNullException(nameof(snapshot));
            var m=snapshot.Metrics;
            var root=new JObject
            {
                ["schemaVersion"]=1,["kind"]="nyaforge.evidence.metadata",
                ["snapshotId"]=snapshot.SnapshotId,["instanceId"]=snapshot.InstanceId,
                ["documentId"]=snapshot.DocumentId,["documentRevision"]=snapshot.DocumentRevision,["stateHash"]=snapshot.StateHash,
                ["objectId"]=Text(snapshot.ObjectId=="" ? null : snapshot.ObjectId),["graphId"]=Text(snapshot.GraphId),["finalOutputNodeId"]=Text(snapshot.OutputNodeId),
                ["target"]=new JObject { ["kind"]=snapshot.Target.Kind.ToString(),["nodeId"]=Text(snapshot.Target.NodeId),["portId"]=Text(snapshot.Target.PortId) },
                ["state"]=snapshot.State.ToString(),["finalEvaluationComplete"]=snapshot.FinalEvaluationComplete,
                ["stalePreviewRevision"]=snapshot.StalePreviewRevision.HasValue ? new JValue(snapshot.StalePreviewRevision.Value) : JValue.CreateNull(),
                ["outputContentHash"]=Text(snapshot.OutputContentHash),["meshHash"]=Text(snapshot.Value?.Mesh?.ContentHash),["domainId"]=Text(snapshot.Value?.DomainId),
                ["space"]="avatar",["units"]="meters",
                ["sourceEpoch"]=JValue.CreateNull(),["workspaceRevision"]=JValue.CreateNull(),["pose"]=JValue.CreateNull(),
                ["captureStatus"]="not_requested",["artifacts"]=new JArray(),
                ["validation"]=new JObject { ["geometry"]="not_run",["fit"]="not_run",["pose"]="not_run" },
                ["diagnostics"]=new JArray(snapshot.Diagnostics.Select(d=>new JObject { ["nodeId"]=d.NodeId,["code"]=d.Code,["message"]=d.Message }))
            };
            root["metrics"]=m==null ? JValue.CreateNull() : new JObject
            {
                ["renderVertexCount"]=m.RenderVertexCount,["triangleCount"]=m.TriangleCount,["renderSubmeshCount"]=m.RenderSubmeshCount,
                ["logicalVertexCount"]=m.LogicalVertexCount.HasValue ? new JValue(m.LogicalVertexCount.Value) : JValue.CreateNull(),
                ["polygonFaceCount"]=m.PolygonFaceCount.HasValue ? new JValue(m.PolygonFaceCount.Value) : JValue.CreateNull(),
                ["assignedMaterialCount"]=m.AssignedMaterialCount,["boundsMin"]=Point(m.BoundsMin),["boundsMax"]=Point(m.BoundsMax),
                ["boundsSource"]=snapshot.Value.Mesh==null ? "loose_points" : "render_positions",["imageHashes"]=new JArray(m.ImageHashes)
            };
            return new UTF8Encoding(false,true).GetBytes(root.ToString(Formatting.Indented));
        }
    }
}
