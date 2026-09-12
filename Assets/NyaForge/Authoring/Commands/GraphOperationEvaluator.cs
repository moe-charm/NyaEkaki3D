using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    internal static class GraphOperationEvaluator
    {
        internal static AuthoringDocument Apply(AuthoringDocument before, AuthoringOperation op, long revision)
        {
            if (op.Kind == "object.add_graph")
                return before.AddGraph(op.NewObjectId, op.Graph, revision);
            if (op.Kind == "object.select") return before.SelectObject(op.NewObjectId, revision);
            Checks.Require(!before.IsEmpty, "NO_EDITABLE_OBJECT", "Add a graph object first.");
            var graph = before.ActiveObject.Graph;
            var nodes = graph.Nodes.Values.AsEnumerable(); var edges = graph.Edges.AsEnumerable(); string output = graph.OutputNodeId;
            switch (op.Kind)
            {
                case "graph.replace": return before.WithGraph(op.Graph, revision);
                case "graph.paint.stroke": return before.WithGraph(PaintEditing.Stroke(graph,op.PaintContext,op.StrokePoints,op.BrushRadius,op.BrushColor),revision);
                case "graph.paint.rebind": return before.WithGraph(PaintRebinding.Apply(graph, op.PaintRebind), revision);
                case "graph.layers.edit": return before.WithGraph(LayerEditing.Apply(graph,op.LayerContext,op.LayerChange),revision);
                case "graph.layers.migrate": return before.WithGraph(LayerEditing.Migrate(graph,op.MigrationContext,op.MigrationLayerId),revision);
                case "graph.node.add": nodes = nodes.Concat(new[] { op.Node }); break;
                case "graph.node.update":
                    Checks.Require(graph.Nodes.ContainsKey(op.Node.NodeId), "NODE_NOT_FOUND", "Cannot update a missing node.");
                    var old = graph.Nodes[op.Node.NodeId];
                    Checks.Require(old.TypeId == op.Node.TypeId && old.Version == op.Node.Version, "NODE_TYPE_CHANGED", "Parameter updates cannot replace a node type or version.");
                    return before.WithGraph(graph.ReplaceNode(op.Node), revision);
                case "graph.node.remove":
                    Checks.Require(graph.Nodes.ContainsKey(op.NodeId), "NODE_NOT_FOUND", "Cannot remove a missing node.");
                    nodes = nodes.Where(n => n.NodeId != op.NodeId); edges = edges.Where(e => e.FromNode != op.NodeId && e.ToNode != op.NodeId);
                    if (output == op.NodeId) output = ""; break;
                case "graph.output": output = op.NodeId; break;
                case "graph.connect": edges = edges.Concat(new[] { op.Edge }); break;
                case "graph.disconnect":
                    Checks.Require(edges.Any(e => e.ToNode == op.NodeId && e.ToPort == op.Port), "EDGE_NOT_FOUND", "Input has no connection.");
                    edges = edges.Where(e => e.ToNode != op.NodeId || e.ToPort != op.Port); break;
                case "graph.vertices.translate": return before.WithGraph(GraphEditing.Translate(graph, op.EditContext, op.VertexIds, op.Delta), revision);
                case "graph.polygon.translate": return before.WithGraph(PolygonEditing.Translate(graph, op.EditContext, op.ElementIds, op.Delta), revision);
                case "graph.polygon.extrude": return before.WithGraph(PolygonEditing.Extrude(graph, op.EditContext, op.ElementIds, op.Delta), revision);
                case "graph.polygon.delete-faces": return before.WithGraph(PolygonEditing.DeleteFaces(graph,op.EditContext,op.ElementIds),revision);
                case "graph.polygon.cap": return before.WithGraph(PolygonEditing.FillBoundary(graph,op.EditContext,op.ElementIds),revision);
                case "graph.polygon.bridge": return before.WithGraph(PolygonEditing.Bridge(graph,op.EditContext,op.ElementIds,(int)op.Delta.X),revision);
                case "graph.polygon.dissolve-faces": return before.WithGraph(PolygonEditing.DissolveFaces(graph,op.EditContext,op.ElementIds),revision);
                case "graph.polygon.weld": return before.WithGraph(PolygonEditing.Weld(graph,op.EditContext,op.ElementIds),revision);
                case "graph.polygon.cut-path": return before.WithGraph(PolygonEditing.CutPath(graph,op.EditContext,op.CutPath),revision);
                case "graph.polygon.cut-edges": return before.WithGraph(PolygonEditing.CutEdges(graph,op.EditContext,op.ElementIds,op.Delta.X,op.Delta.Y),revision);
                case "graph.polygon.add-vertex": return before.WithGraph(PolygonEditing.AddVertex(graph,op.EditContext,op.Delta),revision);
                case "graph.polygon.create-face": return before.WithGraph(PolygonEditing.CreateFace(graph,op.EditContext,op.ElementIds,(int)op.Delta.X),revision);
                case "graph.polygon.merge-faces": return before.WithGraph(PolygonEditing.MergeFaces(graph,op.EditContext,op.ElementIds),revision);
                case "graph.polygon.split-face": return before.WithGraph(PolygonEditing.SplitFace(graph,op.EditContext,op.ElementIds),revision);
                case "graph.polygon.insert-edge-vertex": return before.WithGraph(PolygonEditing.InsertEdgeVertex(graph,op.EditContext,op.ElementIds,op.Delta.X),revision);
                case "graph.polygon.material": return before.WithGraph(op.MaterialNodeId=="" ? PolygonEditing.AssignMaterial(graph,op.EditContext,op.ElementIds,op.MaterialSlot) : MaterialFaceEditing.Assign(graph,op.EditContext,op.ElementIds,op.MaterialSlot,op.MaterialNodeId),revision);
                case "graph.polygon.solidify": return before.WithGraph(PolygonEditing.Solidify(graph, op.EditContext, op.Delta.X), revision);
                case "graph.polygon.uv-project": return before.WithGraph(PolygonEditing.ProjectUv(graph, op.EditContext), revision);
                case "graph.polygon.uv-transform": return before.WithGraph(PolygonEditing.TransformUv(graph, op.EditContext, op.ElementIds, op.UvTransform), revision);
                default: throw new AuthoringException("UNSUPPORTED_OPERATION", "Unsupported graph operation.");
            }
            return before.WithGraph(new AuthoringGraph(graph.GraphId, nodes, edges, output), revision);
        }
    }
}


