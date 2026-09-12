using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    /// <summary>Creates an inward shell using averaged geometric vertex normals and boundary walls.</summary>
    public static class PolygonSolidify
    {
        public static PolygonMesh Apply(PolygonMesh mesh, float thickness)
        {
            Checks.Finite(thickness);
            Checks.Require(thickness > 1e-8f, "INVALID_THICKNESS", "Thickness must be positive.");
            PolygonRenderAdapter.Build(mesh);
            var faces = mesh.Faces.OrderBy(f => f.Id).ToArray();
            var normals = new Dictionary<ulong, Vec3>();
            var uses = new Dictionary<ulong, List<(CageFace face, CageCorner a, CageCorner b)>>();
            var edgeUses = new Dictionary<CageEdgeId, List<(CageFace face, CageCorner a, CageCorner b)>>();
            foreach (var face in faces)
            {
                var normal = PolygonExtrusion.FaceNormal(mesh, face);
                for (int i = 0; i < face.Corners.Count; i++)
                {
                    var a = face.Corners[i]; var b = face.Corners[(i + 1) % face.Corners.Count];
                    normals.TryGetValue(a.VertexId, out var sum); normals[a.VertexId] = sum + normal;
                    var use = (face, a, b);
                    if (!uses.TryGetValue(a.VertexId, out var vertexUses)) uses.Add(a.VertexId, vertexUses = new List<(CageFace, CageCorner, CageCorner)>());
                    vertexUses.Add(use);
                    var edge = new CageEdgeId(a.VertexId, b.VertexId);
                    if (!edgeUses.TryGetValue(edge, out var entries)) edgeUses.Add(edge, entries = new List<(CageFace, CageCorner, CageCorner)>());
                    entries.Add(use);
                }
            }
            foreach (var entries in edgeUses.Values)
            {
                Checks.Require(entries.Count <= 2, "NONMANIFOLD_SELECTION", "Thickness requires a manifold surface.");
                if (entries.Count == 2) Checks.Require(entries[0].a.VertexId == entries[1].b.VertexId && entries[0].b.VertexId == entries[1].a.VertexId,
                    "INCONSISTENT_WINDING", "Adjacent faces must have opposite edge directions.");
            }
            // A vertex must have a single fan; two surfaces touching at one vertex are not a shell.
            foreach (var vertex in uses)
            {
                var adjacency = vertex.Value.ToDictionary(u => u.face.Id, _ => new HashSet<ulong>());
                int boundary = 0;
                foreach (var use in vertex.Value)
                {
                    var corners = use.face.Corners; int i = Enumerable.Range(0, corners.Count).First(j => corners[j].VertexId == vertex.Key);
                    foreach (ulong neighbor in new[] { corners[(i + corners.Count - 1) % corners.Count].VertexId, use.b.VertexId })
                    {
                        var incident = mesh.EdgeFaces[new CageEdgeId(vertex.Key, neighbor)];
                        if (incident.Count == 1) boundary++;
                        foreach (ulong id in incident) if (id != use.face.Id) adjacency[use.face.Id].Add(id);
                    }
                }
                var visited = new HashSet<ulong>(); var pending = new Stack<ulong>(); pending.Push(vertex.Value[0].face.Id);
                while (pending.Count > 0) { ulong id = pending.Pop(); if (visited.Add(id)) foreach (ulong next in adjacency[id]) pending.Push(next); }
                Checks.Require((boundary == 0 || boundary == 2) && visited.Count == vertex.Value.Count, "NONMANIFOLD_SELECTION", "Vertex has disconnected surface fans.");
            }
            var boundaries = edgeUses.Values.Where(e => e.Count == 1).Select(e => e[0]).ToArray();
            long cornersCount = faces.Sum(f => (long)f.Corners.Count) * 2 + boundaries.Length * 4L;
            Checks.Require(mesh.Vertices.Count + normals.Count <= AuthoringLimits.MaxVertices && faces.Length * 2L + boundaries.Length <= AuthoringLimits.MaxIndices / 3 && cornersCount <= AuthoringLimits.MaxIndices,
                "BUDGET_EXCEEDED", "Thickness exceeds mesh budget.");
            ulong vertexId = mesh.IdWatermarks.Vertex, faceId = mesh.IdWatermarks.Face, cornerId = mesh.IdWatermarks.Corner;
            var vertices = mesh.Vertices.Values.ToList(); var inner = new Dictionary<ulong, ulong>();
            var positions = mesh.Vertices.ToDictionary(p => p.Key, p => p.Value.Position);
            foreach (var entry in normals.OrderBy(p => p.Key))
            {
                ulong id = Next(ref vertexId); inner.Add(entry.Key, id);
                var position = positions[entry.Key] - Unit(entry.Value) * thickness;
                positions.Add(id, position); vertices.Add(new CageVertex(id, position));
            }
            var resultFaces = faces.ToList();
            foreach (var face in faces)
                resultFaces.Add(new CageFace(Next(ref faceId), face.Material, face.Corners.Reverse().Select(c => new CageCorner(Next(ref cornerId), inner[c.VertexId], c.Uv0,
                    c.Normal.HasValue ? c.Normal.Value * -1 : (Vec3?)null,
                    c.Tangent.HasValue ? new Vec4(c.Tangent.Value.X, c.Tangent.Value.Y, c.Tangent.Value.Z, -c.Tangent.Value.W) : (Vec4?)null)).ToArray()));
            foreach (var edge in boundaries)
            {
                ulong[] ids = { edge.b.VertexId, edge.a.VertexId, inner[edge.a.VertexId], inner[edge.b.VertexId] };
                Vec3 direction = positions[ids[1]] - positions[ids[0]], depth = positions[ids[3]] - positions[ids[0]];
                var normal = Unit(Cross(direction, depth)); var tangent = Unit(direction);
                float width = Length(direction); var uv = new[] { new Vec2(), new Vec2(width, 0), new Vec2(width, thickness), new Vec2(0, thickness) };
                var corners = new CageCorner[4];
                for (int i = 0; i < 4; i++) corners[i] = new CageCorner(Next(ref cornerId), ids[i], edge.a.Uv0.HasValue ? uv[i] : (Vec2?)null,
                    edge.a.Normal.HasValue ? normal : (Vec3?)null, edge.a.Tangent.HasValue ? new Vec4(tangent.X, tangent.Y, tangent.Z, 1) : (Vec4?)null);
                resultFaces.Add(new CageFace(Next(ref faceId), edge.face.Material, corners));
            }
            var result = new PolygonMesh(mesh.DomainId, vertices, resultFaces,mesh.IdWatermarks);
            Checks.Require(result.EdgeFaces.Values.All(e => e.Count == 2), "INVALID_THICKNESS", "Thickness did not close the surface.");
            PolygonRenderAdapter.Build(result); return result;
        }
        static ulong Next(ref ulong id) { Checks.Require(id < ulong.MaxValue, "ELEMENT_ID_EXHAUSTED", "No element IDs remain."); return ++id; }
        static Vec3 Cross(Vec3 a, Vec3 b) => new Vec3(a.Y * b.Z - a.Z * b.Y, a.Z * b.X - a.X * b.Z, a.X * b.Y - a.Y * b.X);
        static float Length(Vec3 v) { double length = Math.Sqrt((double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z); Checks.Require(length > 1e-12 && length <= float.MaxValue, "INVALID_THICKNESS", "Surface has no usable offset direction."); return (float)length; }
        static Vec3 Unit(Vec3 v) => v * (1 / Length(v));
    }
}
