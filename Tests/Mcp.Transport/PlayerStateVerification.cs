using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

internal static class PlayerStateVerification
{
    public static async Task RunAsync(string[] args)
    {
        if(args.Length!=5) throw new ArgumentException("Expected --player-state instance document revision stateHash");
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(25));
        var token=deadline.Token;
        await using var client=await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Name="NyaForge Player verification",Command="dotnet",Arguments=[Path.Combine(AppContext.BaseDirectory,"NyaForge.Mcp.dll"),"--instance",args[1]],StandardErrorLines=line=>Console.Error.WriteLine(line)
        }),cancellationToken:token);
        var tools=await client.ListToolsAsync(cancellationToken:token);
        if(!tools.Any(t=>t.Name=="forge_get_state")) throw new Exception("State tool absent");
        if(args[0]=="--player-create") { await PlayerCreateVerification.RunAsync(client,token);await PlayerSaveVerification.RunAsync(client,token);return; }
        var capabilityReply=await client.CallToolAsync("forge_capabilities",cancellationToken:token);
        if(capabilityReply.IsError==true) throw new Exception("Capabilities failed");
        using(var capabilityJson=JsonDocument.Parse(string.Join("",capabilityReply.Content.OfType<TextContentBlock>().Select(t=>t.Text))))
        {
            var capabilities=capabilityJson.RootElement;
            if(capabilities.GetProperty("instanceId").GetString()!=args[1] || !capabilities.GetProperty("remoteEditing").GetBoolean() || capabilities.GetProperty("nodeDefinitions").GetArrayLength()==0 || !capabilities.GetProperty("remoteMethods").EnumerateArray().Any(value=>value.GetString()=="surface_fit_inspect")) throw new Exception("Invalid live capabilities");
        }
        // The plain fixture has no avatar target, so the read-only fit endpoint
        // must fail with a structured MCP error instead of mutating the graph.
        var fitReply=await client.CallToolAsync("forge_surface_fit_inspect",cancellationToken:token);
        if(fitReply.IsError!=true) throw new Exception("Surface fit inspection unexpectedly succeeded without an avatar target");
        // Repeated calls exercise disconnect/reconnect on the same live Player listener.
        var graphReply=await client.CallToolAsync("forge_graph_inspect",cancellationToken:token);
        if(graphReply.IsError==true) throw new Exception("Graph inspection failed");
        using(var graphJson=JsonDocument.Parse(string.Join("",graphReply.Content.OfType<TextContentBlock>().Select(t=>t.Text))))
        {
            var inspection=graphJson.RootElement;var graph=inspection.GetProperty("graph");
            if(inspection.GetProperty("documentId").GetString()!=args[2] || inspection.GetProperty("stateHash").GetString()!=args[4] || graph.GetProperty("nodes").GetArrayLength()<2 || graph.GetProperty("edges").GetArrayLength()<1 || !graph.GetProperty("evaluationComplete").GetBoolean()) throw new Exception("Live graph differs from fixture");
        }
        var validationReply=await client.CallToolAsync("forge_validate",new Dictionary<string,object?>
        {
            ["validation"]=new { documentId=args[2], expectedRevision=long.Parse(args[3]), profile="pc" }
        },cancellationToken:token);
        if(validationReply.IsError==true) throw new Exception("Validation failed: "+string.Join("",validationReply.Content.OfType<TextContentBlock>().Select(t=>t.Text)));
        using(var validationJson=JsonDocument.Parse(string.Join("",validationReply.Content.OfType<TextContentBlock>().Select(t=>t.Text))))
        {
            var validation=validationJson.RootElement;
            if(validation.GetProperty("status").GetString()!="pass" || validation.GetProperty("metrics").GetProperty("triangles").GetInt32()<=0) throw new Exception("Live validation did not pass");
        }
        for(int i=0;i<3;i++)
        {
            var reply=await client.CallToolAsync("forge_get_state",cancellationToken:token);
            if(reply.IsError==true) throw new Exception("Player state call "+i+" failed: "+string.Join("",reply.Content.OfType<TextContentBlock>().Select(t=>t.Text)));
            using var json=JsonDocument.Parse(string.Join("",reply.Content.OfType<TextContentBlock>().Select(t=>t.Text)));
            var state=json.RootElement;
            if(state.GetProperty("instanceId").GetString()!=args[1] || state.GetProperty("documentId").GetString()!=args[2] || state.GetProperty("revision").GetInt64().ToString()!=args[3] || state.GetProperty("stateHash").GetString()!=args[4])
                throw new Exception("MCP state differs from live Player state");
        }
        await PlayerObjectLabelVerification.RunAsync(client,token);
        await PlayerApplyVerification.RunAsync(client,token);
        await PlayerExportVerification.RunAsync(client,token);
        await PlayerSaveVerification.RunAsync(client,token);
        Console.WriteLine("PASS: external MCP client -> sidecar -> live Player, state reads and apply/replay/conflict/undo");
    }
}
