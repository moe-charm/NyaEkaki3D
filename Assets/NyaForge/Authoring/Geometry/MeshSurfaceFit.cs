using System;

namespace NyaForge.Authoring.Geometry
{
    /// <summary>Bounded rest-space clothing projection onto an avatar surface.</summary>
    public static class MeshSurfaceFit
    {
        public static Vec3[] ProjectPositions(
            MeshData clothingMesh, RestTransform clothingTransform,
            MeshData avatarMesh, RestTransform avatarTransform,
            float surfaceOffset, float maxDistance)
        {
            Checks.Require(clothingMesh != null && avatarMesh != null, "INVALID_SURFACE_FIT", "Clothing and avatar meshes are required.");
            clothingTransform.Validate(); avatarTransform.Validate();
            Checks.Finite(surfaceOffset); Checks.Finite(maxDistance);
            Checks.Require(maxDistance > 0f && maxDistance <= 10f, "INVALID_SURFACE_FIT", "Surface fit distance must be between zero and 10 metres.");
            Checks.Require(Math.Abs(surfaceOffset) <= maxDistance, "INVALID_SURFACE_FIT", "Surface offset cannot exceed the fit distance.");
            var projection = new MeshSurfaceProjection(avatarMesh, avatarTransform);
            var result = new Vec3[clothingMesh.VertexCount];
            for (int vertex = 0; vertex < clothingMesh.VertexCount; vertex++)
            {
                Vec3 worldPoint = clothingTransform.ToAvatarPoint(clothingMesh.Positions[vertex]);
                MeshSurfaceHit hit = projection.FindClosest(worldPoint);
                Checks.Require(hit.DistanceSquared <= (double)maxDistance * maxDistance,
                    "SURFACE_FIT_DISTANCE", "A clothing vertex is farther from the avatar surface than the configured limit.");
                Vec3 a = avatarMesh.Positions[hit.A], b = avatarMesh.Positions[hit.B], c = avatarMesh.Positions[hit.C];
                Vec3 nearestLocal = a * (float)hit.U + b * (float)hit.V + c * (float)hit.W;
                Vec3 edgeA = b - a, edgeB = c - a;
                Vec3 normal = Cross(edgeA, edgeB);
                float length = (float)Math.Sqrt(Dot(normal, normal));
                Checks.Require(length > 1e-7f, "DEGENERATE_TRIANGLE", "Avatar surface contains a degenerate triangle.");
                normal = normal * (1f / length);
                Vec3 targetWorld = avatarTransform.ToAvatarPoint(nearestLocal) + normal * (surfaceOffset * avatarTransform.Scale);
                result[vertex] = clothingTransform.ToLocalVector(targetWorld - clothingTransform.Translation);
                Checks.Finite(result[vertex]);
            }
            return result;
        }

        static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }
        static double Dot(Vec3 a, Vec3 b) { return (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z; }
    }
}
