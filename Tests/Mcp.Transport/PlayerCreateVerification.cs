using System.Text.Json;
using ModelContextProtocol.Client;
using NyaForge.Mcp;
using static PlayerApplyVerification;
internal static class PlayerCreateVerification
{
    static string Id()=>Guid.NewGuid().ToString("D");
    static void Success(JsonElement r) { if(!r.GetProperty("success").GetBoolean()) throw new Exception("Creation command failed: "+r); }
    public static async Task RunAsync(McpClient client,CancellationToken token)
    {
        var empty=await Call(client,"forge_get_state",null,token);if(empty.GetProperty("objects").GetArrayLength()!=0) throw new Exception("Expected empty Player");
        string obj=Id(),graph=Id(),plane=Id(),edit=Id(),output=Id(),material=Id(),assignment=Id();
        var create=Command(empty,Id(),new ApplyOperation
        {
            kind="object.add_graph",newObjectId=obj,graph=new GraphCommand
            {
                graphId=graph,outputNodeId=output,nodes=[
                    new NodeCommand {nodeId=plane,typeId="primitive.plane",version=1,parameters=new NodeParameters {width=.2,height=.1}},
                    new NodeCommand {nodeId=edit,typeId="mesh.edit",version=1,parameters=new NodeParameters()},
                    new NodeCommand {nodeId=material,typeId="material.standard",version=1,parameters=new NodeParameters {baseColor=[.8,.15,.3,1],metallic=.25,roughness=.6,emission=[0,0,0],alphaMode="Opaque",alphaCutoff=.5}},
                    new NodeCommand {nodeId=assignment,typeId="mesh.assign-material",version=1,parameters=new NodeParameters()},
                    new NodeCommand {nodeId=output,typeId="mesh.output",version=1,parameters=new NodeParameters()}],
                edges=[new EdgeCommand {fromNode=plane,fromPort="mesh",toNode=edit,toPort="mesh"},new EdgeCommand {fromNode=edit,fromPort="mesh",toNode=assignment,toPort="mesh"},new EdgeCommand {fromNode=material,fromPort="material",toNode=assignment,toPort="material"},new EdgeCommand {fromNode=assignment,fromPort="mesh",toNode=output,toPort="mesh"}]
            }
        });
        Success(await Call(client,"forge_apply",create,token));Success(await Call(client,"forge_apply",create,token));
        var state=await Call(client,"forge_get_state",null,token);
        if(state.GetProperty("revision").GetInt64()!=1 || state.GetProperty("objects")[0].GetProperty("objectId").GetString()!=obj) throw new Exception("Creation/replay identity differs");
        string hash=state.GetProperty("stateHash").GetString()!;
        Success(await Call(client,"forge_apply",Command(state,Id(),new ApplyOperation {kind="history.undo"}),token));
        state=await Call(client,"forge_get_state",null,token);if(state.GetProperty("objects").GetArrayLength()!=0) throw new Exception("Undo did not empty project");
        Success(await Call(client,"forge_apply",Command(state,Id(),new ApplyOperation {kind="history.redo"}),token));
        state=await Call(client,"forge_get_state",null,token);if(state.GetProperty("stateHash").GetString()!=hash) throw new Exception("Redo changed graph");
        var inspected=await Call(client,"forge_graph_inspect",null,token);
        var materialNode=inspected.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==material);
        var materialInfo=materialNode.GetProperty("materialOutput");
        if(materialInfo.GetProperty("baseColorLinear")[0].GetDouble()!=.8 || materialInfo.GetProperty("baseColorLinear")[1].GetDouble()!=.15 || materialInfo.GetProperty("metallic").GetDouble()!=.25 || materialInfo.GetProperty("roughness").GetDouble()!=.6 || materialInfo.GetProperty("alphaMode").GetString()!="Opaque") throw new Exception("Material inspection lost explicit parameters");
        var assignedNode=inspected.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==assignment);
        if(assignedNode.GetProperty("assignedMaterial").GetProperty("contentHash").GetString()!=materialInfo.GetProperty("contentHash").GetString()) throw new Exception("Assigned material identity differs");
        var contextJson=inspected.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==edit).GetProperty("editContext");
        var context=JsonSerializer.Deserialize<EditContextCommand>(contextJson.GetRawText())!;
        int vertex=await PlayerVertexVerification.RunAsync(client,state,context,token);
        var move=new ApplyOperation {kind="graph.vertices.translate",context=context,vertexIds=[vertex],delta=[.01,0,0]};
        Success(await Call(client,"forge_apply",Command(state,Id(),move),token));
        state=await Call(client,"forge_get_state",null,token);if(state.GetProperty("stateHash").GetString()==hash) throw new Exception("Graph vertex edit had no effect");
        await PlayerCaptureVerification.RunAsync(client,state.GetProperty("stateHash").GetString()!,token);
        await PlayerExportVerification.RunAsync(client,token);
        await PlayerPolygonVerification.RunAsync(client,token);
        state=await Call(client,"forge_get_state",null,token);
        Success(await Call(client,"forge_apply",Command(state,Id(),new ApplyOperation {kind="graph.node.update",node=new NodeCommand {nodeId=plane,typeId="primitive.plane",version=1,parameters=new NodeParameters {width=.3,height=.1}}}),token));
        state=await Call(client,"forge_get_state",null,token);if(state.GetProperty("stateHash").GetString()==hash) throw new Exception("Node update had no effect");
        var stale=await Call(client,"forge_apply",Command(state,Id(),move),token);
        if(stale.GetProperty("code").GetString()!="EDIT_CONTEXT_STALE") throw new Exception("Old upstream context was accepted: "+stale);
        Console.WriteLine("PASS: empty Player -> graph creation, replay, Undo/Redo, context vertex edit and stale upstream rejection");
    }
}
