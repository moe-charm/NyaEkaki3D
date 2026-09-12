using System;
using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation BridgePolygonBoundaries(GraphEditContext context,ulong[] first,ulong[] second,int offset=-1)
        {
            Checks.Require(context!=null && first!=null && second!=null && first.Length==second.Length && first.Length>=3 && first.Length<=256,"INVALID_BRIDGE","Choose two equal-size boundary loops.");
            return new AuthoringOperation("graph.polygon.bridge",Array.Empty<int>(),new Vec3(offset,0,0),false)
                { EditContext=context,ElementIds=Array.AsReadOnly(first.Concat(second).ToArray()) };
        }
    }
}
