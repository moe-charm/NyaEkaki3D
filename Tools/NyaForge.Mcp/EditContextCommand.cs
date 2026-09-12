namespace NyaForge.Mcp;
public sealed class EditContextCommand
{
    public required string graphId { get; init; }
    public required string nodeId { get; init; }
    public required string inputSnapshot { get; init; }
    public required string domainId { get; init; }
}
