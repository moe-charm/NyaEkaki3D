using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NyaForge.Mcp;
internal static class PlayerExportVerification
{
    public static async Task RunAsync(McpClient client,CancellationToken token)
    {
        var state=await PlayerApplyVerification.Call(client,"forge_get_state",null,token);
        var export=new ExportProjectCommand {documentId=state.GetProperty("documentId").GetString()!,expectedRevision=state.GetProperty("revision").GetInt64(),directory=state.GetProperty("saveTarget").GetProperty("directory").GetString()!,exportId=Guid.NewGuid().ToString("D")};
        byte[]? original=null;string? manifest=null;
        for(int i=0;i<2;i++)
        {
            var reply=await client.CallToolAsync("forge_export",new Dictionary<string,object?> { ["export"]=export },cancellationToken:token);
            if(reply.IsError==true) throw new Exception("Export tool failed");
            using var json=JsonDocument.Parse(string.Join("",reply.Content.OfType<TextContentBlock>().Select(c=>c.Text)));var result=json.RootElement;
            if(i==0)
            {
                if(!result.GetProperty("success").GetBoolean() || result.GetProperty("stateHash").GetString()!=state.GetProperty("stateHash").GetString()) throw new Exception("Export mismatch: "+result);
                manifest=result.GetProperty("manifestPath").GetString()!;original=File.ReadAllBytes(manifest);
                if(Path.GetFullPath(Path.GetDirectoryName(manifest)!)!=Path.GetFullPath(Path.Combine(export.directory,"exports",export.exportId))) throw new Exception("Wrong export destination");
            }
            else if(result.GetProperty("code").GetString()!="EXPORT_DESTINATION_EXISTS" || !original!.SequenceEqual(File.ReadAllBytes(manifest!))) throw new Exception("Export replay overwrote output");
        }
        var after=await PlayerApplyVerification.Call(client,"forge_get_state",null,token);
        if(after.GetProperty("stateHash").GetString()!=state.GetProperty("stateHash").GetString() || after.GetProperty("dirty").GetBoolean()!=state.GetProperty("dirty").GetBoolean()) throw new Exception("Export changed document");
        Console.WriteLine("PASS: MCP export writes bounded destination, rejects replay and preserves document state");
    }
}
