using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Geometry;

internal static partial class Program
{
    static void RunMeshSurfaceRegionTests()
    {
        Test("bone surface region selects flattened triangles near the segment", () =>
        {
            var mesh = new MeshData(
                new[]
                {
                    new Vec3(-.02f, 0, 0), new Vec3(.02f, 0, 0), new Vec3(0, .04f, 0),
                    new Vec3(.4f, 0, 0), new Vec3(.44f, 0, 0), new Vec3(.42f, .04f, 0),
                    new Vec3(-.02f, 0, .025f), new Vec3(.02f, 0, .025f), new Vec3(0, .04f, .025f)
                },
                new Vec3[0], new Vec4[0], new Vec2[0],
                new[] { new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 } });
            var selected = MeshSurfaceRegion.SelectTrianglesNearBone(mesh, new RestTransform(2f, new Vec3(1, 0, 0)),
                new Vec3(.96f, 0, 0), new Vec3(1.04f, 0, 0), .06f);
            True(selected.SequenceEqual(new[] { 0, 2 }));
        });

        Test("bone surface region validates radius and preserves submesh flattening", () =>
        {
            var mesh = new MeshData(new[] { new Vec3(0, 0, 0), new Vec3(1, 0, 0), new Vec3(0, 1, 0) },
                new Vec3[0], new Vec4[0], new Vec2[0], new[] { new[] { 0, 1, 2 } });
            var selected = MeshSurfaceRegion.SelectTrianglesNearBone(mesh, new RestTransform(1, new Vec3()),
                new Vec3(0, 0, -.01f), new Vec3(0, 0, .01f), 1.1f);
            Equal(1, selected.Length); Equal(0, selected[0]);
            try
            {
                MeshSurfaceRegion.SelectTrianglesNearBone(mesh, new RestTransform(1, new Vec3()),
                    new Vec3(), new Vec3(0, 0, 1), 0);
                True(false);
            }
            catch (AuthoringException error) { Equal("INVALID_SURFACE_REGION", error.Code); }
        });
    }
}
