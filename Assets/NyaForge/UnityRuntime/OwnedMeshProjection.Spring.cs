using NyaForge.Authoring.Graph;

namespace NyaForge.UnityRuntime
{
    public sealed partial class OwnedMeshProjection
    {
        public void ShowSpringPreview(GraphMeshValue output)
        {
            using (var prepared = PrepareMesh(output.Mesh, output.Transform, null, output.BaseColor, output.Material?.Parameters, output))
                prepared.Commit();
        }
    }
}
