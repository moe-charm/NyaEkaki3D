using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class PolygonExtrusion
    {
        /// <summary>Extrudes a selected face region. Caps keep face/corner IDs; boundary walls receive new IDs.</summary>
        public static PolygonMesh Extrude(PolygonMesh mesh, IEnumerable<ulong> faceIds, Vec3 delta)
        {
            Checks.Finite(delta); Checks.Require(faceIds != null, "INVALID_SELECTION", "Face selection is required.");
            var ids = faceIds.Take(AuthoringLimits.MaxIndices / 3 + 1).ToArray(); var selected = new HashSet<ulong>(ids);
            Checks.Require(ids.Length > 0 && selected.Count == ids.Length && ids.Length <= mesh.Faces.Count, "INVALID_SELECTION", "Select distinct existing faces.");
            var chosen = mesh.Faces.Where(f => selected.Contains(f.Id)).OrderBy(f => f.Id).ToArray();
            var byFace = mesh.Faces.ToDictionary(f => f.Id);
            Checks.Require(chosen.Length == ids.Length, "INVALID_SELECTION", "Selected face is missing.");
            PolygonRenderAdapter.Build(mesh);
            var boundaries = new List<(CageFace face, CageCorner a, CageCorner b)>();
            foreach (var face in chosen)
            {
                var normal = FaceNormal(mesh, face);
                double distance = (double)normal.X * delta.X + (double)normal.Y * delta.Y + (double)normal.Z * delta.Z;
                Checks.Require(Math.Abs(distance) > 1e-8, "INVALID_EXTRUSION", "Extrusion must leave each selected face plane.");
                for (int i = 0; i < face.Corners.Count; i++)
                {
                    var a = face.Corners[i]; var b = face.Corners[(i + 1) % face.Corners.Count];
                    var edge = new CageEdgeId(a.VertexId, b.VertexId); var incident = mesh.EdgeFaces[edge];
                    Checks.Require(incident.Count <= 2, "NONMANIFOLD_SELECTION", "Selected region contains a nonmanifold edge.");
                    if (incident.Count == 2)
                    {
                        var other = byFace[incident.First(id => id != face.Id)];
                        bool reversed = Enumerable.Range(0, other.Corners.Count).Any(j => other.Corners[j].VertexId == b.VertexId && other.Corners[(j + 1) % other.Corners.Count].VertexId == a.VertexId);
                        Checks.Require(reversed, "INCONSISTENT_WINDING", "Faces sharing an edge must have opposite edge directions.");
                    }
                    if (incident.Count(id => selected.Contains(id)) == 1) boundaries.Add((face, a, b));
                }
            }
            var liftedIds = chosen.SelectMany(f => f.Corners).Select(c => c.VertexId).Distinct().OrderBy(id => id).ToArray();
            Checks.Require(mesh.Vertices.Count + liftedIds.Length <= AuthoringLimits.MaxVertices && mesh.Faces.Count + boundaries.Count <= AuthoringLimits.MaxIndices / 3 &&
                mesh.Faces.Sum(f => (long)f.Corners.Count) + boundaries.Count * 4L <= AuthoringLimits.MaxIndices, "BUDGET_EXCEEDED", "Extrusion exceeds mesh budget.");
            ulong vertexId = mesh.IdWatermarks.Vertex, faceId = mesh.IdWatermarks.Face, cornerId = mesh.IdWatermarks.Corner;
            var vertices = mesh.Vertices.Values.ToList(); var lifted = new Dictionary<ulong, ulong>();
            foreach (ulong id in liftedIds) { ulong next = Next(ref vertexId); lifted.Add(id, next); vertices.Add(new CageVertex(next, mesh.Vertices[id].Position + delta)); }
            var faces = mesh.Faces.Select(f => !selected.Contains(f.Id) ? f : new CageFace(f.Id, f.Material,
                f.Corners.Select(c => new CageCorner(c.Id, lifted[c.VertexId], c.Uv0, c.Normal, c.Tangent)))).ToList();
            foreach (var edge in boundaries)
            {
                var a = mesh.Vertices[edge.a.VertexId].Position; var b = mesh.Vertices[edge.b.VertexId].Position;
                Vec3 direction = b - a, tangent = Unit(direction), normal = Unit(Cross(direction, delta));
                float width = Length(direction), height = Length(delta);
                ulong[] vertexRefs = { edge.a.VertexId, edge.b.VertexId, lifted[edge.b.VertexId], lifted[edge.a.VertexId] };
                Vec2[] uv = { new Vec2(), new Vec2(width, 0), new Vec2(width, height), new Vec2(0, height) };
                var corners = new CageCorner[4];
                for (int i = 0; i < 4; i++) corners[i] = new CageCorner(Next(ref cornerId), vertexRefs[i], edge.a.Uv0.HasValue ? uv[i] : (Vec2?)null,
                    edge.a.Normal.HasValue ? normal : (Vec3?)null, edge.a.Tangent.HasValue ? new Vec4(tangent.X, tangent.Y, tangent.Z, 1) : (Vec4?)null);
                faces.Add(new CageFace(Next(ref faceId), edge.face.Material, corners));
            }
            var result = new PolygonMesh(mesh.DomainId, vertices, faces,mesh.IdWatermarks); PolygonRenderAdapter.Build(result); return result;
        }
        static ulong Next(ref ulong value) { Checks.Require(value < ulong.MaxValue, "ELEMENT_ID_EXHAUSTED", "No element IDs remain."); return ++value; }
        static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        static float Length(Vec3 v) { double n = Math.Sqrt((double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z); Checks.Require(n > 1e-12 && n <= float.MaxValue, "INVALID_EXTRUSION", "Invalid extrusion edge or vector."); return (float)n; }
        static Vec3 Unit(Vec3 v) => v * (1 / Length(v));
        public static Vec3 FaceNormal(PolygonMesh mesh, CageFace face)
        {
            var origin = mesh.Vertices[face.Corners[0].VertexId].Position; var normal = new Vec3();
            for (int i = 1; i < face.Corners.Count - 1; i++) normal += Cross(mesh.Vertices[face.Corners[i].VertexId].Position - origin, mesh.Vertices[face.Corners[i + 1].VertexId].Position - origin);
            return Unit(normal);
        }
    }
}
