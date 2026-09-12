using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation InsertPolygonEdgeVertex(GraphEditContext context,ulong[] vertices,float fraction=.5f)
        {
            Checks.Require(context!=null && vertices!=null && vertices.Length==2,"INVALID_SELECTION","Select two edge endpoints.");
            return new AuthoringOperation("graph.polygon.insert-edge-vertex",Array.Empty<int>(),new Vec3(fraction,0,0),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])vertices.Clone()) };
        }
    }
}
