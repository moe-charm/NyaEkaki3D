using System.Collections.Generic;
using NyaForge.Authoring.Import;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Bridges a renderable graph value to the source-space skin deformer.</summary>
    /// <remarks>
    /// This is deliberately an adapter rather than a new persisted graph node. The graph keeps
    /// its authored BoneId/4-influence contract; complete source slots and bind matrices remain
    /// in ImportedRigSession. Callers must provide a palette in the source world space.
    /// </remarks>
    public static class SourceSkinGraphAdapter
    {
        public static GraphMeshValue Apply(GraphMeshValue input, SourceSkin skin,
            SourceSkinBinding binding, IReadOnlyList<SourceAffine> posedJointWorld)
        {
            Checks.Require(input != null && input.Mesh != null, "INPUT_UNRESOLVED", "Source skin needs a renderable graph mesh.");
            Checks.Require(input.Polygon == null, "EDIT_MODE_UNSUPPORTED", "Source skin cannot deform a polygon editing value.");
            var mesh = SourceSkinDeformer.Apply(input.Mesh, skin, binding, posedJointWorld);
            return input.WithMesh(mesh);
        }
    }
}
