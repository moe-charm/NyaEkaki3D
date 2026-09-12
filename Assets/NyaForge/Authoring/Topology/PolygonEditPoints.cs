using System;
using System.Linq;

namespace NyaForge.Authoring.Topology
{
    /// <summary>Editing point order retains the render prefix and appends loose vertices by stable ID.
    /// It does not change the rendered or exported triangle mesh.</summary>
    public static class PolygonEditPoints
    {
        public static ulong[] VertexIds(PolygonMesh polygon)
        {
            if(polygon==null) return Array.Empty<ulong>();
            if(polygon.Faces.Count==0) return polygon.Vertices.Keys.OrderBy(id=>id).ToArray();
            var rendered=PolygonRenderAdapter.Build(polygon).RenderVertexMap.Select(v=>v.VertexId).ToArray();
            var used=rendered.ToHashSet();
            return rendered.Concat(polygon.Vertices.Keys.Where(id=>!used.Contains(id)).OrderBy(id=>id)).ToArray();
        }
    }
}

