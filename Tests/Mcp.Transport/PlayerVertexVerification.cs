using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NyaForge.Mcp;

internal static class PlayerVertexVerification
{
    public static async Task<int> RunAsync(McpClient client,JsonElement state,EditContextCommand context,CancellationToken token)
    {
        var ids=new List<int>();
        for(int offset=0;offset<4;offset+=2)
        {
            var query=new MeshPageQuery {documentId=state.GetProperty("documentId").GetString()!,revision=state.GetProperty("revision").GetInt64(),nodeId=context.nodeId,port="input",snapshotHash=context.inputSnapshot,offset=offset,count=2};
            var reply=await client.CallToolAsync("forge_vertices_inspect",new Dictionary<string,object?> {{"vertices",query}},cancellationToken:token);
            if(reply.IsError==true) throw new Exception("Vertex inspection failed");
            using var json=JsonDocument.Parse(string.Join("",reply.Content.OfType<TextContentBlock>().Select(c=>c.Text)));
            var page=json.RootElement;
            if(page.GetProperty("total").GetInt32()!=4 || page.GetProperty("snapshotHash").GetString()!=context.inputSnapshot || page.GetProperty("idKind").GetString()!="meshVertexIndex") throw new Exception("Vertex page identity differs");
            foreach(var v in page.GetProperty("vertices").EnumerateArray())
            {
                ids.Add(v.GetProperty("id").GetInt32());
                var rest=v.GetProperty("restPosition");var avatar=v.GetProperty("avatarPosition");
                for(int axis=0;axis<3;axis++) if(rest[axis].GetDouble()!=avatar[axis].GetDouble()) throw new Exception("Identity transform differs");
            }
            if(offset==0 ? page.GetProperty("nextOffset").GetInt32()!=2 : page.GetProperty("nextOffset").ValueKind!=JsonValueKind.Null) throw new Exception("Vertex pagination differs");
        }
        if(!ids.SequenceEqual(new[]{0,1,2,3})) throw new Exception("Vertex pages skipped or duplicated IDs");
        var faceQuery=new MeshPageQuery {documentId=state.GetProperty("documentId").GetString()!,revision=state.GetProperty("revision").GetInt64(),nodeId=context.nodeId,port="input",snapshotHash=context.inputSnapshot,offset=0,count=1};
        var rejected=await client.CallToolAsync("forge_faces_inspect",new Dictionary<string,object?> {{"faces",faceQuery}},cancellationToken:token);
        if(rejected.IsError!=true) throw new Exception("Triangle mesh was given polygon face identities");
        // A rejected query must not stop the instance listener.
        var recovered=await PlayerApplyVerification.Call(client,"forge_get_state",null,token);
        if(recovered.GetProperty("stateHash").GetString()!=state.GetProperty("stateHash").GetString()) throw new Exception("Rejected inspection changed state");
        Console.WriteLine("PASS: live MCP vertex pages preserve identity, positions and complete index coverage");
        return ids[0];
    }
}

