using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        static string AppendImportedMaterials(List<GraphNode> nodes, List<GraphEdge> edges, string meshNodeId, int submeshCount, IReadOnlyList<GlbMaterialSource> materials)
        {
            if (materials == null || materials.Count != submeshCount || materials.Count == 0) return meshNodeId;
            var slots = materials.Select(item => item.SubmeshIndex).Distinct().OrderBy(item => item).ToArray();
            if (!slots.SequenceEqual(Enumerable.Range(0, submeshCount))) return meshNodeId;
            string assignmentId = Guid.NewGuid().ToString("D");
            nodes.Add(GraphNode.AssignMaterials(assignmentId, slots));
            edges.Add(new GraphEdge(meshNodeId, "mesh", assignmentId, "mesh"));
            foreach (var material in materials)
            {
                string materialId = Guid.NewGuid().ToString("D");
                nodes.Add(GraphNode.StandardMaterial(materialId, material.Parameters));
                edges.Add(new GraphEdge(materialId, "material", assignmentId, GraphNode.MaterialSlotPort(material.SubmeshIndex)));
            }
            return assignmentId;
        }
    }
}
