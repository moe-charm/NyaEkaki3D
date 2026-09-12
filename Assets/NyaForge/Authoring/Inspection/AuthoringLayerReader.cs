using System.Linq;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Inspection
{
    internal static class AuthoringLayerReader
    {
        internal static JToken Read(AuthoringGraph graph,GraphNode node,GraphImageValue image)
        {
            if(node.TypeId!=BuiltinNodes.LayeredPaint || BuiltinNodes.Find(node)==null || node.LayerStack==null) return JValue.CreateNull();
            var context=image==null ? null : new LayerEditContext(graph,node,image);
            return new JObject
            {
                ["width"]=node.PaintWidth,["height"]=node.PaintHeight,["order"]="bottom-to-top",
                ["context"]=context==null ? JValue.CreateNull() : (JToken)new JObject { ["graphId"]=context.GraphId,["nodeId"]=context.NodeId,["stackHash"]=context.StackHash,["uvHash"]=context.UvHash,["meshDomain"]=context.MeshDomain },
                ["layers"]=new JArray(node.LayerStack.Layers.Select(l=>new JObject { ["id"]=l.Id,["name"]=l.Name,["opacity"]=l.Opacity,["visible"]=l.Visible,["hasMask"]=l.Mask!=null }))
            };
        }
    }
}
