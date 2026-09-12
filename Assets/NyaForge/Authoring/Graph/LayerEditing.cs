using System.Linq;
using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring.Graph
{
    public sealed class LayerEditContext
    {
        public string GraphId { get; }
        public string NodeId { get; }
        public string StackHash { get; }
        public string UvHash { get; }
        public string MeshDomain { get; }
        LayerEditContext(string graph,string node,string stack,string uv,string domain)
        { GraphId=graph;NodeId=node;StackHash=stack;UvHash=uv;MeshDomain=domain; }
        public static LayerEditContext FromIdentity(string graph,string node,string stack,string uv,string domain)
        {
            Checks.Id(graph);Checks.Id(node);Checks.HashText(stack);Checks.HashText(uv);Checks.HashText(domain);
            return new LayerEditContext(graph,node,stack,uv,domain);
        }
        internal LayerEditContext(AuthoringGraph graph,GraphNode node,GraphImageValue image)
        {
            GraphId=graph.GraphId;NodeId=node.NodeId;UvHash=image.UvHash;MeshDomain=image.MeshDomain;
            StackHash=Checks.Hash(PaintLayersCodec.Write(node.LayerStack,Checks.Hash));
        }
    }
    public static class LayerEditing
    {
        public static LayerEditContext Context(AuthoringGraph graph,string nodeId)
        {
            Checks.Require(graph.Nodes.TryGetValue(nodeId,out var node) && node.TypeId==BuiltinNodes.LayeredPaint && node.Version==1,"PAINT_LAYER_NODE_REQUIRED","Select a supported layered Paint node.");
            var result=GraphEvaluator.Evaluate(graph);
            Checks.Require(result.ImageOutputs.TryGetValue(nodeId,out var image),"PAINT_UNRESOLVED","Resolve the layered Paint UV binding before editing.");
            return new LayerEditContext(graph,node,image);
        }
        public static AuthoringGraph Apply(AuthoringGraph graph,LayerEditContext context,PaintLayerChange change)
        {
            Checks.Require(context!=null && context.GraphId==graph.GraphId && change!=null,"PAINT_CONTEXT_STALE","A valid layer context is required.");
            var current=Context(graph,context.NodeId);
            Checks.Require(current.StackHash==context.StackHash && current.UvHash==context.UvHash && current.MeshDomain==context.MeshDomain,"PAINT_CONTEXT_STALE","Layer stack or UV changed.");
            var node=graph.Nodes[context.NodeId];var layers=node.LayerStack.Layers.ToList();
            int index=layers.FindIndex(l=>l.Id==change.LayerId);
            if(change.Action==PaintLayerAction.Add)
            {
                Checks.Require(index<0,"INVALID_PAINT_LAYERS","Layer ID already exists.");
                Checks.Require(change.Index>=0 && change.Index<=layers.Count,"INVALID_LAYER_INDEX","Insertion position outside stack.");
                layers.Insert(change.Index,change.Layer);
            }
            else
            {
                Checks.Require(index>=0,"PAINT_LAYER_NOT_FOUND","Layer no longer exists.");
                var layer=layers[index];
                switch(change.Action)
                {
                    case PaintLayerAction.Remove: layers.RemoveAt(index);break;
                    case PaintLayerAction.Move:
                        Checks.Require(change.Index>=0 && change.Index<layers.Count,"INVALID_LAYER_INDEX","Move position outside stack.");
                        layers.RemoveAt(index);layers.Insert(change.Index,layer);break;
                    case PaintLayerAction.Appearance: layers[index]=layer.WithAppearance(change.Opacity,change.Visible);break;
                    case PaintLayerAction.Rename: layers[index]=new PaintLayer(layer.Id,change.Name,layer.Image,layer.Opacity,layer.Visible,layer.Mask);break;
                    case PaintLayerAction.Mask: layers[index]=layer.WithMask(change.Coverage);break;
                    case PaintLayerAction.Stroke: layers[index]=layer.WithImage(PaintStroke.Apply(layer.Image,change.Points,change.Radius,change.Color));break;
                    case PaintLayerAction.MaskStroke: layers[index]=layer.WithMask(PaintMaskStroke.Apply(layer.Mask,change.Points,change.Radius,change.MaskTarget,change.MaskStrength));break;
                    case PaintLayerAction.PathStroke: layers[index]=layer.WithImage(PaintStroke.ApplyPaths(layer.Image,change.Path,change.Radius,change.Color));break;
                    case PaintLayerAction.MaskPathStroke: layers[index]=layer.WithMask(PaintMaskStroke.ApplyPaths(layer.Mask,change.Path,change.Radius,change.MaskTarget,change.MaskStrength));break;
                    default: throw new AuthoringException("UNSUPPORTED_OPERATION","Unsupported layer operation.");
                }
            }
            return graph.ReplaceNode(GraphNode.LayeredPaint(node.NodeId,new PaintLayers(node.PaintWidth,node.PaintHeight,layers),current.UvHash,current.MeshDomain));
        }
        public static AuthoringGraph Migrate(AuthoringGraph graph,PaintEditContext context,string layerId)
        {
            Checks.Require(context!=null && context.GraphId==graph.GraphId,"PAINT_CONTEXT_STALE","Paint migration context is stale.");
            var current=PaintEditing.Context(graph,context.NodeId);
            Checks.Require(current.ImageHash==context.ImageHash && current.UvHash==context.UvHash && current.MeshDomain==context.MeshDomain,"PAINT_CONTEXT_STALE","Paint image or binding changed.");
            var image=GraphEvaluator.Evaluate(graph).ImageOutputs[context.NodeId].Image;
            var stack=new PaintLayers(image.Width,image.Height,new[]{new PaintLayer(layerId,"背景",image)});
            return graph.ReplaceNode(GraphNode.LayeredPaint(context.NodeId,stack,current.UvHash,current.MeshDomain));
        }
    }
}
