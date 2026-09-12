using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace NyaForge.UnityRuntime.Platform
{
    internal static class AuthoringPipeRecoveryVerification
    {
        internal static async Task Run(string instance)
        {
            using(var timeout=new CancellationTokenSource(12000))
            {
                foreach(var frame in new[]{new byte[]{10},new byte[]{255,10},Encoding.UTF8.GetBytes("{broken}\n"),Encoding.UTF8.GetBytes("{\"method\":{}}\n")})
                {
                    using(var client=new NamedPipeClientStream(".","NyaForge.Authoring."+instance,PipeDirection.InOut,PipeOptions.Asynchronous))
                    {
                        await client.ConnectAsync(timeout.Token);
                        await client.WriteAsync(frame,0,frame.Length,timeout.Token);await client.FlushAsync(timeout.Token);
                        var one=new byte[1];
                        try
                        {
                            if(await client.ReadAsync(one,0,1,timeout.Token)!=0) throw new InvalidOperationException("Malformed request unexpectedly returned data.");
                        }
                        catch(IOException) { /* DisconnectNamedPipe may report broken pipe instead of EOF. */ }
                    }
                }
                // Peer closes before the newline; listener must return to accept the next client.
                using(var client=new NamedPipeClientStream(".","NyaForge.Authoring."+instance,PipeDirection.InOut,PipeOptions.Asynchronous))
                {
                    await client.ConnectAsync(timeout.Token);var partial=Encoding.UTF8.GetBytes("{\"version\":");
                    await client.WriteAsync(partial,0,partial.Length,timeout.Token);await client.FlushAsync(timeout.Token);
                }
            }
        }
    }
}
