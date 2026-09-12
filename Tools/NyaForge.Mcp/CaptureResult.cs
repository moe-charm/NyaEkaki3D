using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
namespace NyaForge.Mcp;
internal static class CaptureResult
{
    public static CallToolResult Convert(System.Text.Json.JsonElement value)
    {
        var summary=JsonNode.Parse(value.GetRawText())!.AsObject();summary.Remove("images");
        var blocks=new List<ContentBlock> {new TextContentBlock {Text=summary.ToJsonString()}};
        foreach(var image in value.GetProperty("images").EnumerateArray())
            blocks.Add(ImageContentBlock.FromBytes(System.Convert.FromBase64String(image.GetProperty("data").GetString()!),"image/png"));
        return new CallToolResult {Content=blocks,StructuredContent=System.Text.Json.JsonSerializer.SerializeToElement(summary)};
    }
}
