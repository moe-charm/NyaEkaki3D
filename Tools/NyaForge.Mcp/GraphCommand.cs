using System.Text.Json.Serialization;
namespace NyaForge.Mcp;
public sealed class GraphCommand
{
    public required string graphId { get; init; }
    public required string outputNodeId { get; init; }
    public required NodeCommand[] nodes { get; init; }
    public required EdgeCommand[] edges { get; init; }
}
public sealed class NodeCommand
{
    public required string nodeId { get; init; }
    public required string typeId { get; init; }
    public required int version { get; init; }
    public required NodeParameters parameters { get; init; }
}
public sealed class NodeParameters
{
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public int[]? slots { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? domainId { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public PolygonVertexCommand[]? vertices { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public PolygonFaceCommand[]? faces { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? scale { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double[]? translation { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double[]? baseColor { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? metallic { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? roughness { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double[]? emission { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public string? alphaMode { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? alphaCutoff { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? width { get; init; }
    [JsonIgnore(Condition=JsonIgnoreCondition.WhenWritingNull)] public double? height { get; init; }
}
public sealed class EdgeCommand
{
    public required string fromNode { get; init; }
    public required string fromPort { get; init; }
    public required string toNode { get; init; }
    public required string toPort { get; init; }
}
