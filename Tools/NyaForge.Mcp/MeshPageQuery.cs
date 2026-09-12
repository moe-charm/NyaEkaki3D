namespace NyaForge.Mcp;

public sealed class MeshPageQuery
{
    public required string documentId { get; init; }
    public required long revision { get; init; }
    public required string nodeId { get; init; }
    public required string port { get; init; }
    public required string snapshotHash { get; init; }
    public required int offset { get; init; }
    public required int count { get; init; }
}

