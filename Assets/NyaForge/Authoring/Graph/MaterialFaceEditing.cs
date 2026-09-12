using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Atomically updates face slots, their material connection, and proven unchanged image mappings.</summary>
    public static class MaterialFaceEditing
    {
        public static AuthoringGraph Assign(AuthoringGraph graph,GraphEditContext context,IEnumerable<ulong> faces,int slot,string materialId)
        {
            var before=GraphEvaluator.Evaluate(graph);
            Checks.Require(before.IsComplete && before.Output.SlotMaterials!=null,"MATERIAL_REQUIRED","Resolve the graph and enable per-slot materials first.");
            Checks.Require(graph.Nodes.TryGetValue(materialId,out var material) && material.TypeId==BuiltinNodes.StandardMaterial && material.Version==1,"MATERIAL_REQUIRED","Select a standard material node.");
            var outputEdge=graph.Edges.Single(e=>e.ToNode==graph.OutputNodeId && e.ToPort=="mesh");
            var assignment=graph.Nodes[outputEdge.FromNode];
            Checks.Require(assignment.TypeId==BuiltinNodes.AssignMaterials,"MATERIAL_ORDER_UNSUPPORTED","Output must directly follow slot assignment.");
            var changed=PolygonEditing.AssignMaterial(graph,context,faces,slot);
            changed=new AuthoringGraph(changed.GraphId,changed.Nodes.Values.Select(n=>n.NodeId==assignment.NodeId ? GraphNode.AssignMaterials(n.NodeId,n.MaterialSlots.Append(slot).Distinct()) : n),
                changed.Edges.Where(e=>!(e.ToNode==assignment.NodeId && e.ToPort==GraphNode.MaterialSlotPort(slot))).Append(new GraphEdge(materialId,"material",assignment.NodeId,GraphNode.MaterialSlotPort(slot))),changed.OutputNodeId);
            var after=GraphEvaluator.Evaluate(changed);
            foreach(var node in graph.Nodes.Values.Where(n=>n.Version==1 && (n.TypeId==BuiltinNodes.Paint || n.TypeId==BuiltinNodes.LayeredPaint) && n.PaintImage!=null))
            {
                Checks.Require(before.ImageOutputs.ContainsKey(node.NodeId),"PAINT_INPUT_UNRESOLVED","Do not rebind unresolved paint during material assignment.");
                before.MeshInputs.TryGetValue(node.NodeId,out var oldInput);after.MeshInputs.TryGetValue(node.NodeId,out var newInput);
                Checks.Require(oldInput!=null && newInput!=null,"PAINT_INPUT_UNRESOLVED","Material assignment changed a paint input path.");
                RequireSameMapping(oldInput,newInput);
                string hash=PaintUvBinding.Hash(newInput.Polygon);
                if(hash!=node.PaintUvHash) changed=PaintRebinding.Apply(changed,PaintRebinding.Context(changed,node.NodeId));
            }
            Checks.Require(GraphEvaluator.Evaluate(changed).IsComplete,"MATERIAL_ASSIGNMENT_UNRESOLVED","The material assignment leaves unresolved downstream edits.");
            return changed;
        }
        static void RequireSameMapping(GraphMeshValue before,GraphMeshValue after)
        {
            var a=before.Polygon;var b=after.Polygon;
            Checks.Require(before.DomainId==after.DomainId && before.Transform.Equals(after.Transform) && a!=null && b!=null && a.DomainId==b.DomainId && a.Vertices.Count==b.Vertices.Count && a.Faces.Count==b.Faces.Count,"PAINT_UV_CHANGED","Only material slots may change during automatic image rebinding.");
            foreach(var pair in a.Vertices) Checks.Require(b.Vertices.TryGetValue(pair.Key,out var vertex) && pair.Value.Position.Equals(vertex.Position),"PAINT_UV_CHANGED","Geometry changed during material assignment.");
            var faces=b.Faces.ToDictionary(f=>f.Id);
            foreach(var face in a.Faces)
            {
                Checks.Require(faces.TryGetValue(face.Id,out var other) && face.Corners.Count==other.Corners.Count,"PAINT_UV_CHANGED","Face mapping changed.");
                for(int i=0;i<face.Corners.Count;i++)
                {
                    var x=face.Corners[i];var y=other.Corners[i];
                    Checks.Require(x.Id==y.Id && x.VertexId==y.VertexId && Equals(x.Uv0,y.Uv0) && Equals(x.Normal,y.Normal) && Equals(x.Tangent,y.Tangent),"PAINT_UV_CHANGED","Corner mapping changed.");
                }
            }
        }
    }
}
