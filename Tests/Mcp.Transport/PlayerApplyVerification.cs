using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NyaForge.Mcp;
internal static class PlayerApplyVerification
{
    internal static async Task<JsonElement> Call(McpClient client,string name,object? command,CancellationToken token)
    {
        var result=await client.CallToolAsync(name,command==null ? null : new Dictionary<string,object?> { ["command"]=command },cancellationToken:token);
        if(result.IsError==true) throw new Exception("MCP apply probe failed: "+string.Join("",result.Content.OfType<TextContentBlock>().Select(c=>c.Text)));
        using var json=JsonDocument.Parse(string.Join("",result.Content.OfType<TextContentBlock>().Select(c=>c.Text)));return json.RootElement.Clone();
    }
    internal static ApplyCommand Command(JsonElement state,string id,ApplyOperation operation)=>new()
    {
        expectedInstanceId=state.GetProperty("instanceId").GetString()!,documentId=state.GetProperty("documentId").GetString()!,
        expectedDocumentRevision=state.GetProperty("revision").GetInt64(),commandId=id,objectId=state.GetProperty("objects").GetArrayLength()==0 ? "" : state.GetProperty("objects")[0].GetProperty("objectId").GetString()!,
        expectedBaselineHash=state.GetProperty("editSourceHash").GetString()!,operations=[operation]
    };
    public static async Task RunAsync(McpClient client,CancellationToken token)
    {
        var before=await Call(client,"forge_get_state",null,token);
        var op=new ApplyOperation { kind="vertices.translate",vertexIds=[0,4],delta=[.01,0,0] };
        var command=Command(before,Guid.NewGuid().ToString("D"),op);
        var first=await Call(client,"forge_apply",command,token);
        if(!first.GetProperty("success").GetBoolean()) throw new Exception("Edit rejected: "+first);
        var repeat=await Call(client,"forge_apply",command,token);
        if(!repeat.GetProperty("success").GetBoolean() || repeat.GetProperty("revision").GetInt64()!=first.GetProperty("revision").GetInt64()) throw new Exception("Replay applied twice");
        var stale=await Call(client,"forge_apply",Command(before,Guid.NewGuid().ToString("D"),op),token);
        if(stale.GetProperty("code").GetString()!="REVISION_CONFLICT") throw new Exception("Stale revision not rejected");
        var edited=await Call(client,"forge_get_state",null,token);
        if(edited.GetProperty("stateHash").GetString()==before.GetProperty("stateHash").GetString()) throw new Exception("Edit had no effect");
        await PlayerCaptureVerification.RunAsync(client,edited.GetProperty("stateHash").GetString()!,token);
        var undo=await Call(client,"forge_apply",Command(edited,Guid.NewGuid().ToString("D"),new ApplyOperation {kind="history.undo"}),token);
        if(!undo.GetProperty("success").GetBoolean()) throw new Exception("Undo rejected");
        var restored=await Call(client,"forge_get_state",null,token);
        if(restored.GetProperty("stateHash").GetString()!=before.GetProperty("stateHash").GetString()) throw new Exception("Undo did not restore original state");
    }
}
