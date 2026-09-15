using System;
using System.Collections.Generic;

namespace NyaForge.Authoring.Geometry
{
    /// <summary>Deterministically selects avatar triangles near an authored bone segment.</summary>
    public static class MeshSurfaceRegion
    {
        /// <summary>
        /// Returns flattened triangle indices whose vertices or centroid are within
        /// <paramref name="radius"/> metres of the bone segment. Mesh positions are
        /// converted to avatar-rest space before measuring, so the same selector is
        /// valid for imported affine placements and for fit/weight callers.
        /// </summary>
        public static int[] SelectTrianglesNearBone(
            MeshData mesh, RestTransform transform, Vec3 head, Vec3 tail, float radius)
        {
            Checks.Require(mesh != null, "INVALID_SURFACE_REGION", "Avatar mesh is required.");
            transform.Validate(); Checks.Finite(head); Checks.Finite(tail); Checks.Finite(radius);
            Checks.Require(radius > 0f && radius <= 10f, "INVALID_SURFACE_REGION", "Surface region radius must be between zero and 10 metres.");
            var positions = new Vec3[mesh.VertexCount];
            for (int i = 0; i < positions.Length; i++) positions[i] = transform.ToAvatarPoint(mesh.Positions[i]);
            double radiusSquared = (double)radius * radius;
            var selected = new List<int>();
            int triangle = 0;
            foreach (var submesh in mesh.Submeshes)
            {
                Checks.Require(submesh.Length % 3 == 0, "INVALID_SURFACE_REGION", "Avatar submesh triangle indices are incomplete.");
                for (int i = 0; i < submesh.Length; i += 3, triangle++)
                {
                    int ia = submesh[i], ib = submesh[i + 1], ic = submesh[i + 2];
                    Checks.Require(ia >= 0 && ia < positions.Length && ib >= 0 && ib < positions.Length && ic >= 0 && ic < positions.Length,
                        "INVALID_SURFACE_REGION", "Avatar triangle index is outside the mesh domain.");
                    Vec3 a = positions[ia], b = positions[ib], c = positions[ic];
                    Vec3 center = (a + b + c) * (1f / 3f);
                    if (DistanceSquaredToSegment(a, head, tail) <= radiusSquared ||
                        DistanceSquaredToSegment(b, head, tail) <= radiusSquared ||
                        DistanceSquaredToSegment(c, head, tail) <= radiusSquared ||
                        DistanceSquaredToSegment(center, head, tail) <= radiusSquared)
                        selected.Add(triangle);
                }
            }
            return selected.ToArray();
        }

        static double DistanceSquaredToSegment(Vec3 point, Vec3 start, Vec3 end)
        {
            Vec3 segment = end - start;
            double lengthSquared = Dot(segment, segment);
            if (lengthSquared <= 1e-14) return Dot(point - start, point - start);
            double t = Dot(point - start, segment) / lengthSquared;
            t = Math.Max(0d, Math.Min(1d, t));
            Vec3 closest = start + segment * (float)t;
            return Dot(point - closest, point - closest);
        }

        static double Dot(Vec3 a, Vec3 b) => (double)a.X * b.X + (double)a.Y * b.Y + (double)a.Z * b.Z;
    }
}
