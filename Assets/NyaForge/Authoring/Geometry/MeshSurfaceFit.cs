using System;

namespace NyaForge.Authoring.Geometry
{
    /// <summary>Measured result of one bounded rest-space surface fit.</summary>
    public sealed class MeshSurfaceFitResult
    {
        public Vec3[] Positions { get; }
        /// <summary>Number of clothing vertices whose world position changed by more than 1e-7 metres.</summary>
        public int MovedVertexCount { get; }
        /// <summary>Largest distance from an original clothing vertex to its nearest avatar triangle, in metres.</summary>
        public float MaxProjectionDistance { get; }
        /// <summary>Mean distance from original clothing vertices to their nearest avatar triangle, in metres.</summary>
        public float AverageProjectionDistance { get; }
        /// <summary>Largest world-space displacement caused by the fit, in metres.</summary>
        public float MaxDisplacement { get; }
        /// <summary>Mean world-space displacement caused by the fit, in metres.</summary>
        public float AverageDisplacement { get; }

        internal MeshSurfaceFitResult(Vec3[] positions, int movedVertexCount, float maxProjectionDistance,
            float averageProjectionDistance, float maxDisplacement, float averageDisplacement)
        {
            Positions = positions ?? throw new ArgumentNullException(nameof(positions));
            MovedVertexCount = movedVertexCount;
            MaxProjectionDistance = maxProjectionDistance;
            AverageProjectionDistance = averageProjectionDistance;
            MaxDisplacement = maxDisplacement;
            AverageDisplacement = averageDisplacement;
        }
    }

    /// <summary>Bounded rest-space clothing projection onto an avatar surface.</summary>
    public static class MeshSurfaceFit
    {
        public static MeshSurfaceFitResult Project(
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
            int moved = 0;
            double projectionDistanceTotal = 0d, displacementTotal = 0d;
            float maxProjectionDistance = 0f, maxDisplacement = 0f;
            for (int vertex = 0; vertex < clothingMesh.VertexCount; vertex++)
            {
                Vec3 worldPoint = clothingTransform.ToAvatarPoint(clothingMesh.Positions[vertex]);
                MeshSurfaceHit hit = projection.FindClosest(worldPoint);
                double projectionDistanceSquared = hit.DistanceSquared;
                Checks.Require(projectionDistanceSquared <= (double)maxDistance * maxDistance,
                    "SURFACE_FIT_DISTANCE", "A clothing vertex is farther from the avatar surface than the configured limit.");
                float projectionDistance = (float)Math.Sqrt(projectionDistanceSquared);
                maxProjectionDistance = Math.Max(maxProjectionDistance, projectionDistance);
                projectionDistanceTotal += projectionDistance;
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
                Vec3 displacement = targetWorld - worldPoint;
                float displacementLength = (float)Math.Sqrt(Dot(displacement, displacement));
                maxDisplacement = Math.Max(maxDisplacement, displacementLength);
                displacementTotal += displacementLength;
                if (displacementLength > 1e-7f) moved++;
            }
            float count = clothingMesh.VertexCount;
            return new MeshSurfaceFitResult(result, moved, maxProjectionDistance,
                count == 0 ? 0f : (float)(projectionDistanceTotal / count), maxDisplacement,
                count == 0 ? 0f : (float)(displacementTotal / count));
        }

        public static Vec3[] ProjectPositions(
            MeshData clothingMesh, RestTransform clothingTransform,
            MeshData avatarMesh, RestTransform avatarTransform,
            float surfaceOffset, float maxDistance)
            => Project(clothingMesh, clothingTransform, avatarMesh, avatarTransform, surfaceOffset, maxDistance).Positions;

        static Vec3 Cross(Vec3 a, Vec3 b)
        {
            return new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        }
        static double Dot(Vec3 a, Vec3 b) { return (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z; }
    }
}
