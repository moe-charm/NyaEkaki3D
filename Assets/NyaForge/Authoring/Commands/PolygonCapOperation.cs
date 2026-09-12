using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation FillPolygonBoundary(GraphEditContext context,ulong[] vertices)
        {
            Checks.Require(context!=null && vertices!=null && vertices.Length<=256,"INVALID_SELECTION","Choose a bounded polygon boundary.");
            return new AuthoringOperation("graph.polygon.cap",Array.Empty<int>(),new Vec3(),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])vertices.Clone()) };
        }
    }
}
