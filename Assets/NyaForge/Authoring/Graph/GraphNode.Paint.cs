using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring.Graph
{
    public sealed partial class GraphNode
    {
        public PaintImage PaintImage { get; private set; }
        public int PaintWidth { get; private set; }
        public int PaintHeight { get; private set; }
        public string PaintUvHash { get; private set; }
        public static GraphNode Paint(string id, int width = 256, int height = 256, PaintImage image = null, string uvHash = "", string domain = "")
        {
            PaintImage.ValidateDimensions(width,height);
            if (image != null)
            {
                Checks.Require(image.Width == width && image.Height == height,"INVALID_IMAGE_SIZE","Paint payload dimensions differ from node.");
                Checks.HashText(uvHash); Checks.HashText(domain);
            }
            else Checks.Require(uvHash == "" && domain == "","INVALID_PAINT_BINDING","Unpainted nodes cannot carry a stale image binding.");
            return new GraphNode(id,BuiltinNodes.Paint,1,null,Identity,0,0,0,true,"",domain,Empty,"")
            { PaintWidth=width,PaintHeight=height,PaintImage=image,PaintUvHash=uvHash };
        }
    }
}
