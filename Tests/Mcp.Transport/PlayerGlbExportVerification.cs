using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NyaForge.Mcp;

internal static class PlayerGlbExportVerification
{
    static async Task<JsonElement> CallExport(McpClient client, ExportGlbCommand export, CancellationToken token)
    {
        var result = await client.CallToolAsync("forge_export_glb", new Dictionary<string, object?> { ["export"] = export }, cancellationToken: token);
        if (result.IsError == true) throw new Exception("MCP GLB export probe failed: " + string.Join("", result.Content.OfType<TextContentBlock>().Select(c => c.Text)));
        using var json = JsonDocument.Parse(string.Join("", result.Content.OfType<TextContentBlock>().Select(c => c.Text)));
        return json.RootElement.Clone();
    }

    public static async Task RunAsync(string[] args)
    {
        if(args.Length!=5) throw new ArgumentException("Expected --player-glb-export instance document revision stateHash");
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(35)); var token=deadline.Token;
        await using var client=await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        { Name="NyaForge Player GLB export verification",Command="dotnet",Arguments=[Path.Combine(AppContext.BaseDirectory,"NyaForge.Mcp.dll"),"--instance",args[1]],StandardErrorLines=line=>Console.Error.WriteLine(line) }),cancellationToken:token);
        var state=await PlayerApplyVerification.Call(client,"forge_get_state",null,token);
        var export=new ExportGlbCommand { documentId=state.GetProperty("documentId").GetString()!, expectedRevision=state.GetProperty("revision").GetInt64(), directory=state.GetProperty("saveTarget").GetProperty("directory").GetString()!, exportId=Guid.NewGuid().ToString("D"), profile="static" };
        var first=await CallExport(client,export,token);
        if(!first.GetProperty("success").GetBoolean() || first.GetProperty("profile").GetString()!="StaticGeometry") throw new Exception("MCP GLB export failed: "+first);
        string path=first.GetProperty("glbPath").GetString()!; if(!File.Exists(path) || BitConverter.ToUInt32(File.ReadAllBytes(path),0)!=0x46546c67) throw new Exception("MCP GLB output is missing or invalid");
        var repeat=await CallExport(client,export,token);
        if(repeat.GetProperty("success").GetBoolean() || repeat.GetProperty("code").GetString()!="EXPORT_DESTINATION_EXISTS") throw new Exception("MCP GLB export replay overwrote destination");
        var after=await PlayerApplyVerification.Call(client,"forge_get_state",null,token);
        if(after.GetProperty("stateHash").GetString()!=state.GetProperty("stateHash").GetString() || after.GetProperty("revision").GetInt64()!=state.GetProperty("revision").GetInt64()) throw new Exception("MCP GLB export changed document state");
        Console.WriteLine("PASS: MCP standard GLB export preserves state and rejects destination replay");
    }
}
