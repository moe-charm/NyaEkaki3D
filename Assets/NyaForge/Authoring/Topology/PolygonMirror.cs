using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    /// <summary>Original plus reflected geometry. Reflection reverses winding and tangent handedness.</summary>
    public static class PolygonMirror
    {
        public static PolygonMesh Apply(PolygonMesh source, string outputDomain, int axis, float plane)
        {
            Checks.Require(axis >= 0 && axis <= 2, "INVALID_MIRROR_AXIS", "Mirror axis must be X, Y or Z."); Checks.Finite(plane);
            Checks.Require(source.Vertices.Count * 2L <= AuthoringLimits.MaxVertices && source.Faces.Sum(f => (long)f.Corners.Count) * 2 <= AuthoringLimits.MaxIndices,
                "BUDGET_EXCEEDED", "Mirror exceeds mesh capacity.");
            PolygonRenderAdapter.Build(source);
            ulong vertex = source.IdWatermarks.Vertex, face = source.IdWatermarks.Face, corner = source.IdWatermarks.Corner;
            var vertices = source.Vertices.Values.ToList(); var faces = source.Faces.ToList(); var reflected = new Dictionary<ulong, ulong>();
            foreach (var item in source.Vertices.Values.OrderBy(v => v.Id))
            {
                ulong id = Next(ref vertex); reflected.Add(item.Id,id);
                var p = Reflect(item.Position, axis);
                p += axis == 0 ? new Vec3(2*plane,0,0) : axis == 1 ? new Vec3(0,2*plane,0) : new Vec3(0,0,2*plane);
                vertices.Add(new CageVertex(id,p));
            }
            foreach (var item in source.Faces.OrderBy(f => f.Id))
            {
                var corners = new List<CageCorner>();
                foreach (var c in item.Corners.Reverse())
                {
                    Vec4? tangent = null;
                    if (c.Tangent.HasValue)
                    {
                        var t = c.Tangent.Value; var direction = Reflect(new Vec3(t.X,t.Y,t.Z),axis);
                        tangent = new Vec4(direction.X,direction.Y,direction.Z,-t.W);
                    }
                    corners.Add(new CageCorner(Next(ref corner), reflected[c.VertexId],c.Uv0,c.Normal.HasValue ? Reflect(c.Normal.Value,axis) : (Vec3?)null,tangent));
                }
                faces.Add(new CageFace(Next(ref face),item.Material,corners));
            }
            var result = new PolygonMesh(outputDomain,vertices,faces,source.IdWatermarks); PolygonRenderAdapter.Build(result); return result;
        }
        static Vec3 Reflect(Vec3 v,int axis) => new Vec3(axis == 0 ? -v.X : v.X,axis == 1 ? -v.Y : v.Y,axis == 2 ? -v.Z : v.Z);
        static ulong Next(ref ulong id) { Checks.Require(id < ulong.MaxValue,"ELEMENT_ID_EXHAUSTED","No element IDs remain."); return ++id; }
    }
}
