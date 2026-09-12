using System.Linq;

namespace NyaForge.Authoring.Graph
{
    public static class MaterialSlotPaintEditing
    {
        public static AuthoringGraph AddBlank(AuthoringGraph graph,int slot,string materialId,string paintId)
        {
            Checks.Require(GraphEvaluator.Evaluate(graph).IsComplete,"GRAPH_INCOMPLETE","Resolve the graph before adding a Paint.");
            var independent=MaterialSlotEditing.MakeIndependent(graph,slot,materialId);
            var route=OutputSurfaceConnections.Resolve(independent,slot);
            var edges=independent.Edges.Where(e=>!(e.ToNode==materialId && e.ToPort=="baseColor")).Concat(new[]{
                new GraphEdge(route.Geometry.FromNode,route.Geometry.FromPort,paintId,"mesh"),new GraphEdge(paintId,"image",materialId,"baseColor")});
            return new AuthoringGraph(graph.GraphId,independent.Nodes.Values.Append(GraphNode.Paint(paintId)),edges,graph.OutputNodeId);
        }
    }
}
