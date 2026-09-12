using System;
using System.Linq;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        ulong[] SelectedPolygonVertices()
        {
            var map=NyaForge.Authoring.Topology.PolygonEditPoints.VertexIds(DisplayedGraphValue()?.Polygon);
            return selection.Where(i=>i>=0 && i<map.Length).Select(i=>map[i]).Distinct().OrderBy(id=>id).ToArray();
        }
    }
}
