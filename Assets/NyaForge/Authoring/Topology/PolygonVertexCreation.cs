using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonVertexCreation
    {
        /// <summary>Add an unconnected authoring vertex in mesh-local coordinates.</summary>
        public static PolygonMesh Add(PolygonMesh mesh,Vec3 position)
        {
            Checks.Require(mesh!=null,"INVALID_MESH","A polygon mesh is required.");Checks.Finite(position);
            Checks.Require(mesh.IdWatermarks.Vertex<ulong.MaxValue,"ELEMENT_ID_EXHAUSTED","No vertex IDs remain.");
            Checks.Require(mesh.Vertices.Count<AuthoringLimits.MaxVertices,"BUDGET_EXCEEDED","Vertex budget exceeded.");
            return new PolygonMesh(mesh.DomainId,mesh.Vertices.Values.Concat(new[]{new CageVertex(mesh.IdWatermarks.Vertex+1,position)}),mesh.Faces,mesh.IdWatermarks);
        }
    }
}
