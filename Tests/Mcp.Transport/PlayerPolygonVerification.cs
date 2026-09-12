using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using NyaForge.Mcp;
using static PlayerApplyVerification;

internal static class PlayerPolygonVerification
{
    public static async Task RunAsync(McpClient client,CancellationToken token)
    {
        string node=Guid.NewGuid().ToString("D");
        var state=await Call(client,"forge_get_state",null,token);
        var add=new ApplyOperation {kind="graph.node.add",node=new NodeCommand {nodeId=node,typeId="mesh.polygon-source",version=1,parameters=new NodeParameters
        {
            domainId=Guid.NewGuid().ToString("D"),scale=1,translation=[0,0,0],
            vertices=[new(){id="11",position=[0,0,0]},new(){id="22",position=[.1,0,0]},new(){id="33",position=[0,.1,0]}],
            faces=[new(){id="9007199254740993",materialSlot=0,corners=[new(){id="101",vertexId="11"},new(){id="102",vertexId="22"},new(){id="103",vertexId="33"}]}]
        }}};
        var result=await Call(client,"forge_apply",Command(state,Guid.NewGuid().ToString("D"),add),token);
        if(!result.GetProperty("success").GetBoolean()) throw new Exception("Polygon generation failed: "+result);
        state=await Call(client,"forge_get_state",null,token);
        var graph=await Call(client,"forge_graph_inspect",null,token);
        var mesh=graph.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==node).GetProperty("meshOutput");
        var query=new MeshPageQuery {documentId=state.GetProperty("documentId").GetString()!,revision=state.GetProperty("revision").GetInt64(),nodeId=node,port="output",snapshotHash=mesh.GetProperty("snapshotHash").GetString()!,offset=0,count=1};
        var reply=await client.CallToolAsync("forge_faces_inspect",new Dictionary<string,object?> {{"faces",query}},cancellationToken:token);
        if(reply.IsError==true) throw new Exception("Polygon inspection failed");
        using var json=JsonDocument.Parse(string.Join("",reply.Content.OfType<TextContentBlock>().Select(c=>c.Text)));
        var face=json.RootElement.GetProperty("faces")[0];
        if(face.GetProperty("id").GetString()!="9007199254740993" || !face.GetProperty("corners").EnumerateArray().Select(c=>c.GetProperty("vertexId").GetString()).SequenceEqual(new[]{"11","22","33"})) throw new Exception("Polygon identity or winding changed");
        string edit=Guid.NewGuid().ToString("D");
        async Task Apply(ApplyOperation operation)
        {
            var current=await Call(client,"forge_get_state",null,token);
            var applied=await Call(client,"forge_apply",Command(current,Guid.NewGuid().ToString("D"),operation),token);
            if(!applied.GetProperty("success").GetBoolean()) throw new Exception("Polygon edit failed: "+applied);
        }
        await Apply(new ApplyOperation {kind="graph.node.add",node=new NodeCommand {nodeId=edit,typeId="mesh.polygon-edit",version=1,parameters=new NodeParameters()}});
        await Apply(new ApplyOperation {kind="graph.connect",fromNode=node,fromPort="mesh",toNode=edit,toPort="mesh"});
        graph=await Call(client,"forge_graph_inspect",null,token);
        var contextJson=graph.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==edit).GetProperty("editContext");
        var context=JsonSerializer.Deserialize<EditContextCommand>(contextJson.GetRawText())!;
        await Apply(new ApplyOperation {kind="polygon.faces.extrude",context=context,elementIds=[face.GetProperty("id").GetString()!],delta=[0,0,.05]});
        await Apply(new ApplyOperation {kind="polygon.uv.project",context=context});
        graph=await Call(client,"forge_graph_inspect",null,token);
        var changed=graph.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==edit).GetProperty("meshOutput");
        state=await Call(client,"forge_get_state",null,token);
        var afterQuery=new MeshPageQuery {documentId=state.GetProperty("documentId").GetString()!,revision=state.GetProperty("revision").GetInt64(),nodeId=edit,port="output",snapshotHash=changed.GetProperty("snapshotHash").GetString()!,offset=0,count=64};
        var afterReply=await client.CallToolAsync("forge_faces_inspect",new Dictionary<string,object?> {{"faces",afterQuery}},cancellationToken:token);
        if(afterReply.IsError==true) throw new Exception("Extruded face inspection failed");
        using var afterJson=JsonDocument.Parse(string.Join("",afterReply.Content.OfType<TextContentBlock>().Select(c=>c.Text)));
        if(afterJson.RootElement.GetProperty("total").GetInt32()<=1) throw new Exception("Extrusion did not create side faces");
        foreach(var projectedFace in afterJson.RootElement.GetProperty("faces").EnumerateArray())
            foreach(var corner in projectedFace.GetProperty("corners").EnumerateArray())
                if(corner.GetProperty("uv0").ValueKind!=JsonValueKind.Array || corner.GetProperty("uv0").GetArrayLength()!=2) throw new Exception("Projected UV missing from face inspection");
        await Apply(new ApplyOperation {kind="history.undo"});
        await Apply(new ApplyOperation {kind="history.redo"});
        await PlayerPaintVerification.RunAsync(client,edit,token);
        await Apply(new ApplyOperation {kind="graph.node.remove",nodeId=edit});
        state=await Call(client,"forge_get_state",null,token);
        result=await Call(client,"forge_apply",Command(state,Guid.NewGuid().ToString("D"),new ApplyOperation {kind="graph.node.remove",nodeId=node}),token);
        if(!result.GetProperty("success").GetBoolean()) throw new Exception("Polygon cleanup failed");
        Console.WriteLine("PASS: live MCP polygon generation, observed-face extrusion, UV projection/readback and Undo/Redo");
    }
}
