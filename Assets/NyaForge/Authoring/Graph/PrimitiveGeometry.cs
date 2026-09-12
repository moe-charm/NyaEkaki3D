namespace NyaForge.Authoring.Graph
{
    internal static class PrimitiveGeometry
    {
        internal static void ValidatePlane(float width, float height)
        {
            Checks.Finite(width); Checks.Finite(height);
            Checks.Require(width >= .001f && height >= .001f && width <= 100 && height <= 100,
                "PARAMETER_RANGE", "Plane dimensions must be between 0.001 and 100 metres.");
        }
        internal static MeshData Plane(float width, float height)
        {
            ValidatePlane(width, height);
            float x = width / 2, y = height / 2;
            return new MeshData(
                new[] { new Vec3(-x,-y,0), new Vec3(x,-y,0), new Vec3(x,y,0), new Vec3(-x,y,0) },
                new[] { new Vec3(0,0,-1), new Vec3(0,0,-1), new Vec3(0,0,-1), new Vec3(0,0,-1) },
                new[] { new Vec4(1,0,0,-1), new Vec4(1,0,0,-1), new Vec4(1,0,0,-1), new Vec4(1,0,0,-1) },
                new[] { new Vec2(0,0), new Vec2(1,0), new Vec2(1,1), new Vec2(0,1) },
                new[] { new[] { 0,2,1,0,3,2 } });
        }
    }
}
