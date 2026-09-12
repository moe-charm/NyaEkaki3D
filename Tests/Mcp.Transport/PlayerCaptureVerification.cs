using System.Buffers.Binary;
using System.Security.Cryptography;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
internal static class PlayerCaptureVerification
{
    public static async Task RunAsync(McpClient client,string expectedHash,CancellationToken token)
    {
        var reply=await client.CallToolAsync("forge_capture",cancellationToken:token);
        if(reply.IsError==true || reply.StructuredContent==null) throw new Exception("Capture failed or lost structured content");
        var summary=reply.StructuredContent.Value;
        if(summary.GetProperty("metadata").GetProperty("stateHash").GetString()!=expectedHash) throw new Exception("Capture targeted a different state");
        var record=summary.GetProperty("capture");var images=reply.Content.OfType<ImageContentBlock>().ToArray();
        if(images.Length!=5 || record.GetProperty("images").GetArrayLength()!=5) throw new Exception("Five MCP image blocks missing");
        if(record.GetProperty("snapshotId").GetString()!=summary.GetProperty("metadata").GetProperty("snapshotId").GetString()) throw new Exception("Capture/metadata snapshots differ");
        for(int i=0;i<images.Length;i++)
        {
            var png=images[i].DecodedData.ToArray();
            if(images[i].MimeType!="image/png" || png.Length<24 || !png.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10})) throw new Exception("Invalid image block");
            if(BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(16,4))!=256 || BinaryPrimitives.ReadInt32BigEndian(png.AsSpan(20,4))!=256) throw new Exception("Unexpected capture dimensions");
            string hash=Convert.ToHexString(SHA256.HashData(png)).ToLowerInvariant();
            if(hash!=record.GetProperty("images")[i].GetProperty("sha256").GetString()) throw new Exception("MCP image bytes differ from capture record");
        }
        Console.WriteLine("PASS: five live Player PNG image blocks with matching snapshot, hashes and dimensions over MCP");
    }
}
