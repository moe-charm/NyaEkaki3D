using System;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring
{
    public sealed partial class AuthoringOperation
    {
        public static AuthoringOperation AddPolygonVertex(GraphEditContext context,Vec3 position)
        {
            Checks.Require(context!=null,"INVALID_SELECTION","Choose a polygon edit stage.");Checks.Finite(position);
            return new AuthoringOperation("graph.polygon.add-vertex",Array.Empty<int>(),position,false) { EditContext=context };
        }
    }
}
