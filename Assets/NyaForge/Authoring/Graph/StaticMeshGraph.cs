using System;
using System.Collections.Generic;
using System.Text;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Deterministic graph representation of the existing static editing profile.</summary>
    internal static class StaticMeshGraph
    {
        internal static string Id(string objectId, string role)
        {
            string hash = Checks.Hash(Encoding.UTF8.GetBytes(objectId + ":" + role));
            var bytes = new byte[16];
            for (int i = 0; i < bytes.Length; i++) bytes[i] = Convert.ToByte(hash.Substring(i * 2, 2), 16);
            return new Guid(bytes).ToString("D");
        }
        internal static AuthoringGraph Create(string objectId, MeshData mesh, RestTransform transform, bool enabled, IDictionary<int, Vec3> offsets)
        {
            string sourceId = Id(objectId, "source"), editId = Id(objectId, "edit"), outputId = Id(objectId, "output");
            var value = GraphMeshValue.Source(sourceId, mesh, transform);
            return new AuthoringGraph(Id(objectId, "graph"), new[]
            {
                GraphNode.Source(sourceId, mesh, transform),
                GraphNode.Edit(editId, enabled, offsets, value.SnapshotHash, value.DomainId), GraphNode.Output(outputId)
            }, new[] { new GraphEdge(sourceId, "mesh", editId, "mesh"), new GraphEdge(editId, "mesh", outputId, "mesh") }, outputId);
        }
    }
}
