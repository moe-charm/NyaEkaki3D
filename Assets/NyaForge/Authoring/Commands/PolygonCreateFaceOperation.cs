using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation CreatePolygonFace(GraphEditContext context,ulong[] perimeter,int material=0)
        {
            Checks.Require(context!=null && perimeter!=null && perimeter.Length>=3 && perimeter.Length<=256,"INVALID_SELECTION","Specify an ordered face perimeter.");
            Checks.Require(material>=0 && material<AuthoringLimits.MaxSubmeshes,"INVALID_FACE","Material slot is outside budget.");
            return new AuthoringOperation("graph.polygon.create-face",Array.Empty<int>(),new Vec3(material,0,0),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])perimeter.Clone()) };
        }
    }
}
