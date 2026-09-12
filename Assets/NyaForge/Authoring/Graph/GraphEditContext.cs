using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Graph
{
    public sealed class GraphEditContext
    {
        public string GraphId { get; }
        public string NodeId { get; }
        public string InputSnapshot { get; }
        public string DomainId { get; }
        internal GraphEditContext(AuthoringGraph graph, string node, GraphMeshValue input)
        { GraphId = graph.GraphId; NodeId = node; InputSnapshot = input.SnapshotHash; DomainId = input.DomainId; }
        private GraphEditContext(string graph,string node,string snapshot,string domain)
        { GraphId=graph;NodeId=node;InputSnapshot=snapshot;DomainId=domain; }
        public static GraphEditContext FromIdentity(string graph,string node,string snapshot,string domain)
        {
            Checks.Id(graph);Checks.Id(node);Checks.HashText(snapshot);Checks.HashText(domain);
            return new GraphEditContext(graph,node,snapshot,domain);
        }
    }

    public static class GraphEditing
    {
        public static GraphEditContext Context(AuthoringGraph graph, string nodeId)
        {
            Checks.Require(graph.Nodes.ContainsKey(nodeId) && (graph.Nodes[nodeId].TypeId == BuiltinNodes.EditMesh || graph.Nodes[nodeId].TypeId == BuiltinNodes.PolygonEdit) && BuiltinNodes.Find(graph.Nodes[nodeId]) != null,
                "EDIT_MODE_UNSUPPORTED", "Choose a supported EditMesh node.");
            var evaluation = GraphEvaluator.Evaluate(graph);
            GraphMeshValue input;
            Checks.Require(evaluation.MeshInputs.TryGetValue(nodeId, out input), "INPUT_UNRESOLVED", "Edit input is not available.");
            Checks.Require((input.Polygon != null) == (graph.Nodes[nodeId].TypeId == BuiltinNodes.PolygonEdit), "EDIT_MODE_UNSUPPORTED", "Choose the edit node matching the input geometry domain.");
            return new GraphEditContext(graph, nodeId, input);
        }

        public static AuthoringGraph Translate(AuthoringGraph graph, GraphEditContext context, IEnumerable<int> vertices, Vec3 delta)
        {
            Checks.Require(context != null && graph.GraphId == context.GraphId, "EDIT_CONTEXT_STALE", "Edit context belongs to another graph.");
            var current = Context(graph, context.NodeId);
            Checks.Require(graph.Nodes[context.NodeId].TypeId == BuiltinNodes.EditMesh, "EDIT_MODE_UNSUPPORTED", "Render indices cannot edit polygons.");
            Checks.Require(current.InputSnapshot == context.InputSnapshot && current.DomainId == context.DomainId, "EDIT_CONTEXT_STALE", "Reacquire the edit context after an upstream change.");
            Checks.Require(vertices != null, "INVALID_SELECTION", "Selection is required.");
            var ids = vertices.Take(AuthoringLimits.MaxVertices + 1).ToArray();
            Checks.Require(ids.Length > 0 && ids.Length <= AuthoringLimits.MaxVertices && ids.Distinct().Count() == ids.Length, "INVALID_SELECTION", "Select distinct vertices within budget.");
            Checks.Finite(delta);
            int count = GraphEvaluator.Evaluate(graph).MeshInputs[context.NodeId].Mesh.VertexCount;
            foreach (int id in ids) Checks.Require(id >= 0 && id < count, "INVALID_VERTEX", "Vertex is outside the edit input domain.");
            var node = graph.Nodes[context.NodeId];
            Checks.Require(node.Offsets.Count == 0 || node.ExpectedInputSnapshot == current.InputSnapshot && node.ExpectedDomain == current.DomainId,
                "EDIT_INPUT_CHANGED", "Existing payload requires explicit rebase; it will not be silently retargeted.");
            var offsets = new Dictionary<int, Vec3>(node.Offsets);
            foreach (int id in ids)
            {
                Vec3 before; offsets.TryGetValue(id, out before); var next = before + delta; Checks.Finite(next);
                if (next.X == 0 && next.Y == 0 && next.Z == 0) offsets.Remove(id); else offsets[id] = next;
            }
            var candidate = graph.ReplaceNode(GraphNode.Edit(node.NodeId, true, offsets, current.InputSnapshot, current.DomainId));
            var result = GraphEvaluator.Evaluate(candidate);
            var failure = result.Diagnostics.FirstOrDefault(d => d.NodeId == context.NodeId);
            if (failure != null) throw new AuthoringException(failure.Code, failure.Message);
            return candidate;
        }
    }
}
