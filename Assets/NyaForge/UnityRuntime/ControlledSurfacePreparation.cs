using System;
using System.Threading;
using NyaForge.Authoring.Paint;

namespace NyaForge.UnityRuntime
{
    /// <summary>Verification fixture: hold the first real worker while the Player processes UI events.</summary>
    internal sealed class ControlledSurfacePreparation : IDisposable
    {
        readonly ManualResetEventSlim entered=new ManualResetEventSlim();
        readonly ManualResetEventSlim release=new ManualResetEventSlim();
        readonly bool failFirst;
        int calls;
        public SurfacePreparationQueue Queue { get; }
        public bool Entered=>entered.IsSet;
        public int Calls=>Volatile.Read(ref calls);
        public ControlledSurfacePreparation(bool fail=false)
        { failFirst=fail;Queue=new SurfacePreparationQueue(Build); }
        public void Release()=>release.Set();
        PreparedSurface Build(SurfacePreparationInput input,SurfacePaintMesh reusable,CancellationToken cancellation)
        {
            if(Interlocked.Increment(ref calls)==1)
            {
                entered.Set();
                if(!release.Wait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Controlled preparation was not released.");
                if(failFirst) throw new InvalidOperationException("verification preparation failure");
            }
            // Intentionally finish even after cancellation: only the queue's generation fence can reject this result.
            return new PreparedSurface(reusable ?? new SurfacePaintMesh(input.Mesh,input.Transform,input.LogicalVertexIds),
                input.Camera.BuildCoverage(input.Mesh,input.Transform));
        }
        public void Dispose()
        {
            release.Set();Queue.Dispose();
            Queue.WorkerCompletion.ContinueWith(_=> { entered.Dispose();release.Dispose(); });
        }
    }
}
