using NyaForge.Authoring.Geometry;

namespace NyaForge.UnityRuntime
{
    /// <summary>
    /// Transient result passed between the attachment fit measurement and its
    /// UI/MCP projections. It deliberately carries no graph or scene state.
    /// </summary>
    sealed class AttachmentSurfaceFitMeasurement
    {
        public MeshSurfaceFitResult Result;
        public MeshSurfaceClearanceResult Clearance;
        public MeshSurfaceClearanceResult ProjectedClearance;
        public string TargetObjectId;
        public string Region;
        public string Vertices;
        public int[] TriangleIds;
        public int[] VertexIds;
        public float Offset;
        public float MaxDistance;
    }
}
