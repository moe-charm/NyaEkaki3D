using System.Text.Json;
using ModelContextProtocol.Client;
using NyaForge.Mcp;
using static PlayerApplyVerification;

internal static class PlayerLayerVerification
{
    public static async Task RunAsync(McpClient client,string paint,CancellationToken token)
    {
        async Task<JsonElement> Node()
        {
            var graph=await Call(client,"forge_graph_inspect",null,token);
            return graph.GetProperty("graph").GetProperty("nodes").EnumerateArray().Single(n=>n.GetProperty("nodeId").GetString()==paint);
        }
        async Task Apply(ApplyOperation op)
        {
            var state=await Call(client,"forge_get_state",null,token);
            var reply=await Call(client,"forge_apply",Command(state,Guid.NewGuid().ToString("D"),op),token);
            if(!reply.GetProperty("success").GetBoolean()) throw new Exception("Layer operation failed: "+reply);
        }
        async Task<LayerContextCommand> Context()=>JsonSerializer.Deserialize<LayerContextCommand>((await Node()).GetProperty("layerStack").GetProperty("context").GetRawText())!;
        string background=Guid.NewGuid().ToString("D"),overlay=Guid.NewGuid().ToString("D");
        var before=await Node();string hash=before.GetProperty("imageOutput").GetProperty("imageHash").GetString()!;
        await Apply(new ApplyOperation {kind="layers.migrate",paintContext=JsonSerializer.Deserialize<PaintContextCommand>(before.GetProperty("paintContext").GetRawText())!,layerId=background});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=hash) throw new Exception("Layer migration changed image");
        await Apply(new ApplyOperation {kind="layers.add",layerContext=await Context(),layerId=overlay,name="overlay",width=64,height=64,index=1});
        await Apply(new ApplyOperation {kind="layers.stroke",layerContext=await Context(),layerId=overlay,points=[[.5,.5]],radius=4,color=[0,255,0,255]});
        string drawn=(await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!;
        if(drawn==hash) throw new Exception("Layer stroke did not alter composite");
        await Apply(new ApplyOperation {kind="layers.mask.fill",layerContext=await Context(),layerId=overlay,width=64,height=64,target=0});
        var masked=await Node();
        if(masked.GetProperty("imageOutput").GetProperty("imageHash").GetString()!=hash || !masked.GetProperty("layerStack").GetProperty("layers")[1].GetProperty("hasMask").GetBoolean()) throw new Exception("Mask did not hide layer");
        await Apply(new ApplyOperation {kind="layers.mask.stroke",layerContext=await Context(),layerId=overlay,points=[[.5,.5]],radius=2,target=255,strength=1});
        string revealed=(await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!;
        if(revealed==hash || revealed==drawn) throw new Exception("Mask stroke did not partially reveal layer");
        await Apply(new ApplyOperation {kind="layers.mask.clear",layerContext=await Context(),layerId=overlay});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=drawn) throw new Exception("Mask removal did not restore layer");
        await Apply(new ApplyOperation {kind="history.undo"});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=revealed) throw new Exception("Mask Undo mismatch");
        await Apply(new ApplyOperation {kind="history.redo"});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()!=drawn) throw new Exception("Mask Redo mismatch");
        await Apply(new ApplyOperation {kind="layers.appearance",layerContext=await Context(),layerId=background,opacity=.5,visible=false});
        if((await Node()).GetProperty("imageOutput").GetProperty("imageHash").GetString()==hash) throw new Exception("Hidden background did not change composite");
        await Apply(new ApplyOperation {kind="layers.appearance",layerContext=await Context(),layerId=background,opacity=1,visible=true});
        await Apply(new ApplyOperation {kind="layers.rename",layerContext=await Context(),layerId=overlay,name="linework"});
        await Apply(new ApplyOperation {kind="layers.move",layerContext=await Context(),layerId=overlay,index=0});
        var moved=await Node();var first=moved.GetProperty("layerStack").GetProperty("layers")[0];
        if(first.GetProperty("id").GetString()!=overlay || first.GetProperty("name").GetString()!="linework") throw new Exception("Layer order/name mismatch");
        await Apply(new ApplyOperation {kind="layers.remove",layerContext=await Context(),layerId=overlay});
        await Apply(new ApplyOperation {kind="history.undo"});
        if((await Node()).GetProperty("layerStack").GetProperty("layers").GetArrayLength()!=2) throw new Exception("Layer Undo mismatch");
        await Apply(new ApplyOperation {kind="history.redo"});
        var final=await Node();
        if(final.GetProperty("layerStack").GetProperty("layers").GetArrayLength()!=1 || final.GetProperty("imageOutput").GetProperty("imageHash").GetString()!=hash) throw new Exception("Layer Redo or composite mismatch");
        await PlayerImportVerification.RunAsync(client,paint,token);
        Console.WriteLine("PASS: live MCP layer management, layer stroke, mask fill/stroke/removal and Undo/Redo preserve painted image");
    }
}
