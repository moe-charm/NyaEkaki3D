using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunChokerPrimitiveTests()
    {
        Test("choker primitive has closed quad topology and render attributes", () =>
        {
            var mesh = PolygonPrimitives.Choker(Guid.NewGuid().ToString("D"));
            Equal(24 * 8, mesh.Vertices.Count);
            Equal(24 * 8, mesh.Faces.Count);
            True(mesh.Faces.All(face => face.Corners.Count == 4));
            True(mesh.EdgeFaces.Values.All(faces => faces.Count == 2));
            var rendered = PolygonRenderAdapter.Build(mesh);
            Equal(24 * 8 * 2, rendered.Mesh.TriangleCount);
            Equal(rendered.Mesh.VertexCount, rendered.Mesh.Normals.Count);
            Equal(rendered.Mesh.VertexCount, rendered.Mesh.Tangents.Count);
            Equal(rendered.Mesh.VertexCount, rendered.Mesh.Uv0.Count);
            True(rendered.RenderVertexMap.Any(binding => binding.CornerIds.Count > 1));
        });
        Test("choker primitive rejects unsafe parameters", () =>
        {
            Expect("PARAMETER_RANGE", () => PolygonPrimitives.Choker(Guid.NewGuid().ToString("D"), .005f));
            Expect("PARAMETER_RANGE", () => PolygonPrimitives.Choker(Guid.NewGuid().ToString("D"), .06f, .04f));
            Expect("PARAMETER_RANGE", () => PolygonPrimitives.Choker(Guid.NewGuid().ToString("D"), .06f, .008f, 4));
        });
        Test("cuff primitive has a closed shell and render attributes", () =>
        {
            var mesh = PolygonPrimitives.Cuff(Guid.NewGuid().ToString("D"));
            Equal(32 * 4, mesh.Vertices.Count);
            Equal(32 * 4, mesh.Faces.Count);
            True(mesh.Faces.All(face => face.Corners.Count == 4));
            True(mesh.EdgeFaces.Values.All(faces => faces.Count == 2));
            var rendered = PolygonRenderAdapter.Build(mesh);
            Equal(32 * 4 * 2, rendered.Mesh.TriangleCount);
            Equal(rendered.Mesh.VertexCount, rendered.Mesh.Normals.Count);
            Equal(rendered.Mesh.VertexCount, rendered.Mesh.Tangents.Count);
            Equal(rendered.Mesh.VertexCount, rendered.Mesh.Uv0.Count);
        });
        Test("cuff primitive geometric winding agrees with supplied normals", () =>
        {
            var mesh = PolygonPrimitives.Cuff(Guid.NewGuid().ToString("D"));
            foreach (var face in mesh.Faces)
            {
                var a = mesh.Vertices[face.Corners[0].VertexId].Position;
                var b = mesh.Vertices[face.Corners[1].VertexId].Position;
                var c = mesh.Vertices[face.Corners[2].VertexId].Position;
                var geometric = new Vec3(
                    (b.Y - a.Y) * (c.Z - a.Z) - (b.Z - a.Z) * (c.Y - a.Y),
                    (b.Z - a.Z) * (c.X - a.X) - (b.X - a.X) * (c.Z - a.Z),
                    (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X));
                var supplied = face.Corners[0].Normal.Value;
                True(geometric.X * supplied.X + geometric.Y * supplied.Y + geometric.Z * supplied.Z > 0f);
            }
        });
        Test("cuff primitive rejects unsafe parameters", () =>
        {
            Expect("PARAMETER_RANGE", () => PolygonPrimitives.Cuff(Guid.NewGuid().ToString("D"), .005f));
            Expect("PARAMETER_RANGE", () => PolygonPrimitives.Cuff(Guid.NewGuid().ToString("D"), .04f, .002f));
            Expect("PARAMETER_RANGE", () => PolygonPrimitives.Cuff(Guid.NewGuid().ToString("D"), .04f, .035f, .05f));
            Expect("PARAMETER_RANGE", () => PolygonPrimitives.Cuff(Guid.NewGuid().ToString("D"), .04f, .035f, .004f, 4));
        });
    }
}
