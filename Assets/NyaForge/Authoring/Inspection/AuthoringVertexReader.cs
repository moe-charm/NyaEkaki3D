using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Inspection
{
    public static class AuthoringVertexReader
    {
        public const int MaxPageSize = 1024;

        // Every page is pinned to both the document revision and the evaluated input.
        public static JObject Read(AuthoringWorkspace workspace, string instance, string documentId,
            long revision, string nodeId, bool input, string snapshotHash, int offset, int count)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            lock (workspace.Gate)
            {
                var value=InspectionMesh.Resolve(workspace,instance,documentId,revision,nodeId,input,snapshotHash);
                var doc = workspace.Document;
                Checks.Require(offset >= 0 && count >= 1 && count <= MaxPageSize, "INVALID_PAGE", "Invalid vertex page range.");
                bool polygon = value.Polygon != null;
                int total = polygon ? value.Polygon.Vertices.Count : value.Mesh.VertexCount;
                Checks.Require(offset <= total, "INVALID_PAGE", "Offset exceeds vertex count.");
                var vertices = new JArray();
                if (polygon)
                {
                    foreach (var pair in value.Polygon.Vertices.OrderBy(p => p.Key).Skip(offset).Take(count))
                        vertices.Add(Vertex(new JValue(pair.Key.ToString(System.Globalization.CultureInfo.InvariantCulture)), pair.Value.Position, value.Transform.ToAvatarPoint(pair.Value.Position)));
                }
                else
                {
                    for (int id = offset; id < total && id - offset < count; id++)
                    {
                        var position = value.Mesh.Positions[id];
                        vertices.Add(Vertex(new JValue(id), position, value.Transform.ToAvatarPoint(position)));
                    }
                }
                int next = offset + vertices.Count;
                return new JObject
                {
                    ["instanceId"] = instance, ["documentId"] = documentId, ["revision"] = revision,
                    ["stateHash"] = doc.StateHash, ["graphId"] = doc.Objects[0].Graph.GraphId,
                    ["nodeId"] = nodeId, ["port"] = input ? "input" : "output",
                    ["snapshotHash"] = value.SnapshotHash, ["domainId"] = value.DomainId,
                    ["idKind"] = polygon ? "polygonVertexId" : "meshVertexIndex",
                    ["units"] = "meters", ["total"] = total, ["offset"] = offset,
                    ["nextOffset"] = next < total ? new JValue(next) : JValue.CreateNull(), ["vertices"] = vertices
                };
            }
        }

        static JObject Vertex(JToken id, Vec3 rest, Vec3 avatar) => new JObject
        {
            ["id"] = id, ["restPosition"] = new JArray(rest.X, rest.Y, rest.Z),
            ["avatarPosition"] = new JArray(avatar.X, avatar.Y, avatar.Z)
        };
    }
}
