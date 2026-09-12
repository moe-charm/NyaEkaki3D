using System.Linq;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Shared routing for the legacy image output and the explicit material assignment path.</summary>
    public sealed class OutputSurfaceConnections
    {
        public static System.Collections.Generic.IReadOnlyList<int> ImageSlots(AuthoringGraph graph,string imageNodeId)
        {
            if(graph==null || string.IsNullOrEmpty(imageNodeId)) return System.Array.Empty<int>();
            var result=GraphEvaluator.Evaluate(graph);
            if(!result.IsComplete || result.Output.SlotMaterials==null) return System.Array.Empty<int>();
            var used=result.Output.PolygonRendering?.MaterialSlotMap ?? Enumerable.Range(0,result.Output.Mesh.Submeshes.Count).ToArray();
            return System.Array.AsReadOnly(used.Where(slot=>Resolve(graph,slot)?.ImageNodeId==imageNodeId).ToArray());
        }
        public GraphEdge Geometry { get; }
        public string MaterialNodeId { get; }
        public string ImageNodeId { get; }
        public string ImageTargetNodeId { get; }
        OutputSurfaceConnections(GraphEdge geometry,string material,string image,string target)
        { Geometry=geometry;MaterialNodeId=material;ImageNodeId=image;ImageTargetNodeId=target; }
        public static OutputSurfaceConnections Resolve(AuthoringGraph graph,int? materialSlot=null)
        {
            if(graph==null || graph.OutputNodeId=="") return null;
            GraphEdge Input(string node,string port)=>graph.Edges.SingleOrDefault(e=>e.ToNode==node && e.ToPort==port);
            var geometry=Input(graph.OutputNodeId,"mesh");if(geometry==null) return null;
            string material="",target=graph.OutputNodeId;
            var source=graph.Nodes[geometry.FromNode];
            if(source.TypeId==BuiltinNodes.AssignMaterials && source.Version==1)
            {
                if(!materialSlot.HasValue) return null;
                var link=Input(source.NodeId,GraphNode.MaterialSlotPort(materialSlot.Value));geometry=Input(source.NodeId,"mesh");
                if(link==null || geometry==null) return null;
                var node=graph.Nodes[link.FromNode];
                if(node.TypeId!=BuiltinNodes.StandardMaterial || node.Version!=1) return null;
                material=target=node.NodeId;
            }
            if(source.TypeId==BuiltinNodes.AssignMaterial && source.Version==1)
            {
                var link=Input(source.NodeId,"material");geometry=Input(source.NodeId,"mesh");
                if(link==null || geometry==null) return null;
                var node=graph.Nodes[link.FromNode];
                if(node.TypeId!=BuiltinNodes.StandardMaterial || node.Version!=1) return null;
                material=target=node.NodeId;
            }
            return new OutputSurfaceConnections(geometry,material,Input(target,"baseColor")?.FromNode ?? "",target);
        }
        public static AuthoringGraph AddMaterial(AuthoringGraph graph,string materialId,string assignmentId)
        {
            var route=Resolve(graph);
            Checks.Require(route!=null && GraphEvaluator.Evaluate(graph).IsComplete,"GRAPH_INCOMPLETE","Resolve the output before adding a material.");
            Checks.Require(route.MaterialNodeId=="","MATERIAL_ALREADY_ASSIGNED","The output already has a material.");
            var edges=graph.Edges.Where(e=>e.ToNode!=graph.OutputNodeId).ToList();
            edges.Add(new GraphEdge(route.Geometry.FromNode,route.Geometry.FromPort,assignmentId,"mesh"));
            edges.Add(new GraphEdge(materialId,"material",assignmentId,"material"));
            edges.Add(new GraphEdge(assignmentId,"mesh",graph.OutputNodeId,"mesh"));
            if(route.ImageNodeId!="") edges.Add(new GraphEdge(route.ImageNodeId,"image",materialId,"baseColor"));
            return new AuthoringGraph(graph.GraphId,graph.Nodes.Values.Concat(new[]{GraphNode.StandardMaterial(materialId),GraphNode.AssignMaterial(assignmentId)}),edges,graph.OutputNodeId);
        }
    }
}
