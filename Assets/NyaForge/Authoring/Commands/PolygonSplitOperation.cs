using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation SplitPolygonFace(GraphEditContext context,ulong[] vertices)
        {
            Checks.Require(context!=null && vertices!=null && vertices.Length==2,"INVALID_SELECTION","Select two face vertices.");
            return new AuthoringOperation("graph.polygon.split-face",Array.Empty<int>(),new Vec3(),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])vertices.Clone()) };
        }
    }
}
