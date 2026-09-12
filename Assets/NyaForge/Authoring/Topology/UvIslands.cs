using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public static class UvIslands
    {
        public static HashSet<ulong> Expand(PolygonMesh mesh, IEnumerable<ulong> seedFaces)
        {
            Checks.Require(seedFaces != null, "INVALID_SELECTION", "UV face selection is required.");
            var faces = mesh.Faces.ToDictionary(f => f.Id);
            var seeds = seedFaces.Take(AuthoringLimits.MaxIndices / 3 + 1).ToArray();
            Checks.Require(seeds.Length > 0 && seeds.Length <= faces.Count && seeds.Distinct().Count() == seeds.Length && seeds.All(faces.ContainsKey), "INVALID_SELECTION", "Select distinct existing faces.");
            Checks.Require(mesh.Faces.All(f => f.Corners.All(c => c.Uv0.HasValue)), "UV_MISSING", "Project UVs before editing islands.");
            var edges = new Dictionary<CageEdgeId, List<(ulong face, Vec2 a, Vec2 b)>>();
            foreach (var face in mesh.Faces)
                for (int i = 0; i < face.Corners.Count; i++)
                {
                    var a = face.Corners[i]; var b = face.Corners[(i + 1) % face.Corners.Count];
                    var key = new CageEdgeId(a.VertexId, b.VertexId);
                    if (!edges.TryGetValue(key, out var entries)) edges.Add(key, entries = new List<(ulong, Vec2, Vec2)>());
                    entries.Add((face.Id, a.VertexId == key.A ? a.Uv0.Value : b.Uv0.Value, a.VertexId == key.A ? b.Uv0.Value : a.Uv0.Value));
                }
            var links = faces.Keys.ToDictionary(id => id, _ => new List<ulong>());
            // Group by exact endpoint UVs to avoid a quadratic nonmanifold-edge scan.
            foreach (var entries in edges.Values)
                foreach (var group in entries.GroupBy(e => (e.a.X, e.a.Y, e.b.X, e.b.Y)))
                {
                    ulong first = group.First().face;
                    foreach (var entry in group.Skip(1)) { links[first].Add(entry.face); links[entry.face].Add(first); }
                }
            var result = new HashSet<ulong>(); var pending = new Stack<ulong>(seeds);
            while (pending.Count > 0)
            {
                ulong id = pending.Pop();
                if (result.Add(id)) foreach (ulong neighbor in links[id]) pending.Push(neighbor);
            }
            return result;
        }
    }
}
