using System.Text.Json;
using ModelContextProtocol.Protocol;
using NyaForge.Mcp;

internal static class CaptureResultVerification
{
    public static void Run()
    {
        using var document = JsonDocument.Parse("""
        {"success":true,"images":[{"index":0,"mimeType":"image/png","sha256":"abc","width":32,"height":32,"data":"AQID","camera":{"projection":"orthographic","size":1.5}}]}
        """);
        var result = CaptureResult.Convert(document.RootElement);
        var structured = result.StructuredContent!.Value;
        var image = structured.GetProperty("images")[0];
        if (image.TryGetProperty("data", out _)) throw new Exception("Capture structured result duplicated PNG data.");
        if (image.GetProperty("camera").GetProperty("projection").GetString() != "orthographic") throw new Exception("Capture structured result lost camera metadata.");
        var blocks = result.Content.OfType<ImageContentBlock>().ToArray();
        if (blocks.Length != 1 || !blocks[0].DecodedData.ToArray().SequenceEqual(new byte[] { 1, 2, 3 })) throw new Exception("Capture image block did not retain PNG bytes.");
        Console.WriteLine("PASS: MCP capture result preserves camera metadata without duplicating image bytes");
    }
}
