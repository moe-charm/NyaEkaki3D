using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation MergePolygonFaces(GraphEditContext context,ulong[] faces)
        {
            Checks.Require(context!=null && faces!=null && faces.Length==2,"INVALID_SELECTION","Select two faces.");
            return new AuthoringOperation("graph.polygon.merge-faces",Array.Empty<int>(),new Vec3(),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])faces.Clone()) };
        }
    }
}
