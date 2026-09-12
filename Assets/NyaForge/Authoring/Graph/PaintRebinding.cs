using System.Linq;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Explicitly keeps image pixels while changing their UV interpretation; never reprojects paint.</summary>
    public sealed class PaintRebindContext
    {
        public string GraphId { get; }
        public string NodeId { get; }
        public string ImageHash { get; }
        public string OldUvHash { get; }
        public string OldDomain { get; }
        public string NewUvHash { get; }
        public string NewDomain { get; }
        public string InputNodeId { get; }
        public string InputSnapshot { get; }
        public string LayerStackHash { get; }
        internal PaintRebindContext(AuthoringGraph graph, GraphNode node, GraphEdge edge, GraphMeshValue mesh)
        {
            GraphId = graph.GraphId; NodeId = node.NodeId;
            ImageHash = Checks.Hash(PaintImageCodec.Write(node.PaintImage));
            OldUvHash = node.PaintUvHash; OldDomain = node.ExpectedDomain;
            NewUvHash = PaintUvBinding.Hash(mesh.Polygon); NewDomain = mesh.DomainId;
            InputNodeId = edge.FromNode; InputSnapshot = mesh.SnapshotHash;
            LayerStackHash = node.LayerStack == null ? "" : Checks.Hash(PaintLayersCodec.Write(node.LayerStack,Checks.Hash));
        }
    }

    public static class PaintRebinding
    {
        public static PaintRebindContext Context(AuthoringGraph graph, string nodeId)
        {
            Checks.Require(graph != null, "PAINT_NODE_REQUIRED", "A graph is required.");
            Checks.Require(graph.Nodes.TryGetValue(nodeId, out var node) && (node.TypeId == BuiltinNodes.Paint || node.TypeId == BuiltinNodes.LayeredPaint) && node.Version == 1,
                "PAINT_NODE_REQUIRED", "Select a supported Paint node.");
            Checks.Require(node.PaintImage != null, "PAINT_IMAGE_REQUIRED", "There is no saved image to rebind.");
            var edge = graph.Edges.SingleOrDefault(e => e.ToNode == nodeId && e.ToPort == "mesh");
            Checks.Require(edge != null, "PAINT_INPUT_UNRESOLVED", "Connect the Paint mesh input first.");
            var evaluation = GraphEvaluator.Evaluate(graph);
            Checks.Require(evaluation.MeshOutputs.TryGetValue(edge.FromNode, out var mesh) && mesh.Polygon != null,
                "PAINT_INPUT_UNRESOLVED", "Resolve the upstream polygon mesh first.");
            return new PaintRebindContext(graph, node, edge, mesh);
        }

        public static AuthoringGraph Apply(AuthoringGraph graph, PaintRebindContext context)
        {
            Checks.Require(context != null && graph.GraphId == context.GraphId, "PAINT_CONTEXT_STALE", "Rebind context belongs to another graph.");
            var current = Context(graph, context.NodeId);
            Checks.Require(current.ImageHash == context.ImageHash && current.OldUvHash == context.OldUvHash &&
                current.OldDomain == context.OldDomain && current.NewUvHash == context.NewUvHash &&
                current.NewDomain == context.NewDomain && current.InputNodeId == context.InputNodeId &&
                current.InputSnapshot == context.InputSnapshot && current.LayerStackHash == context.LayerStackHash,
                "PAINT_CONTEXT_STALE", "Paint or its target mesh changed; review the binding again.");
            var node = graph.Nodes[context.NodeId];
            if (node.LayerStack != null)
                return graph.ReplaceNode(GraphNode.LayeredPaint(node.NodeId,node.LayerStack,current.NewUvHash,current.NewDomain));
            var image = node.PaintImage;
            return graph.ReplaceNode(GraphNode.Paint(context.NodeId, image.Width, image.Height, image, current.NewUvHash, current.NewDomain));
        }
    }
}
