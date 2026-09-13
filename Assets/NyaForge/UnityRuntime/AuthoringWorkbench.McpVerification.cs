using System;
using System.Collections;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyMcpListener(Action<string> completed)
        {
            pipeInstance=workspace.InstanceId;authoringPipe=new Platform.AuthoringPipeServer(pipeInstance);
            string instance=pipeInstance,document=workspace.Document.DocumentId;
            var request=Task.Run(async()=>
            {
                await Platform.AuthoringPipeRecoveryVerification.Run(instance);
                using(var timeout=new CancellationTokenSource(12000))
                using(var client=new NamedPipeClientStream(".","NyaForge.Authoring."+instance,PipeDirection.InOut,PipeOptions.Asynchronous))
                {
                    await client.ConnectAsync(timeout.Token);
                    string id=Guid.NewGuid().ToString("D");
                    var json=new JObject { ["version"]=1,["requestId"]=id,["expectedInstanceId"]=instance,["method"]="get_state" };
                    var bytes=Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None)+"\n");
                    await client.WriteAsync(bytes,0,bytes.Length,timeout.Token);await client.FlushAsync(timeout.Token);
                    using(var reader=new StreamReader(client))
                    {
                        var read=reader.ReadLineAsync();
                        if(await Task.WhenAny(read,Task.Delay(10000,timeout.Token))!=read) throw new TimeoutException("Player IPC response timed out");
                        var response=JObject.Parse(await read);
                        Check((bool)response["ok"] && (string)response["requestId"]==id,"Player IPC rejected state");
                        Check((string)response["result"]["documentId"]==document && (string)response["result"]["instanceId"]==instance,"Player IPC read different workspace");
                        Check(response["result"]["referenceProtectedObjectIds"] is JArray &&
                            (bool)response["result"]["activeObjectReferenceProtected"] == false,
                            "Player IPC state omitted reference protection metadata");
                    }
                }
            });
            while(!request.IsCompleted) yield return null;
            string failure=request.IsFaulted ? request.Exception.ToString() : request.IsCanceled ? "IPC test canceled" : null;
            StopMcp();completed(failure);
        }
    }
}
