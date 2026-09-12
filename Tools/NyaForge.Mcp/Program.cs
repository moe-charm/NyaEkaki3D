using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NyaForge.Mcp;

if(args.Length!=2 || args[0]!="--instance" || !Guid.TryParseExact(args[1],"D",out var instance))
{
    Console.Error.WriteLine("Usage: NyaForge.Mcp --instance <NyaForge instance GUID>");
    return 2;
}
var builder=Host.CreateApplicationBuilder();
builder.Logging.ClearProviders();
builder.Logging.AddConsole(options=>options.LogToStandardErrorThreshold=LogLevel.Trace);
builder.Services.AddSingleton(new InstanceConnection(instance));
builder.Services.AddMcpServer().WithStdioServerTransport().WithTools<ForgeTools>();
await builder.Build().RunAsync();
return 0;
