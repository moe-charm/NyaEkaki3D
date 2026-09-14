namespace NyaForge.Mcp;

public sealed class ObjectLabelCommand
{
    public required string documentId { get; init; }
    public required long expectedRevision { get; init; }
    public required string expectedAttachmentsHash { get; init; }
    public required string objectId { get; init; }
    public required string displayName { get; init; }
}
