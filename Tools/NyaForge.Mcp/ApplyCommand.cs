using System.Text.Json.Serialization;
namespace NyaForge.Mcp;

public sealed class ApplyCommand
{
    public required string expectedInstanceId { get; init; }
    public required string documentId { get; init; }
    public required long expectedDocumentRevision { get; init; }
    public required string commandId { get; init; }
    public required string objectId { get; init; }
    public required string expectedBaselineHash { get; init; }
    public required ApplyOperation[] operations { get; init; }
}
public sealed class ApplyOperation
{
    public required string kind { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? path { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? sourceHash { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public bool? fit { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public int? target { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? strength { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public LayerContextCommand? layerContext { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? layerId { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? name { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public int? width { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public int? height { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public int? index { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? opacity { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public bool? visible { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public PaintContextCommand? paintContext { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double[][]? points { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? radius { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public int[]? color { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double[]? position { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? thickness { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public int? materialSlot { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? materialNodeId { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string[]? elementIds { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public EditContextCommand? context { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? newObjectId { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public GraphCommand? graph { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public NodeCommand? node { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public int[]? vertexIds { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double[]? delta { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? nodeId { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? inputPort { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? fromNode { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? fromPort { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? toNode { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? toPort { get; init; }
}
