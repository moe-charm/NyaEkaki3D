using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation WeldPolygonVertices(GraphEditContext context,ulong[] vertices)
        {
            Checks.Require(context!=null && vertices!=null && vertices.Length>=2 && vertices.Length<=AuthoringLimits.MaxVertices,"INVALID_SELECTION","Select vertices to weld.");
            return new AuthoringOperation("graph.polygon.weld",Array.Empty<int>(),new Vec3(),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])vertices.Clone()) };
        }
    }
}
