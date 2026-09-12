using System.Collections.Generic;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring.Graph
{
    public sealed class PaintEditContext
    {
        public string GraphId { get; }
        public string NodeId { get; }
        public string ImageHash { get; }
        public string UvHash { get; }
        public string MeshDomain { get; }
        PaintEditContext(string graph,string node,string image,string uv,string domain)
        { GraphId=graph;NodeId=node;ImageHash=image;UvHash=uv;MeshDomain=domain; }
        public static PaintEditContext FromIdentity(string graph,string node,string image,string uv,string domain)
        {
            Checks.Id(graph);Checks.Id(node);Checks.HashText(image);Checks.HashText(uv);Checks.HashText(domain);
            return new PaintEditContext(graph,node,image,uv,domain);
        }
        internal PaintEditContext(AuthoringGraph graph,string node,GraphImageValue value)
        { GraphId=graph.GraphId;NodeId=node;ImageHash=value.ImageHash;UvHash=value.UvHash;MeshDomain=value.MeshDomain; }
    }
    public static class PaintEditing
    {
        public static PaintEditContext Context(AuthoringGraph graph,string node)
        {
            Checks.Require(graph.Nodes.TryGetValue(node,out var value) && value.TypeId==BuiltinNodes.Paint,"PAINT_NODE_REQUIRED","Select a Paint node.");
            var evaluation=GraphEvaluator.Evaluate(graph);
            Checks.Require(evaluation.ImageOutputs.TryGetValue(node,out var image),"PAINT_UNRESOLVED","Resolve the Paint node's UV binding before editing.");
            return new PaintEditContext(graph,node,image);
        }
        public static AuthoringGraph Stroke(AuthoringGraph graph,PaintEditContext context,IEnumerable<Vec2> points,float radius,Rgba32 color)
        {
            Checks.Require(context!=null && context.GraphId==graph.GraphId,"PAINT_CONTEXT_STALE","Paint context belongs to a different graph.");
            var current=Context(graph,context.NodeId);
            Checks.Require(current.ImageHash==context.ImageHash && current.UvHash==context.UvHash && current.MeshDomain==context.MeshDomain,"PAINT_CONTEXT_STALE","Paint image or UV binding changed.");
            var image=GraphEvaluator.Evaluate(graph).ImageOutputs[context.NodeId].Image;
            var result=PaintStroke.Apply(image,points,radius,color);
            return graph.ReplaceNode(GraphNode.Paint(context.NodeId,result.Width,result.Height,result,current.UvHash,current.MeshDomain));
        }
    }
}
