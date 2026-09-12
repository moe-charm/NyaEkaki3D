using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring.Graph
{
    public sealed partial class GraphNode
    {
        public PaintLayers LayerStack { get; private set; }
        public static GraphNode LayeredPaint(string id,PaintLayers layers,string uvHash,string domain)
        {
            Checks.Require(layers!=null,"INVALID_PAINT_LAYERS","A layered Paint node requires a stack.");
            Checks.HashText(uvHash); Checks.HashText(domain);
            return new GraphNode(id,BuiltinNodes.LayeredPaint,1,null,Identity,0,0,0,true,"",domain,Empty,"")
            {
                LayerStack=layers, PaintWidth=layers.Width, PaintHeight=layers.Height,
                PaintImage=layers.Composite(), PaintUvHash=uvHash
            };
        }
    }
}
