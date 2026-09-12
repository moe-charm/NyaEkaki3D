using System;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public UvTransformSettings UvTransform { get; private set; }
        public static AuthoringOperation TransformUvIslands(GraphEditContext context, ulong[] faces, UvTransformSettings settings)
        {
            Checks.Require(context != null && settings != null && faces != null && faces.Length <= AuthoringLimits.MaxIndices / 3, "INVALID_SELECTION", "Bounded UV selection and transform are required.");
            return new AuthoringOperation("graph.polygon.uv-transform", Array.Empty<int>(), new Vec3(), false)
            { EditContext = context, ElementIds = Array.AsReadOnly((ulong[])faces.Clone()), UvTransform = settings };
        }
    }
}
