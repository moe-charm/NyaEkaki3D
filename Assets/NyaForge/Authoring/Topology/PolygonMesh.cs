using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public sealed class CageVertex
    {
        public ulong Id { get; }
        public Vec3 Position { get; }
        public CageVertex(ulong id, Vec3 position) { ElementIdentity.Check(id); Checks.Finite(position); Id = id; Position = position; }
    }
    public sealed class CageCorner
    {
        public ulong Id { get; }
        public ulong VertexId { get; }
        public Vec2? Uv0 { get; }
        public Vec3? Normal { get; }
        public Vec4? Tangent { get; }
        public CageCorner(ulong id, ulong vertexId, Vec2? uv0 = null, Vec3? normal = null, Vec4? tangent = null)
        {
            ElementIdentity.Check(id); ElementIdentity.Check(vertexId);
            if (uv0.HasValue) { Checks.Finite(uv0.Value.X); Checks.Finite(uv0.Value.Y); }
            if (normal.HasValue) Checks.Finite(normal.Value);
            if (tangent.HasValue) { var v = tangent.Value; Checks.Finite(v.X); Checks.Finite(v.Y); Checks.Finite(v.Z); Checks.Require(v.W == 1 || v.W == -1, "INVALID_MESH", "Tangent handedness must be +/-1."); }
            Id = id; VertexId = vertexId; Uv0 = uv0; Normal = normal; Tangent = tangent;
        }
    }
    public sealed class CageFace
    {
        public ulong Id { get; }
        public int Material { get; }
        public IReadOnlyList<CageCorner> Corners { get; }
        public CageFace(ulong id, int material, IEnumerable<CageCorner> corners)
        {
            ElementIdentity.Check(id); Checks.Require(corners != null, "INVALID_FACE", "Face corners are required.");
            var items = corners.Take(257).ToArray();
            Checks.Require(items.Length >= 3 && items.Length <= 256 && items.All(c => c != null), "INVALID_FACE", "Face requires 3..256 corners.");
            Checks.Require(material >= 0 && material < AuthoringLimits.MaxSubmeshes, "INVALID_FACE", "Material slot is outside budget.");
            Checks.Require(items.Select(c => c.VertexId).Distinct().Count() == items.Length, "INVALID_FACE", "A face cannot revisit a vertex.");
            Id = id; Material = material; Corners = Array.AsReadOnly(items);
        }
    }
    internal static class ElementIdentity
    {
        internal static void Check(ulong id) { Checks.Require(id != 0, "INVALID_ELEMENT_ID", "Element ID zero is reserved."); }
    }
    /// <summary>An edge is identified by its unordered stable endpoint IDs within the mesh domain.</summary>
    public readonly struct CageEdgeId : IEquatable<CageEdgeId>
    {
        public readonly ulong A, B;
        public CageEdgeId(ulong a, ulong b) { ElementIdentity.Check(a); ElementIdentity.Check(b); Checks.Require(a != b, "INVALID_EDGE", "Edge endpoints must differ."); A = Math.Min(a, b); B = Math.Max(a, b); }
        public bool Equals(CageEdgeId other) => A == other.A && B == other.B;
        public override bool Equals(object obj) => obj is CageEdgeId other && Equals(other);
        public override int GetHashCode() => unchecked(A.GetHashCode() * 397 ^ B.GetHashCode());
    }

    /// <summary>Immutable polygon/corner authoring data. Array indices are never element identities.</summary>
    public sealed class PolygonMesh
    {
        public string DomainId { get; }
        public PolygonIdWatermarks IdWatermarks { get; }
        internal PolygonIdWatermarks LiveIdMaxima { get; }
        public IReadOnlyDictionary<ulong, CageVertex> Vertices { get; }
        public IReadOnlyList<CageFace> Faces { get; }
        public IReadOnlyDictionary<CageEdgeId, IReadOnlyList<ulong>> EdgeFaces { get; }
        public PolygonMesh(string domainId, IEnumerable<CageVertex> vertices, IEnumerable<CageFace> faces,PolygonIdWatermarks priorIds=null)
        {
            Checks.Id(domainId); Checks.Require(vertices != null && faces != null, "INVALID_MESH", "Mesh collections are required.");
            var verts = vertices.Take(AuthoringLimits.MaxVertices + 1).ToArray(); var polygons = faces.Take(AuthoringLimits.MaxIndices / 3 + 1).ToArray();
            Checks.Require(verts.Length <= AuthoringLimits.MaxVertices && polygons.Length <= AuthoringLimits.MaxIndices / 3, "BUDGET_EXCEEDED", "Polygon mesh capacity exceeded.");
            var byId = new Dictionary<ulong, CageVertex>();
            foreach (var v in verts) { Checks.Require(v != null && !byId.ContainsKey(v.Id), "INVALID_ELEMENT_ID", "Duplicate or missing vertex."); byId.Add(v.Id, v); }
            var faceIds = new HashSet<ulong>(); var cornerIds = new HashSet<ulong>(); var edges = new Dictionary<CageEdgeId, List<ulong>>();
            int count = 0; CageCorner first = null;
            foreach (var face in polygons)
            {
                Checks.Require(face != null && faceIds.Add(face.Id), "INVALID_ELEMENT_ID", "Duplicate or missing face.");
                count += face.Corners.Count; Checks.Require(count <= AuthoringLimits.MaxIndices, "BUDGET_EXCEEDED", "Corner budget exceeded.");
                for (int i = 0; i < face.Corners.Count; i++)
                {
                    var c = face.Corners[i]; if (first == null) first = c;
                    Checks.Require(byId.ContainsKey(c.VertexId) && cornerIds.Add(c.Id), "INVALID_ELEMENT_ID", "Corner references a missing vertex or duplicate corner.");
                    Checks.Require(c.Uv0.HasValue == first.Uv0.HasValue && c.Normal.HasValue == first.Normal.HasValue && c.Tangent.HasValue == first.Tangent.HasValue, "INVALID_MESH", "Corner attributes must be uniformly present or absent.");
                    var edge = new CageEdgeId(c.VertexId, face.Corners[(i + 1) % face.Corners.Count].VertexId);
                    if (!edges.TryGetValue(edge, out var incident)) edges.Add(edge, incident = new List<ulong>()); incident.Add(face.Id);
                }
            }
            DomainId = domainId; Vertices = new ReadOnlyDictionary<ulong, CageVertex>(byId); Faces = Array.AsReadOnly(polygons);
            LiveIdMaxima=new PolygonIdWatermarks(byId.Keys.DefaultIfEmpty(0UL).Max(),faceIds.DefaultIfEmpty(0UL).Max(),cornerIds.DefaultIfEmpty(0UL).Max());
            IdWatermarks=priorIds==null ? LiveIdMaxima : priorIds.Include(LiveIdMaxima);
            EdgeFaces = new ReadOnlyDictionary<CageEdgeId, IReadOnlyList<ulong>>(edges.ToDictionary(p => p.Key, p => (IReadOnlyList<ulong>)p.Value.AsReadOnly()));
        }
        public PolygonMesh MoveVertices(IEnumerable<ulong> ids, Vec3 delta)
        {
            Checks.Finite(delta); Checks.Require(ids != null, "INVALID_SELECTION", "Selection required.");
            var items = ids.Take(AuthoringLimits.MaxVertices + 1).ToArray(); var selected = new HashSet<ulong>(items);
            Checks.Require(items.Length > 0 && items.Length <= AuthoringLimits.MaxVertices && selected.Count == items.Length && selected.All(Vertices.ContainsKey), "INVALID_SELECTION", "Select distinct existing vertex IDs.");
            return new PolygonMesh(DomainId, Vertices.Values.Select(v => selected.Contains(v.Id) ? new CageVertex(v.Id, v.Position + delta) : v), Faces,IdWatermarks);
        }
    }
}

