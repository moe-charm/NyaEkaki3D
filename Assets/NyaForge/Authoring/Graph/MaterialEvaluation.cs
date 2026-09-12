namespace NyaForge.Authoring.Graph
{
    public sealed class GraphMaterialValue
    {
        public MaterialParameters Parameters { get; }
        public GraphImageValue BaseColor { get; }
        internal GraphMaterialValue(MaterialParameters parameters,GraphImageValue image) { Parameters=parameters;BaseColor=image; }
    }
    internal static class MaterialEvaluation
    {
        internal static GraphMeshValue Assign(GraphMeshValue mesh,GraphMaterialValue material)
        {
            Checks.Require(mesh.Material==null && mesh.SlotMaterials==null && mesh.BaseColor==null,"MATERIAL_ALREADY_ASSIGNED","Assign a material once, after geometry edits. Feed the image into the material node.");
            var bound=PaintEvaluation.Bind(mesh,material.BaseColor);
            return new GraphMeshValue(bound.Mesh,bound.Transform,bound.DomainId,bound.Polygon,bound.PolygonRendering,bound.BaseColor,material);
        }
    }
}
