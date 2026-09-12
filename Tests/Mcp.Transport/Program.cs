using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using NyaForge.Mcp;

if(args.Length>0 && (args[0]=="--player-state" || args[0]=="--player-create")) { await PlayerStateVerification.RunAsync(args);return; }
if(args.Length>0 && args[0]=="--player-glb-export") { await PlayerGlbExportVerification.RunAsync(args);return; }
if(args.Length>0 && args[0]=="--player-secondary")
{
    if(args.Length!=5) throw new ArgumentException("Expected --player-secondary instance document revision stateHash");
    using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(35));
    await using var client=await ModelContextProtocol.Client.McpClient.CreateAsync(new ModelContextProtocol.Client.StdioClientTransport(new ModelContextProtocol.Client.StdioClientTransportOptions
    {
        Name="NyaForge Player secondary-motion verification",Command="dotnet",Arguments=[Path.Combine(AppContext.BaseDirectory,"NyaForge.Mcp.dll"),"--instance",args[1]],StandardErrorLines=line=>Console.Error.WriteLine(line)
    }),cancellationToken:deadline.Token);
    await PlayerSecondaryMotionVerification.RunAsync(client,args,deadline.Token);
    return;
}

foreach(bool wrongIdentity in new[] {false,true})
{
    var instance=Guid.NewGuid();using var deadline=new CancellationTokenSource(TimeSpan.FromSeconds(10));
    using var server=new NamedPipeServerStream("NyaForge.Authoring."+instance.ToString("D"),PipeDirection.InOut,1,PipeTransmissionMode.Byte,PipeOptions.Asynchronous);
    var client=new InstanceConnection(instance).RequestAsync("get_state",deadline.Token);
    await server.WaitForConnectionAsync(deadline.Token);
    using var reader=new StreamReader(server,Encoding.UTF8,false,1024,true);
    using var request=JsonDocument.Parse((await reader.ReadLineAsync(deadline.Token))!);
    var r=request.RootElement;
    if(r.GetProperty("expectedInstanceId").GetString()!=instance.ToString("D") || r.GetProperty("method").GetString()!="get_state") throw new Exception("Wrong request identity/method");
    var reply=JsonSerializer.Serialize(new {version=1,requestId=r.GetProperty("requestId").GetString(),instanceId=(wrongIdentity ? Guid.NewGuid() : instance).ToString("D"),ok=true,result=new { revision=7 }})+"\n";
    await server.WriteAsync(Encoding.UTF8.GetBytes(reply),deadline.Token);await server.FlushAsync(deadline.Token);
    if(wrongIdentity)
    {
        bool rejected=false;try { await client; } catch(IOException) { rejected=true; }
        if(!rejected) throw new Exception("Wrong instance accepted");
    }
    else if((await client).GetProperty("revision").GetInt32()!=7) throw new Exception("Result differs");
}
using(var cancellation=new CancellationTokenSource(TimeSpan.FromMilliseconds(100)))
{
    bool cancelled=false;try { await new InstanceConnection(Guid.NewGuid()).RequestAsync("get_state",cancellation.Token); } catch(OperationCanceledException) { cancelled=true; }
    if(!cancelled) throw new Exception("Missing instance did not cancel");
}
Console.WriteLine("PASS: named-pipe request/result, wrong-instance rejection, unavailable-instance cancellation");
await McpProtocolVerification.RunAsync();
CaptureResultVerification.Run();
