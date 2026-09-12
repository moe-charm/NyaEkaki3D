using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonMeshTests()
    {
        Test("triangle adapter preserves split source vertices and all corner attributes", () =>
        {
            var source = AuthoringFixtures.Panel(100); var cage = TriangleMeshAdapter.Import(GraphId(), source); var rendered = PolygonRenderAdapter.Build(cage);
            Equal(source.VertexCount, cage.Vertices.Count); Equal(source.TriangleCount, cage.Faces.Count); Equal(source.VertexCount, rendered.Mesh.VertexCount);
            Equal(source.Submeshes.Count, rendered.Mesh.Submeshes.Count);
            for (int i = 0; i < rendered.RenderVertexMap.Count; i++)
            {
                int original = (int)rendered.RenderVertexMap[i].VertexId - 1;
                Equal(source.Positions[original], rendered.Mesh.Positions[i]); Equal(source.Normals[original], rendered.Mesh.Normals[i]);
                Equal(source.Tangents[original], rendered.Mesh.Tangents[i]); Equal(source.Uv0[original], rendered.Mesh.Uv0[i]);
            }
        });
        Test("polygon quad keeps stable IDs and exact render element mappings", () =>
        {
            var verts = new[] { new CageVertex(11, new Vec3(0, 0, 0)), new CageVertex(22, new Vec3(1, 0, 0)), new CageVertex(33, new Vec3(1, 1, 0)), new CageVertex(44, new Vec3(0, 1, 0)) };
            var face = new CageFace(100, 0, verts.Select((v, i) => new CageCorner((ulong)(201 + i), v.Id)));
            var cage = new PolygonMesh(GraphId(), verts, new[] { face }); var render = PolygonRenderAdapter.Build(cage);
            Equal(4, render.Mesh.VertexCount); Equal(2, render.Mesh.TriangleCount); Equal(4, cage.EdgeFaces.Count);
            True(render.RenderTriangleMap.All(id => id == 100)); Equal(0, render.Mesh.Normals.Count);
            True(render.RenderVertexMap.SelectMany(v => v.CornerIds).OrderBy(v => v).SequenceEqual(new ulong[] { 201, 202, 203, 204 }));
            var moved = cage.MoveVertices(new ulong[] { 11 }, new Vec3(.1f, 0, 0));
            Equal(cage.DomainId, moved.DomainId); True(cage.EdgeFaces.Keys.All(moved.EdgeFaces.ContainsKey));
            Near(0, cage.Vertices[11].Position.X); Near(.1f, moved.Vertices[11].Position.X);
            Equal(render.Mesh.ContentHash, PolygonRenderAdapter.Build(new PolygonMesh(cage.DomainId, verts.Reverse(), new[] { face })).Mesh.ContentHash);
        });
        Test("corner UV seams split rendering but shared authoring vertex moves together", () =>
        {
            var vertices = new[] { new CageVertex(1, new Vec3()), new CageVertex(2, new Vec3(1, 0, 0)), new CageVertex(3, new Vec3(1, 1, 0)), new CageVertex(4, new Vec3(0, 1, 0)) };
            var faces = new[] {
                new CageFace(1, 0, new[] { new CageCorner(1, 1, new Vec2()), new CageCorner(2, 2, new Vec2(1, 0)), new CageCorner(3, 3, new Vec2(1, 1)) }),
                new CageFace(2, 1, new[] { new CageCorner(4, 1, new Vec2(.5f, 0)), new CageCorner(5, 3, new Vec2(1, 1)), new CageCorner(6, 4, new Vec2(0, 1)) }) };
            var mesh = new PolygonMesh(GraphId(), vertices, faces); var original = PolygonRenderAdapter.Build(mesh);
            Equal(5, original.Mesh.VertexCount); Equal(2, mesh.EdgeFaces[new CageEdgeId(1, 3)].Count);
            var result = PolygonRenderAdapter.Build(mesh.MoveVertices(new ulong[] { 1 }, new Vec3(.1f, 0, 0)));
            var mapped = result.RenderVertexMap.Select((value, index) => (value, index)).Where(p => p.value.VertexId == 1).ToArray();
            Equal(2, mapped.Length); foreach (var p in mapped) Near(.1f, result.Mesh.Positions[p.index].X);
            False(result.Mesh.Uv0[mapped[0].index].X == result.Mesh.Uv0[mapped[1].index].X);
            Equal((ulong)1, result.RenderTriangleMap[0]); Equal((ulong)2, result.RenderTriangleMap[1]);
        });
        Test("concave polygon triangulates without filling its notch", () =>
        {
            var positions = new[] { new Vec3(0, 0, 0), new Vec3(2, 0, 0), new Vec3(2, 2, 0), new Vec3(1, 1, 0), new Vec3(0, 2, 0) };
            var mesh = new PolygonMesh(GraphId(), positions.Select((p, i) => new CageVertex((ulong)i + 1, p)),
                new[] { new CageFace(1, 0, positions.Select((_, i) => new CageCorner((ulong)i + 1, (ulong)i + 1))) });
            var output = PolygonRenderAdapter.Build(mesh).Mesh; Equal(3, output.TriangleCount);
            float area = 0; var indices = output.Submeshes[0];
            for (int i = 0; i < indices.Length; i += 3) { var a = output.Positions[indices[i]]; var b = output.Positions[indices[i + 1]]; var c = output.Positions[indices[i + 2]]; area += ((b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X)) / 2; }
            Near(3, area);
        });
        Test("polygon model rejects invalid references and crossing boundaries", () =>
        {
            var v = new[] { new CageVertex(1, new Vec3()), new CageVertex(2, new Vec3(1, 1, 0)), new CageVertex(3, new Vec3(0, 1, 0)), new CageVertex(4, new Vec3(1, 0, 0)) };
            var face = new CageFace(1, 0, v.Select(p => new CageCorner(p.Id, p.Id)));
            Expect("SELF_INTERSECTING_FACE", () => PolygonRenderAdapter.Build(new PolygonMesh(GraphId(), v, new[] { face })));
            Expect("INVALID_ELEMENT_ID", () => new PolygonMesh(GraphId(), v.Take(3), new[] { face }));
            Expect("INVALID_FACE", () => new CageFace(2, 0, new[] { new CageCorner(1, 1), new CageCorner(2, 1), new CageCorner(3, 2) }));
        });
    }
}
