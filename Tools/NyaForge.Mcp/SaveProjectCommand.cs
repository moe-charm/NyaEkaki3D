namespace NyaForge.Mcp;
public sealed class SaveProjectCommand
{
    public required string documentId { get; init; }
    public required long expectedRevision { get; init; }
    public required string directory { get; init; }
    public required long expectedSaveVersion { get; init; }
}
