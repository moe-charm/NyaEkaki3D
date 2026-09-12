using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.UnityRuntime
{
    public sealed partial class OwnedMeshProjection
    {
        Prepared springProjection;
        GraphMeshValue springAppearance;
        SpringMeshBuffers springBuffers;

        public void ShowSpringPreview(GraphMeshValue output)
        {
            if (current != null && ReferenceEquals(current, springProjection) && CompatibleSpringOutput(springAppearance, output))
            {
                springBuffers.Apply(current.Mesh, output.Mesh, output.Transform);
                current.Points = springBuffers.Points; current.MeshHash = output.Mesh.ContentHash;
                springAppearance = output;
                return;
            }
            using (var prepared = PrepareMesh(output.Mesh, output.Transform, null, output.BaseColor, output.Material?.Parameters, output))
                prepared.Commit();
            // Final-output playback has no editable point handles. Restore rebuilds them once.
            current.PointMarkers?.Dispose(); current.PointMarkers = null;
            current.Mesh.MarkDynamic();
            springProjection = current; springAppearance = output; springBuffers = new SpringMeshBuffers(output.Mesh);
        }

        public void EndSpringPreview()
        {
            if (current != null && ReferenceEquals(current, springProjection)) current.MeshHash = null;
            springProjection = null; springAppearance = null; springBuffers = null;
        }

        static bool CompatibleSpringOutput(GraphMeshValue a, GraphMeshValue b)
        {
            if (a.Mesh.TopologyHash != b.Mesh.TopologyHash || !a.Transform.Equals(b.Transform)
                || a.Mesh.Normals.Count != b.Mesh.Normals.Count || a.Mesh.Tangents.Count != b.Mesh.Tangents.Count
                || !a.Mesh.Uv0.SequenceEqual(b.Mesh.Uv0) || a.PolygonRendering != null || b.PolygonRendering != null
                || a.BaseColor?.ImageHash != b.BaseColor?.ImageHash || a.Material?.Parameters.ContentHash != b.Material?.Parameters.ContentHash
                || a.Material?.BaseColor?.ImageHash != b.Material?.BaseColor?.ImageHash) return false;
            if (a.SlotMaterials == null || b.SlotMaterials == null) return a.SlotMaterials == null && b.SlotMaterials == null;
            return a.SlotMaterials.Count == b.SlotMaterials.Count && a.SlotMaterials.All(pair => b.SlotMaterials.TryGetValue(pair.Key, out var other)
                && pair.Value.Material.Parameters.ContentHash == other.Material.Parameters.ContentHash
                && pair.Value.Material.BaseColor?.ImageHash == other.Material.BaseColor?.ImageHash);
        }
    }
}
