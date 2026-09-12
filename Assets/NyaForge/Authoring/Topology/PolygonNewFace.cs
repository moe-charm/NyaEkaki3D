using System.Linq;

namespace NyaForge.Authoring.Topology
{
    internal static class PolygonNewFace
    {
        internal static CageFace Project(PolygonMesh mesh,CageFace bare)
        {
            var normal=PolygonExtrusion.FaceNormal(mesh,bare);var format=mesh.Faces.FirstOrDefault()?.Corners[0] ?? new CageCorner(1,1,new Vec2(),new Vec3(0,0,1),new Vec4(1,0,0,1));
            var face=new CageFace(bare.Id,bare.Material,bare.Corners.Select(c=>new CageCorner(c.Id,c.VertexId,new Vec2(),
                format.Normal.HasValue ? normal : (Vec3?)null,format.Tangent.HasValue ? new Vec4(1,0,0,1) : (Vec4?)null)));
            var projected=PolygonUvProjection.Apply(new PolygonMesh(mesh.DomainId,face.Corners.Select(c=>mesh.Vertices[c.VertexId]),new[]{face})).Faces[0];
            return format.Uv0.HasValue ? projected : new CageFace(projected.Id,projected.Material,projected.Corners.Select(c=>new CageCorner(c.Id,c.VertexId,null,c.Normal,c.Tangent)));
        }
    }
}

