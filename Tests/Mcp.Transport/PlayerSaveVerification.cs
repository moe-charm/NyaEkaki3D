using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NyaForge.Mcp;
internal static class PlayerSaveVerification
{
    public static async Task RunAsync(McpClient client,CancellationToken token)
    {
        var state=await PlayerApplyVerification.Call(client,"forge_get_state",null,token);
        var save=new SaveProjectCommand {documentId=state.GetProperty("documentId").GetString()!,expectedRevision=state.GetProperty("revision").GetInt64(),directory=state.GetProperty("saveTarget").GetProperty("directory").GetString()!,expectedSaveVersion=state.GetProperty("saveTarget").GetProperty("expectedSaveVersion").GetInt64()};
        for(int i=0;i<2;i++)
        {
            var response=await client.CallToolAsync("forge_save_project",new Dictionary<string,object?> { ["save"]=save },cancellationToken:token);
            if(response.IsError==true) throw new Exception("Save transport failed");
            using var json=JsonDocument.Parse(string.Join("",response.Content.OfType<TextContentBlock>().Select(c=>c.Text)));var result=json.RootElement;
            if(i==0 && (!result.GetProperty("success").GetBoolean() || result.GetProperty("stateHash").GetString()!=state.GetProperty("stateHash").GetString())) throw new Exception("Native save failed: "+result);
            if(i==1 && result.GetProperty("code").GetString()!="SAVE_CONFLICT") throw new Exception("Repeated save overwrote without version check");
        }
        var after=await PlayerApplyVerification.Call(client,"forge_get_state",null,token);
        if(after.GetProperty("dirty").GetBoolean() || after.GetProperty("revision").GetInt64()!=save.expectedRevision || after.GetProperty("saveVersion").GetInt64()!=save.expectedSaveVersion+1) throw new Exception("Save changed revision or left dirty state");
        Console.WriteLine("PASS: native MCP save, version-conflict replay and clean unchanged document revision");
    }
}
