using System;
using System.Linq;
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
    }
}
