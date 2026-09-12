using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation DeletePolygonFaces(GraphEditContext context,ulong[] faceIds)
        {
            Checks.Require(context!=null && faceIds!=null && faceIds.Length<=AuthoringLimits.MaxIndices/3,"INVALID_SELECTION","Bounded face selection is required.");
            return new AuthoringOperation("graph.polygon.delete-faces",Array.Empty<int>(),new Vec3(),false)
                { EditContext=context,ElementIds=Array.AsReadOnly((ulong[])faceIds.Clone()) };
        }
    }
}
