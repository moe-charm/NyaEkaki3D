using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public sealed class UvTransformSettings
    {
        public Vec2 Translation { get; }
        public float Degrees { get; }
        public float Scale { get; }
        public UvTransformSettings(Vec2 translation, float degrees = 0, float scale = 1)
        {
            Checks.Finite(translation.X); Checks.Finite(translation.Y); Checks.Finite(degrees); Checks.Finite(scale);
            Checks.Require(scale >= .000001f && scale <= 1000000, "INVALID_UV_SCALE", "UV scale must be positive and bounded.");
            Translation = translation; Degrees = degrees; Scale = scale;
        }
    }

    public static class UvIslandTransform
    {
        public static PolygonMesh Apply(PolygonMesh mesh, IEnumerable<ulong> seeds, UvTransformSettings settings)
        {
            Checks.Require(settings != null, "INVALID_UV_TRANSFORM", "UV transform is required.");
            var selected = UvIslands.Expand(mesh, seeds);
            var points = mesh.Faces.Where(f => selected.Contains(f.Id)).SelectMany(f => f.Corners).Select(c => c.Uv0.Value).ToArray();
            double cx = ((double)points.Min(p => p.X) + points.Max(p => p.X)) / 2;
            double cy = ((double)points.Min(p => p.Y) + points.Max(p => p.Y)) / 2;
            double radians = (settings.Degrees % 360) * Math.PI / 180, cosine = Math.Cos(radians), sine = Math.Sin(radians);
            var faces = mesh.Faces.Select(face => !selected.Contains(face.Id) ? face : new CageFace(face.Id, face.Material, face.Corners.Select(c =>
            {
                var uv = c.Uv0.Value; double u = uv.X - cx, v = uv.Y - cy;
                var next = new Vec2((float)(cx + (u * cosine - v * sine) * settings.Scale + settings.Translation.X),
                    (float)(cy + (u * sine + v * cosine) * settings.Scale + settings.Translation.Y));
                return new CageCorner(c.Id, c.VertexId, next, c.Normal, c.Tangent);
            }))).ToArray();
            var result = new PolygonMesh(mesh.DomainId, mesh.Vertices.Values, faces,mesh.IdWatermarks);
            if (settings.Degrees % 360 != 0) result = PolygonTangents.Recalculate(result, selected);
            PolygonRenderAdapter.Build(result); return result;
        }
    }
}
