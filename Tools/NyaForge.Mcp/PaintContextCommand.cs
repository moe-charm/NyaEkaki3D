namespace NyaForge.Mcp;
public sealed class PaintContextCommand
{
    public required string graphId { get; init; }
    public required string nodeId { get; init; }
    public required string imageHash { get; init; }
    public required string uvHash { get; init; }
    public required string meshDomain { get; init; }
}
