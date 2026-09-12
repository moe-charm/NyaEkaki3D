using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonPrimitives
    {
        public static PolygonMesh Plane(string domainId, float width = .2f, float height = .1f)
        {
            var source = PrimitiveGeometry.Plane(width, height);
            var vertices = source.Positions.Select((p, i) => new CageVertex((ulong)i + 1, p));
            var corners = new[] { 0, 3, 2, 1 }.Select((v, i) => new CageCorner((ulong)i + 1, (ulong)v + 1, source.Uv0[v], source.Normals[v], source.Tangents[v]));
            return new PolygonMesh(domainId, vertices, new[] { new CageFace(1, 0, corners) });
        }
    }
}
