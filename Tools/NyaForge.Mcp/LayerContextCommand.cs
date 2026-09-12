namespace NyaForge.Mcp;
public sealed class LayerContextCommand
{
    public required string graphId { get; init; }
    public required string nodeId { get; init; }
    public required string stackHash { get; init; }
    public required string uvHash { get; init; }
    public required string meshDomain { get; init; }
}
