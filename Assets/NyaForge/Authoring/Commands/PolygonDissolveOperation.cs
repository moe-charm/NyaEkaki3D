using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation DissolvePolygonFaces(GraphEditContext context,ulong[] faces)
        {
            Checks.Require(context!=null && faces!=null && faces.Length>=2 && faces.Length<=AuthoringLimits.MaxIndices/3,"INVALID_SELECTION","Select connected coplanar faces.");
            return new AuthoringOperation("graph.polygon.dissolve-faces",Array.Empty<int>(),new Vec3(),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])faces.Clone()) };
        }
    }
}

