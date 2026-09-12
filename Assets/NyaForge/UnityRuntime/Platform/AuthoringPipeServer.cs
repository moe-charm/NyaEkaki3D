using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Inspection;
namespace NyaForge.UnityRuntime.Platform
{
    /// <summary>Single-client, bounded local transport. Pump must be called on the application thread.</summary>
    internal sealed class AuthoringPipeServer : IDisposable
    {
        readonly string instance;
        readonly CancellationTokenSource stop=new CancellationTokenSource();
        readonly object gate=new object();
        TaskCompletionSource<JObject> pending;
        AuthoringIpcRequest pendingRequest;
        NamedPipeServerStream pipe;
        public string Failure { get; private set; }
        public AuthoringPipeServer(string instance) { this.instance=instance;Task.Run(Run); }
        public void Pump(Func<AuthoringIpcRequest,JObject> dispatch)
        {
            TaskCompletionSource<JObject> item;AuthoringIpcRequest request;lock(gate) { item=pending;request=pendingRequest;pending=null; }
            if(item==null || item.Task.IsCompleted) return;
            try { item.TrySetResult(dispatch(request)); } catch(Exception e) { item.TrySetException(e); }
        }
        async Task Run()
        {
            try
            {
                using(var server=WindowsAuthoringPipe.Create(instance))
                {
                    lock(gate) { if(stop.IsCancellationRequested) return;pipe=server; }
                    while(!stop.IsCancellationRequested)
                    {
                        await server.WaitForConnectionAsync(stop.Token);
                        using(var timeout=CancellationTokenSource.CreateLinkedTokenSource(stop.Token))
                        {
                            timeout.CancelAfter(8000);
                            try { await Respond(server,timeout.Token); }
                            catch(Exception e) when(e is IOException || e is OperationCanceledException || e is JsonException || e is ArgumentException) { }
                        }
                        lock(gate) { pending?.TrySetCanceled();pending=null; }
                        server.Disconnect();
                    }
                }
            }
            catch(Exception e) { if(!stop.IsCancellationRequested) Failure=e.ToString(); }
        }
        async Task Respond(Stream stream,CancellationToken token)
        {
            var bytes=new MemoryStream();var one=new byte[1];
            while(true)
            {
                if(await stream.ReadAsync(one,0,1,token)==0) return;
                if(one[0]==10) break;
                if(bytes.Length>=65536) throw new IOException("Request exceeds limit.");bytes.WriteByte(one[0]);
            }
            AuthoringIpcRequest request;
            try { request=AuthoringIpcRequest.Parse(bytes.ToArray()); }
            catch(Exception e) { throw new IOException("Invalid read request",e); }
            string id=request.RequestId;
            var reply=new JObject { ["version"]=1,["requestId"]=id,["instanceId"]=instance,["ok"]=false };
            try
            {
                if(request.ExpectedInstanceId!=instance) throw new InvalidOperationException("STALE_INSTANCE");
                var completion=new TaskCompletionSource<JObject>(TaskCreationOptions.RunContinuationsAsynchronously);
                lock(gate) { pendingRequest=request;pending=completion; }
                using(token.Register(()=>completion.TrySetCanceled())) reply["result"]=await completion.Task;
                reply["ok"]=true;
            }
            catch(OperationCanceledException) { throw; }
            catch(Exception e) { reply["error"]=e.Message; }
            var response=Encoding.UTF8.GetBytes(reply.ToString(Formatting.None)+"\n");
            await stream.WriteAsync(response,0,response.Length,token);await stream.FlushAsync(token);
            // DisconnectNamedPipe discards unread output. Let the one-request client consume
            // the response and close first; the connection deadline bounds a stalled client.
            await stream.ReadAsync(one,0,1,token);
        }
        public void Dispose()
        {
            stop.Cancel();lock(gate) { pending?.TrySetCanceled();pending=null;pipe?.Dispose();pipe=null; }
        }
    }
}
