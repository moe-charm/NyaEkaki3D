using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

internal static class McpProtocolVerification
{
    public static async Task RunAsync()
    {
        using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var token=deadline.Token;var instance=Guid.NewGuid();
        var executable=Path.Combine(AppContext.BaseDirectory,"NyaForge.Mcp.dll");
        await using var client=await McpClient.CreateAsync(new StdioClientTransport(new StdioClientTransportOptions
        {
            Name="NyaForge protocol verification",Command="dotnet",Arguments=[executable,"--instance",instance.ToString("D")]
        }),cancellationToken:token);
        var tools=await client.ListToolsAsync(cancellationToken:token);
        if(tools.Count!=11 || !tools.Any(t=>t.Name=="forge_apply") || !tools.Any(t=>t.Name=="forge_get_state") || !tools.Any(t=>t.Name=="forge_capabilities") || !tools.Any(t=>t.Name=="forge_graph_inspect") || !tools.Any(t=>t.Name=="forge_validate")) throw new Exception("Unexpected MCP tool registry");
        foreach(bool accepted in new[]{true,false})
        {
            using var pipe=new NamedPipeServerStream("NyaForge.Authoring."+instance.ToString("D"),PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
            var call=client.CallToolAsync("forge_get_state",cancellationToken:token);
            await pipe.WaitForConnectionAsync(token);
            using var reader=new StreamReader(pipe,Encoding.UTF8,false,1024,true);
            using var request=JsonDocument.Parse((await reader.ReadLineAsync(token))!);
            var r=request.RootElement;
            if(r.GetProperty("method").GetString()!="get_state" || r.GetProperty("expectedInstanceId").GetString()!=instance.ToString("D")) throw new Exception("MCP forwarded wrong request");
            var response=JsonSerializer.Serialize(new {version=1,requestId=r.GetProperty("requestId").GetString(),instanceId=instance.ToString("D"),ok=accepted,result=new { revision=23 },error="TEST_REJECTED"})+"\n";
            await pipe.WriteAsync(Encoding.UTF8.GetBytes(response),token);await pipe.FlushAsync(token);
            var result=await call;
            if(accepted)
            {
                if(result.IsError==true) throw new Exception("State tool failed");
                var text=string.Join("",result.Content.OfType<TextContentBlock>().Select(c=>c.Text));
                using var state=JsonDocument.Parse(text);
                if(state.RootElement.GetProperty("revision").GetInt32()!=23) throw new Exception("State result lost in MCP serialization");
            }
            else if(result.IsError!=true) throw new Exception("IPC failure reported as MCP success");
        }
        Console.WriteLine("PASS: real MCP stdio startup, tool discovery, state call and IPC error propagation");
    }
}



