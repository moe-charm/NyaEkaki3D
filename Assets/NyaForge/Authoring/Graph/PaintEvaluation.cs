using NyaForge.Authoring.Paint;

namespace NyaForge.Authoring.Graph
{
    public sealed class GraphImageValue
    {
        public PaintImage Image { get; }
        public string UvHash { get; }
        public string MeshDomain { get; }
        public string ImageHash { get; }
        /// <summary>Stable graph Paint node that produced this image.</summary>
        public string PaintNodeId { get; }
        internal GraphImageValue(PaintImage image,string uvHash,string meshDomain,string paintNodeId = "")
        { Image=image;UvHash=uvHash;MeshDomain=meshDomain;PaintNodeId=paintNodeId ?? "";ImageHash=Checks.Hash(PaintImageCodec.Write(image)); }
    }

    internal static class PaintEvaluation
    {
        internal static GraphImageValue Apply(GraphNode node,GraphMeshValue input)
        {
            // Imported GLB images are immutable, unbound base-color resources. They
            // intentionally bypass polygon UV validation and are connected directly
            // to a StandardMaterial node; authored paint still requires a polygon UV
            // binding and follows the strict path below.
            if (input.Polygon == null)
            {
                Checks.Require(node.PaintImage != null && node.PaintUvHash == "" && node.ExpectedDomain == "", "UV_MISSING", "Paint requires polygon UVs.");
                return new GraphImageValue(node.PaintImage, "", input.DomainId, node.NodeId);
            }
            string uvHash=PaintUvBinding.Hash(input.Polygon);
            if(node.PaintImage != null)
            {
                Checks.Require(node.ExpectedDomain == input.DomainId,"PAINT_UV_CHANGED","Paint belongs to another mesh domain; old image is retained.");
                PaintUvBinding.RequireMatch(node.PaintUvHash,input.Polygon);
            }
            return new GraphImageValue(node.PaintImage ?? new PaintImage(node.PaintWidth,node.PaintHeight,new Rgba32(255,255,255)),uvHash,input.DomainId,node.NodeId);
        }
        internal static GraphMeshValue Bind(GraphMeshValue mesh,GraphImageValue image)
        {
            if(image == null) return mesh;
            Checks.Require(mesh.Material==null && mesh.SlotMaterials==null,"MATERIAL_ALREADY_ASSIGNED","Connect the image to the material node instead of overriding the assigned material at Output.");
            if (mesh.Polygon == null)
            {
                Checks.Require(image.UvHash == "" && image.MeshDomain == mesh.DomainId, "PAINT_UV_CHANGED", "Imported base color input belongs to another mesh.");
                return new GraphMeshValue(mesh.Mesh,mesh.Transform,mesh.DomainId,mesh.Polygon,mesh.PolygonRendering,image);
            }
            Checks.Require(image.MeshDomain == mesh.DomainId,"PAINT_UV_CHANGED","Base color input belongs to another mesh.");
            PaintUvBinding.RequireMatch(image.UvHash,mesh.Polygon);
            return new GraphMeshValue(mesh.Mesh,mesh.Transform,mesh.DomainId,mesh.Polygon,mesh.PolygonRendering,image);
        }
    }
}
