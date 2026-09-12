namespace NyaForge.Mcp;

public sealed class PolygonVertexCommand
{
    public required string id { get; init; }
    public required double[] position { get; init; }
}
public sealed class PolygonFaceCommand
{
    public required string id { get; init; }
    public required int materialSlot { get; init; }
    public required PolygonCornerCommand[] corners { get; init; }
}
public sealed class PolygonCornerCommand
{
    public required string id { get; init; }
    public required string vertexId { get; init; }
}
