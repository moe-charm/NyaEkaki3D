using System.Text.Json.Nodes;
using ModelContextProtocol.Protocol;
namespace NyaForge.Mcp;
internal static class CaptureResult
{
    public static CallToolResult Convert(System.Text.Json.JsonElement value)
    {
        var summary=JsonNode.Parse(value.GetRawText())!.AsObject();
        // Keep the per-image record (including camera and capture conditions) in
        // structured/text output, but never duplicate the PNG base64 payload there.
        // Image bytes are delivered once as MCP ImageContentBlock values below.
        var imageRecords = new JsonArray();
        foreach (var image in summary["images"]?.AsArray() ?? throw new InvalidOperationException("Capture response is missing images."))
        {
            var record = image?.AsObject()?.DeepClone().AsObject();
            if (record == null) throw new InvalidOperationException("Capture response contains an invalid image record.");
            record.Remove("data");
            imageRecords.Add(record);
        }
        summary["images"] = imageRecords;
        var blocks=new List<ContentBlock> {new TextContentBlock {Text=summary.ToJsonString()}};
        foreach(var image in value.GetProperty("images").EnumerateArray())
            blocks.Add(ImageContentBlock.FromBytes(System.Convert.FromBase64String(image.GetProperty("data").GetString()!),"image/png"));
        return new CallToolResult {Content=blocks,StructuredContent=System.Text.Json.JsonSerializer.SerializeToElement(summary)};
    }
}
