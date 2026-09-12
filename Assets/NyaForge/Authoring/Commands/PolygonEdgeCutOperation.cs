using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation CutPolygonBetweenEdges(GraphEditContext context,ulong[] endpoints,float first=.5f,float second=.5f)
        {
            Checks.Require(context!=null && endpoints!=null && endpoints.Length==4,"INVALID_SELECTION","Specify two edge endpoint pairs.");
            Checks.Finite(first);Checks.Finite(second);
            return new AuthoringOperation("graph.polygon.cut-edges",Array.Empty<int>(),new Vec3(first,second,0),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])endpoints.Clone()) };
        }
    }
}
