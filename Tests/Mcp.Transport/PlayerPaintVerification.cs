using System.Text.Json;
using ModelContextProtocol.Client;
using NyaForge.Mcp;
using static PlayerApplyVerification;

internal static class PlayerPaintVerification
{
    public static async Task RunAsync(McpClient client,string meshNode,CancellationToken token)
    {
        string paint=Guid.NewGuid().ToString("D");
        async Task Apply(ApplyOperation op)
        {
            var state=await Call(client,"forge_get_state",null,token);
            var r=await Call(client,"forge_apply",Command(state,Guid.NewGuid().ToString("D"),op),token);
            if(!r.GetProperty("success").GetBoolean()) throw new Exception("Paint setup failed: "+r);
        }
        await Apply(new ApplyOperation {kind="graph.node.add",node=new NodeCommand {nodeId=paint,typeId="image.paint",version=1,parameters=new NodeParameters {width=64,height=64}}});
        await Apply(new ApplyOperation {kind="graph.connect",fromNode=meshNode,fromPort="mesh",toNode=paint,toPort="mesh"});
        async Task<JsonElement> Node()
        {
            var graph=await Call(client,"forge_graph_inspect",null,token);
            return graph.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==paint);
        }
        var before=await Node();var context=JsonSerializer.Deserialize<PaintContextCommand>(before.GetProperty("paintContext").GetRawText())!;
        var state=await Call(client,"forge_get_state",null,token);
        var op=new ApplyOperation {kind="paint.stroke",paintContext=context,points=[[.2,.5],[.8,.5]],radius=4,color=[255,0,0,255]};
        var command=Command(state,Guid.NewGuid().ToString("D"),op);
        var first=await Call(client,"forge_apply",command,token);var again=await Call(client,"forge_apply",command,token);
        if(!first.GetProperty("success").GetBoolean() || !again.GetProperty("success").GetBoolean() || first.GetProperty("revision").GetInt64()!=again.GetProperty("revision").GetInt64()) throw new Exception("Paint replay failed");
        var after=await Node();string hash=after.GetProperty("imageOutput").GetProperty("imageHash").GetString()!;
        if(hash==context.imageHash) throw new Exception("Paint image did not change");
        state=await Call(client,"forge_get_state",null,token);
        var stale=await Call(client,"forge_apply",Command(state,Guid.NewGuid().ToString("D"),op),token);
        if(stale.GetProperty("code").GetString()!="PAINT_CONTEXT_STALE") throw new Exception("Old paint context accepted");
        await Apply(new ApplyOperation {kind="history.undo"});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=context.imageHash) throw new Exception("Paint Undo mismatch");
        await Apply(new ApplyOperation {kind="history.redo"});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=hash) throw new Exception("Paint Redo mismatch");
        await PlayerLayerVerification.RunAsync(client,paint,token);
        await PlayerPaintOutputVerification.RunAsync(client,meshNode,paint,token);
        await Apply(new ApplyOperation {kind="graph.node.remove",nodeId=paint});
        Console.WriteLine("PASS: live MCP paint creation, stroke, replay, stale image rejection and Undo/Redo");
    }
}
