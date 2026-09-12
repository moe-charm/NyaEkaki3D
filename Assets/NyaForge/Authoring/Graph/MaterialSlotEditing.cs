using System.Linq;

namespace NyaForge.Authoring.Graph
{
    public static class MaterialSlotEditing
    {
        public static AuthoringGraph ConvertOutput(AuthoringGraph graph)
        {
            var result=GraphEvaluator.Evaluate(graph);
            Checks.Require(result.IsComplete && result.Output.Material!=null,"MATERIAL_REQUIRED","Assign a standard material before enabling per-slot editing.");
            var outputEdge=graph.Edges.Single(e=>e.ToNode==graph.OutputNodeId && e.ToPort=="mesh");
            var assignment=graph.Nodes[outputEdge.FromNode];
            Checks.Require(assignment.TypeId==BuiltinNodes.AssignMaterial,"MATERIAL_ORDER_UNSUPPORTED","The output must directly follow material assignment.");
            var input=graph.Edges.Single(e=>e.ToNode==assignment.NodeId && e.ToPort=="material");
            var slots=result.Output.PolygonRendering?.MaterialSlotMap ?? Enumerable.Range(0,result.Output.Mesh.Submeshes.Count).ToArray();
            return new AuthoringGraph(graph.GraphId,graph.Nodes.Values.Select(n=>n.NodeId==assignment.NodeId ? GraphNode.AssignMaterials(n.NodeId,slots) : n),
                graph.Edges.Where(e=>e!=input).Concat(slots.Select(s=>new GraphEdge(input.FromNode,input.FromPort,assignment.NodeId,GraphNode.MaterialSlotPort(s)))),graph.OutputNodeId);
        }
        public static AuthoringGraph MakeIndependent(AuthoringGraph graph,int slot,string newMaterialId)
        {
            var route=OutputSurfaceConnections.Resolve(graph,slot);
            Checks.Require(route!=null && route.MaterialNodeId!="","MATERIAL_REQUIRED","Choose an assigned material slot.");
            var assignment=graph.Edges.Single(e=>e.ToNode==graph.OutputNodeId && e.ToPort=="mesh").FromNode;
            Checks.Require(graph.Nodes[assignment].TypeId==BuiltinNodes.AssignMaterials,"MATERIAL_REQUIRED","Enable per-slot editing first.");
            var old=graph.Nodes[route.MaterialNodeId];
            var links=graph.Edges.Where(e=>!(e.ToNode==assignment && e.ToPort==GraphNode.MaterialSlotPort(slot))).ToList();
            links.Add(new GraphEdge(newMaterialId,"material",assignment,GraphNode.MaterialSlotPort(slot)));
            links.AddRange(graph.Edges.Where(e=>e.ToNode==old.NodeId).Select(e=>new GraphEdge(e.FromNode,e.FromPort,newMaterialId,e.ToPort)));
            return new AuthoringGraph(graph.GraphId,graph.Nodes.Values.Append(GraphNode.StandardMaterial(newMaterialId,old.Material)),links,graph.OutputNodeId);
        }
    }
}
