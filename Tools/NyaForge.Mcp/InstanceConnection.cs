using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace NyaForge.Mcp;

/// <summary>One bounded request per connection. Never retries a request or chooses another instance.</summary>
public sealed class InstanceConnection(Guid instance)
{
    const int MaxResponseBytes=4*1024*1024;
    public async Task<JsonElement> RequestAsync(string method,CancellationToken cancellationToken,object? command=null,string payloadField="command")
    {
        using var timeout=CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        var token=timeout.Token;string requestId=Guid.NewGuid().ToString("D");
        using var pipe=new NamedPipeClientStream(".","NyaForge.Authoring."+instance.ToString("D"),PipeDirection.InOut,PipeOptions.Asynchronous);
        await pipe.ConnectAsync(token);
        var body=new Dictionary<string,object?> { ["version"]=1,["requestId"]=requestId,["expectedInstanceId"]=instance.ToString("D"),["method"]=method };
        if(command!=null) body[payloadField]=command;
        var request=JsonSerializer.SerializeToUtf8Bytes(body);
        if(request.Length>65536) throw new ArgumentException("Request exceeds IPC limit");
        await pipe.WriteAsync(request,token);await pipe.WriteAsync(new byte[]{10},token);await pipe.FlushAsync(token);
        using var buffer=new MemoryStream();var one=new byte[1];
        while(true)
        {
            int count=await pipe.ReadAsync(one,token);
            if(count==0) throw new IOException("NyaForge closed the connection before its response completed.");
            if(one[0]==10) break;
            if(buffer.Length>=MaxResponseBytes) throw new IOException("NyaForge response exceeds the IPC limit.");
            buffer.WriteByte(one[0]);
        }
        using var document=JsonDocument.Parse(new UTF8Encoding(false,true).GetString(buffer.ToArray()),new JsonDocumentOptions { MaxDepth=32 });
        var root=document.RootElement;
        if(root.GetProperty("version").GetInt32()!=1 || root.GetProperty("requestId").GetString()!=requestId || root.GetProperty("instanceId").GetString()!=instance.ToString("D"))
            throw new IOException("NyaForge response identity does not match the requested instance and request.");
        if(!root.GetProperty("ok").GetBoolean()) throw new IOException(root.GetProperty("error").GetString() ?? "NyaForge rejected the request.");
        return root.GetProperty("result").Clone();
    }
}
