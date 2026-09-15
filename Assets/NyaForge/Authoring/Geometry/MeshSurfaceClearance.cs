using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Geometry
{
    /// <summary>
    /// Conservative rest-space surface clearance sample. A negative signed
    /// distance means that a clothing vertex lies behind the winding of its
    /// nearest avatar triangle. This is a candidate signal, not a proof that
    /// the two meshes intersect or that a closed avatar volume was entered.
    /// </summary>
    public sealed class MeshSurfaceClearanceResult
    {
        public int EvaluatedVertexCount { get; }
        public int BehindSurfaceVertexCount { get; }
        public float MinimumSignedDistance { get; }
        public float MaximumSignedDistance { get; }
        public IReadOnlyList<int> BehindSurfaceVertexIndices { get; }

        internal MeshSurfaceClearanceResult(int evaluatedVertexCount, int behindSurfaceVertexCount,
            float minimumSignedDistance, float maximumSignedDistance, IEnumerable<int> behindSurfaceVertexIndices)
        {
            EvaluatedVertexCount = evaluatedVertexCount;
            BehindSurfaceVertexCount = behindSurfaceVertexCount;
            MinimumSignedDistance = minimumSignedDistance;
            MaximumSignedDistance = maximumSignedDistance;
            BehindSurfaceVertexIndices = Array.AsReadOnly((behindSurfaceVertexIndices ?? Enumerable.Empty<int>()).ToArray());
        }
    }

    /// <summary>
    /// Samples clothing vertices against the nearest avatar surface. It is
    /// intentionally separate from MeshSurfaceFit so callers never mistake a
    /// bounded projection for a complete collision solver.
    /// </summary>
    public static class MeshSurfaceClearance
    {
        public const float DefaultToleranceMetres = 0.00001f;
        const int MaxReportedVertices = 64;

        public static MeshSurfaceClearanceResult Inspect(
            MeshData clothingMesh, RestTransform clothingTransform,
            MeshData avatarMesh, RestTransform avatarTransform,
            IEnumerable<int> clothingVertexIndices = null,
            IEnumerable<int> avatarTriangleIndices = null,
            float toleranceMetres = DefaultToleranceMetres)
        {
            Checks.Require(clothingMesh != null && avatarMesh != null, "INVALID_SURFACE_CLEARANCE", "Clothing and avatar meshes are required.");
            return InspectPositions(clothingMesh.Positions, clothingTransform, avatarMesh, avatarTransform,
                clothingVertexIndices, avatarTriangleIndices, toleranceMetres);
        }

        /// <summary>
        /// Inspects prospective clothing positions against the avatar surface.
        /// This is used after a bounded fit projection, before the edit is
        /// committed, so callers can show both the authored and projected
        /// candidate counts without manufacturing a temporary MeshData.
        /// </summary>
        public static MeshSurfaceClearanceResult InspectPositions(
            IReadOnlyList<Vec3> clothingPositions, RestTransform clothingTransform,
            MeshData avatarMesh, RestTransform avatarTransform,
            IEnumerable<int> clothingVertexIndices = null,
            IEnumerable<int> avatarTriangleIndices = null,
            float toleranceMetres = DefaultToleranceMetres)
        {
            Checks.Require(clothingPositions != null && avatarMesh != null, "INVALID_SURFACE_CLEARANCE", "Clothing positions and avatar mesh are required.");
            clothingTransform.Validate(); avatarTransform.Validate(); Checks.Finite(toleranceMetres);
            Checks.Require(toleranceMetres >= 0f && toleranceMetres <= 0.1f, "INVALID_SURFACE_CLEARANCE", "Surface clearance tolerance must be between zero and 0.1 metres.");
            HashSet<int> selected = clothingVertexIndices == null ? null : new HashSet<int>(clothingVertexIndices);
            if (selected != null)
            {
                Checks.Require(selected.Count > 0, "SELECTION_EMPTY", "At least one clothing vertex must be selected.");
                foreach (int index in selected)
                    Checks.Require(index >= 0 && index < clothingPositions.Count, "INVALID_VERTEX", "Selected clothing vertex is outside the mesh domain.");
            }
            var projection = new MeshSurfaceProjection(avatarMesh, avatarTransform, avatarTriangleIndices);
            int evaluated = 0, behind = 0;
            float minimum = float.PositiveInfinity, maximum = float.NegativeInfinity;
            var reported = new List<int>();
            for (int vertex = 0; vertex < clothingPositions.Count; vertex++)
            {
                if (selected != null && !selected.Contains(vertex)) continue;
                evaluated++;
                Checks.Finite(clothingPositions[vertex]);
                Vec3 worldPoint = clothingTransform.ToAvatarPoint(clothingPositions[vertex]);
                var hit = projection.FindClosest(worldPoint);
                Vec3 a = avatarMesh.Positions[hit.A], b = avatarMesh.Positions[hit.B], c = avatarMesh.Positions[hit.C];
                Vec3 edgeA = b - a, edgeB = c - a;
                Vec3 normal = Cross(edgeA, edgeB);
                float length = (float)Math.Sqrt(Dot(normal, normal));
                Checks.Require(length > 1e-7f, "DEGENERATE_TRIANGLE", "Avatar surface contains a degenerate triangle.");
                normal = normal * (1f / length);
                Vec3 surfaceLocal = a * (float)(1d - hit.U - hit.V) + b * (float)hit.U + c * (float)hit.V;
                Vec3 surfaceWorld = avatarTransform.ToAvatarPoint(surfaceLocal);
                float signed = Dot(worldPoint - surfaceWorld, normal);
                Checks.Finite(signed);
                minimum = Math.Min(minimum, signed); maximum = Math.Max(maximum, signed);
                if (signed < -toleranceMetres)
                {
                    behind++;
                    if (reported.Count < MaxReportedVertices) reported.Add(vertex);
                }
            }
            if (evaluated == 0) minimum = maximum = 0f;
            return new MeshSurfaceClearanceResult(evaluated, behind, minimum, maximum, reported);
        }

        static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }
}
