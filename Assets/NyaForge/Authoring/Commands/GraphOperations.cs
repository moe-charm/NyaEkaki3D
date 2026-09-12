using System;
using System.IO;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public GraphNode Node { get; private set; }
        public GraphEdge Edge { get; private set; }
        public string NodeId { get; private set; }
        public string Port { get; private set; }
        public GraphEditContext EditContext { get; private set; }
        public System.Collections.Generic.IReadOnlyList<ulong> ElementIds { get; private set; } = Array.Empty<ulong>();
        public static AuthoringOperation ExtrudePolygonFaces(GraphEditContext context, ulong[] faceIds, Vec3 delta)
        {
            Checks.Require(context != null && faceIds != null && faceIds.Length <= AuthoringLimits.MaxIndices / 3, "INVALID_SELECTION", "Bounded face selection is required.");
            return new AuthoringOperation("graph.polygon.extrude", Array.Empty<int>(), delta, false) { EditContext = context, ElementIds = Array.AsReadOnly((ulong[])faceIds.Clone()) };
        }
        public static AuthoringOperation SolidifyPolygon(GraphEditContext context, float thickness)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new AuthoringOperation("graph.polygon.solidify", Array.Empty<int>(), new Vec3(thickness, 0, 0), false) { EditContext = context };
        }
        public static AuthoringOperation ProjectPolygonUv(GraphEditContext context)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return new AuthoringOperation("graph.polygon.uv-project", Array.Empty<int>(), new Vec3(), false) { EditContext = context };
        }
        public static AuthoringOperation TranslatePolygonVertices(GraphEditContext context, ulong[] ids, Vec3 delta)
        {
            Checks.Require(context != null && ids != null && ids.Length <= AuthoringLimits.MaxVertices, "INVALID_SELECTION", "Bounded stable-ID selection is required.");
            return new AuthoringOperation("graph.polygon.translate", Array.Empty<int>(), delta, false) { EditContext = context, ElementIds = Array.AsReadOnly((ulong[])ids.Clone()) };
        }
        static AuthoringOperation GraphOp(string kind) { return new AuthoringOperation(kind, Array.Empty<int>(), new Vec3(), false); }
        public static AuthoringOperation AddGraph(AuthoringGraph graph) => AddGraph(graph,Guid.NewGuid().ToString("D"));
        public static AuthoringOperation AddGraph(AuthoringGraph graph,string objectId)
        {
            if (graph == null) throw new ArgumentNullException(nameof(graph));
            Checks.Id(objectId);
            return new AuthoringOperation("object.add_graph", Array.Empty<int>(), new Vec3(), false) { Graph = graph, NewObjectId = objectId };
        }
        public static AuthoringOperation AddNode(GraphNode node) { return NodeOp("graph.node.add", node); }
        public static AuthoringOperation UpdateNode(GraphNode node) { return NodeOp("graph.node.update", node); }
        static AuthoringOperation NodeOp(string kind, GraphNode node)
        {
            if (node == null) throw new ArgumentNullException(nameof(node));
            var op = GraphOp(kind); op.Node = node; return op;
        }
        public static AuthoringOperation RemoveNode(string nodeId) { Checks.Id(nodeId); var op = GraphOp("graph.node.remove"); op.NodeId = nodeId; return op; }
        public static AuthoringOperation SetOutput(string nodeId)
        {
            if (nodeId != "") Checks.Id(nodeId);
            var op = GraphOp("graph.output"); op.NodeId = nodeId; return op;
        }
        public static AuthoringOperation Connect(GraphEdge edge)
        {
            if (edge == null) throw new ArgumentNullException(nameof(edge));
            var op = GraphOp("graph.connect"); op.Edge = edge; return op;
        }
        public static AuthoringOperation Disconnect(string nodeId, string inputPort)
        {
            Checks.Id(nodeId); Checks.Name(inputPort);
            var op = GraphOp("graph.disconnect"); op.NodeId = nodeId; op.Port = inputPort; return op;
        }
        public static AuthoringOperation TranslateGraphVertices(GraphEditContext context, int[] ids, Vec3 delta)
        {
            if (context == null || ids == null) throw new ArgumentNullException(context == null ? nameof(context) : nameof(ids));
            return new AuthoringOperation("graph.vertices.translate", ids, delta, false) { EditContext = context };
        }

        internal void WriteGraphFingerprint(BinaryWriter writer)
        {
            WritePaintFingerprint(writer);
            WritePaintRebindFingerprint(writer);
            WriteLayerFingerprint(writer);
            WriteCutPathFingerprint(writer);
            if (UvTransform != null) { writer.Write(Checks.Canonical(UvTransform.Translation.X)); writer.Write(Checks.Canonical(UvTransform.Translation.Y)); writer.Write(Checks.Canonical(UvTransform.Degrees)); writer.Write(Checks.Canonical(UvTransform.Scale)); }
            if (Kind == "object.add_graph") { writer.Write(NewObjectId); writer.Write(GraphContentIdentity.Hash(Graph)); }
            if (Kind == "object.select") writer.Write(NewObjectId);
            if (Node != null) writer.Write(GraphContentIdentity.Hash(new AuthoringGraph(Node.NodeId, new[] { Node }, Array.Empty<GraphEdge>(), "")));
            if (Edge != null) { writer.Write(Edge.FromNode); writer.Write(Edge.FromPort); writer.Write(Edge.ToNode); writer.Write(Edge.ToPort); }
            writer.Write(NodeId ?? ""); writer.Write(Port ?? "");
            writer.Write(ElementIds.Count); foreach (ulong id in ElementIds) writer.Write(id);
            if(Kind=="graph.polygon.material") { writer.Write(MaterialSlot);writer.Write(MaterialNodeId ?? ""); }
            if (EditContext != null) { writer.Write(EditContext.GraphId); writer.Write(EditContext.NodeId); writer.Write(EditContext.InputSnapshot); writer.Write(EditContext.DomainId); }
        }
    }
}

