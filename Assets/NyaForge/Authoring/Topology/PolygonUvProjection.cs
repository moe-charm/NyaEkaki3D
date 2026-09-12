using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    /// <summary>Projects each face onto its dominant plane and packs independent islands into a grid.</summary>
    public static class PolygonUvProjection
    {
        public static PolygonMesh Apply(PolygonMesh mesh)
        {
            PolygonRenderAdapter.Build(mesh);
            var ordered = mesh.Faces.OrderBy(f => f.Id).ToArray();
            int columns = (int)Math.Ceiling(Math.Sqrt(ordered.Length));
            int rows = (ordered.Length + columns - 1) / columns;
            float cell = 1f / Math.Max(columns, rows);
            var faces = new List<CageFace>();
            for (int index = 0; index < ordered.Length; index++)
            {
                var face = ordered[index];
                var geometricNormal = PolygonExtrusion.FaceNormal(mesh, face);
                float x = Math.Abs(geometricNormal.X), y = Math.Abs(geometricNormal.Y), z = Math.Abs(geometricNormal.Z);
                Vec3 u = x >= y && x >= z ? new Vec3(0, 1, 0) : new Vec3(1, 0, 0);
                Vec3 v = z >= x && z >= y ? new Vec3(0, 1, 0) : new Vec3(0, 0, 1);
                // Resolve ties using the same axis decision for both basis vectors.
                if (x >= y && x >= z) v = new Vec3(0, 0, 1);
                else if (y >= z) v = new Vec3(0, 0, 1);
                var projected = face.Corners.Select(c => mesh.Vertices[c.VertexId].Position).Select(p => new Vec2(Dot(p, u), Dot(p, v))).ToArray();
                float minU = projected.Min(p => p.X), minV = projected.Min(p => p.Y);
                float width = projected.Max(p => p.X) - minU, height = projected.Max(p => p.Y) - minV;
                float extent = Math.Max(width, height);
                Checks.Require(extent > 1e-12f && float.IsFinite(extent), "INVALID_UV_PROJECTION", "Face projection has no usable extent.");
                float scale = cell * .9f / extent;
                float originU = index % columns * cell + cell * .05f + (extent - width) * scale * .5f;
                float originV = index / columns * cell + cell * .05f + (extent - height) * scale * .5f;
                var direction = Cross(v, geometricNormal);
                if (Dot(direction, u) < 0) direction = direction * -1;
                var corners = new CageCorner[face.Corners.Count];
                for (int i = 0; i < corners.Length; i++)
                {
                    var c = face.Corners[i]; Vec4? tangent = null;
                    if (c.Tangent.HasValue)
                    {
                        var normal = Unit(c.Normal ?? geometricNormal);
                        var t = Unit(direction - normal * Dot(normal, direction));
                        tangent = new Vec4(t.X, t.Y, t.Z, Dot(Cross(normal, t), v) < 0 ? -1 : 1);
                    }
                    corners[i] = new CageCorner(c.Id, c.VertexId,
                        new Vec2(originU + (projected[i].X - minU) * scale, originV + (projected[i].Y - minV) * scale), c.Normal, tangent);
                }
                faces.Add(new CageFace(face.Id, face.Material, corners));
            }
            var result = new PolygonMesh(mesh.DomainId, mesh.Vertices.Values, faces,mesh.IdWatermarks);
            PolygonRenderAdapter.Build(result); return result;
        }
        static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
        static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y*b.Z-a.Z*b.Y,a.Z*b.X-a.X*b.Z,a.X*b.Y-a.Y*b.X);
        static Vec3 Unit(Vec3 v)
        {
            double length = Math.Sqrt((double)v.X*v.X + (double)v.Y*v.Y + (double)v.Z*v.Z);
            Checks.Require(length > 1e-12 && length <= float.MaxValue, "INVALID_UV_PROJECTION", "Cannot derive a tangent frame.");
            return v * (float)(1 / length);
        }
    }
}
