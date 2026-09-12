using ModelContextProtocol.Client;
using NyaForge.Mcp;
using static PlayerApplyVerification;

internal static class PlayerPaintOutputVerification
{
    public static async Task RunAsync(McpClient client,string mesh,string paint,CancellationToken token)
    {
        var graph=await Call(client,"forge_graph_inspect",null,token);
        string original=graph.GetProperty("graph").GetProperty("outputNodeId").GetString()!;
        string output=Guid.NewGuid().ToString("D");
        async Task Apply(ApplyOperation operation)
        {
            var state=await Call(client,"forge_get_state",null,token);
            var reply=await Call(client,"forge_apply",Command(state,Guid.NewGuid().ToString("D"),operation),token);
            if(!reply.GetProperty("success").GetBoolean()) throw new Exception("Paint output setup failed: "+reply);
        }
        await Apply(new ApplyOperation {kind="graph.node.add",node=new NodeCommand {nodeId=output,typeId="mesh.output",version=1,parameters=new NodeParameters()}});
        await Apply(new ApplyOperation {kind="graph.connect",fromNode=mesh,fromPort="mesh",toNode=output,toPort="mesh"});
        await Apply(new ApplyOperation {kind="graph.connect",fromNode=paint,fromPort="image",toNode=output,toPort="baseColor"});
        await Apply(new ApplyOperation {kind="graph.output",nodeId=output});
        var painted=await Call(client,"forge_get_state",null,token);
        if(!painted.GetProperty("evaluation").GetProperty("complete").GetBoolean()) throw new Exception("Painted output is incomplete");
        await PlayerCaptureVerification.RunAsync(client,painted.GetProperty("stateHash").GetString()!,token);
        await PlayerExportVerification.RunAsync(client,token);
        await PlayerSaveVerification.RunAsync(client,token);
        await Apply(new ApplyOperation {kind="graph.output",nodeId=original});
        await Apply(new ApplyOperation {kind="graph.node.remove",nodeId=output});
        Console.WriteLine("PASS: MCP painted final output -> five captures, Surface Bake and native save");
    }
}
