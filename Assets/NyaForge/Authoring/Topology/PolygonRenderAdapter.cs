using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    public sealed class RenderVertexBinding
    {
        public ulong VertexId { get; }
        public IReadOnlyList<ulong> CornerIds { get; }
        internal RenderVertexBinding(ulong vertex, IEnumerable<ulong> corners) { VertexId = vertex; CornerIds = Array.AsReadOnly(corners.ToArray()); }
    }
    public sealed class PolygonRenderMesh
    {
        public MeshData Mesh { get; }
        public string DomainId { get; }
        public IReadOnlyList<RenderVertexBinding> RenderVertexMap { get; }
        // Concatenated submesh triangle order, matching MeshData.Submeshes.
        public IReadOnlyList<ulong> RenderTriangleMap { get; }
        // Render submeshes are dense; authored material slots are stable and may have gaps.
        public IReadOnlyList<int> MaterialSlotMap { get; }
        internal PolygonRenderMesh(string domain, MeshData mesh, RenderVertexBinding[] vertices, ulong[] faces,int[] materialSlots)
        { DomainId = domain; Mesh = mesh; RenderVertexMap = Array.AsReadOnly(vertices); RenderTriangleMap = Array.AsReadOnly(faces); MaterialSlotMap=Array.AsReadOnly(materialSlots); }
    }
    public static class PolygonRenderAdapter
    {
        public static PolygonRenderMesh Build(PolygonMesh source)=>PolygonDerivedData.Render(source);
        internal static PolygonRenderMesh BuildUncached(PolygonMesh source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            Checks.Require(source.Faces.Count>0,"NO_RENDERABLE_FACES","Polygon contains editing points but no renderable faces.");
            var positions = new List<Vec3>(); var normals = new List<Vec3>(); var tangents = new List<Vec4>(); var uv = new List<Vec2>();
            var vertexIds = new List<ulong>(); var cornerIds = new List<List<ulong>>();
            var lookup = new Dictionary<(ulong, Vec2?, Vec3?, Vec4?), int>();
            var materialSlots=source.Faces.Select(f=>f.Material).Distinct().OrderBy(slot=>slot).ToArray();
            var submeshForSlot=materialSlots.Select((slot,index)=>(slot,index)).ToDictionary(p=>p.slot,p=>p.index);
            int slots = materialSlots.Length;
            var indices = Enumerable.Range(0, slots).Select(_ => new List<int>()).ToArray();
            var faces = Enumerable.Range(0, slots).Select(_ => new List<ulong>()).ToArray();
            int totalIndices = 0;
            foreach (var face in source.Faces.OrderBy(f => f.Id))
            {
                int[] local = new int[face.Corners.Count];
                for (int i = 0; i < local.Length; i++)
                {
                    var c = face.Corners[i]; var key = (c.VertexId, c.Uv0, c.Normal, c.Tangent);
                    if (!lookup.TryGetValue(key, out int index))
                    {
                        Checks.Require(positions.Count < AuthoringLimits.MaxVertices, "BUDGET_EXCEEDED", "Render vertex splitting exceeds budget.");
                        index = positions.Count; lookup.Add(key, index); positions.Add(source.Vertices[c.VertexId].Position);
                        if (c.Uv0.HasValue) uv.Add(c.Uv0.Value); if (c.Normal.HasValue) normals.Add(c.Normal.Value); if (c.Tangent.HasValue) tangents.Add(c.Tangent.Value);
                        vertexIds.Add(c.VertexId); cornerIds.Add(new List<ulong>());
                    }
                    cornerIds[index].Add(c.Id); local[i] = index;
                }
                var triangles = PolygonTriangulator.Triangulate(source, face); totalIndices += triangles.Length;
                Checks.Require(totalIndices <= AuthoringLimits.MaxIndices, "BUDGET_EXCEEDED", "Render triangle budget exceeded.");
                int submesh=submeshForSlot[face.Material];
                indices[submesh].AddRange(triangles.Select(i => local[i]));
                faces[submesh].AddRange(Enumerable.Repeat(face.Id, triangles.Length / 3));
            }
            var mesh = new MeshData(positions.ToArray(), normals.ToArray(), tangents.ToArray(), uv.ToArray(), indices.Select(s => s.ToArray()).ToArray());
            return new PolygonRenderMesh(source.DomainId, mesh, vertexIds.Select((id, i) => new RenderVertexBinding(id, cornerIds[i])).ToArray(), faces.SelectMany(f => f).ToArray(),materialSlots);
        }
    }
}

