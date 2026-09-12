using System.Security.Cryptography;
using System.Text.Json;
using ModelContextProtocol.Client;
using NyaForge.Mcp;
using static PlayerApplyVerification;

internal static class PlayerImportVerification
{
    public static async Task RunAsync(McpClient client,string paint,CancellationToken token)
    {
        async Task<JsonElement> Node()
        {
            var graph=await Call(client,"forge_graph_inspect",null,token);
            return graph.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==paint);
        }
        async Task Apply(ApplyOperation operation)
        {
            var current=await Call(client,"forge_get_state",null,token);
            var result=await Call(client,"forge_apply",Command(current,Guid.NewGuid().ToString("D"),operation),token);
            if(!result.GetProperty("success").GetBoolean()) throw new Exception("Import fixture operation failed: "+result);
        }
        var state=await Call(client,"forge_get_state",null,token);var node=await Node();
        string path=Path.Combine(state.GetProperty("saveTarget").GetProperty("directory").GetString()!,"import-fixture.png");
        byte[] bytes=File.ReadAllBytes(path);string id=Guid.NewGuid().ToString("D"),before=node.GetProperty("imageOutput").GetProperty("imageHash").GetString()!;
        var operation=new ApplyOperation {kind="layers.import",layerContext=JsonSerializer.Deserialize<LayerContextCommand>(node.GetProperty("layerStack").GetProperty("context").GetRawText())!,layerId=id,name="imported blue",width=64,height=64,index=1,path=path,sourceHash=Convert.ToHexStringLower(SHA256.HashData(bytes)),fit=true};
        var command=Command(state,Guid.NewGuid().ToString("D"),operation);
        var first=await Call(client,"forge_import_image",command,token);var replay=await Call(client,"forge_import_image",command,token);
        if(!first.GetProperty("success").GetBoolean() || !replay.GetProperty("success").GetBoolean() || first.GetProperty("revision").GetInt64()!=replay.GetProperty("revision").GetInt64()) throw new Exception("Import/replay failed");
        var imported=await Node();string hash=imported.GetProperty("imageOutput").GetProperty("imageHash").GetString()!;
        if(hash==before || imported.GetProperty("layerStack").GetProperty("layers")[1].GetProperty("id").GetString()!=id) throw new Exception("Imported layer identity/image missing");
        var changed=(byte[])bytes.Clone();changed[^1]^=1;File.WriteAllBytes(path,changed);
        try
        {
            var rejected=await client.CallToolAsync("forge_import_image",new Dictionary<string,object?> {{"command",command}},cancellationToken:token);
            if(rejected.IsError!=true || (await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=hash) throw new Exception("Changed PNG source was accepted or changed the image");
        }
        finally { File.WriteAllBytes(path,bytes); }
        await Apply(new ApplyOperation {kind="history.undo"});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=before) throw new Exception("Import Undo differs");
        await Apply(new ApplyOperation {kind="history.redo"});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=hash) throw new Exception("Import Redo differs");
        node=await Node();
        await Apply(new ApplyOperation {kind="layers.remove",layerContext=JsonSerializer.Deserialize<LayerContextCommand>(node.GetProperty("layerStack").GetProperty("context").GetRawText())!,layerId=id});
        Console.WriteLine("PASS: live MCP PNG import, fitting, replay, source hash rejection and Undo/Redo");
    }
}
