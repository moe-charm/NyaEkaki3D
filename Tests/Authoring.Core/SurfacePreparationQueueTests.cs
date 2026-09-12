using System;
using System.Threading;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;

internal static partial class Program
{
    static SurfacePreparationInput PreparationInput(float width=100)=>new SurfacePreparationInput(RayTriangle(),new RestTransform(1,new Vec3()),
        new SurfaceCameraSnapshot(new Vec4(2,0,0,-1),new Vec4(0,2,0,-1),new Vec4(0,0,0,1),new Vec4(0,0,-1,1),new Vec2(),new Vec2(width,100),.01f,10));
    static PreparedSurface BuildPrepared(SurfacePreparationInput input,SurfacePaintMesh reusable)=>new PreparedSurface(
        reusable ?? new SurfacePaintMesh(input.Mesh,input.Transform,input.LogicalVertexIds),input.Camera.BuildCoverage(input.Mesh,input.Transform));
    static void AwaitPreparation(SurfacePreparationQueue queue) { True(queue.WorkerCompletion.Wait(5000)); }
    static void RunSurfacePreparationQueueTests()
    {
        Test("worker prepares a complete surface and reuses ray data for a new camera",()=>
        {
            using(var queue=new SurfacePreparationQueue())
            {
                long first=queue.Request(PreparationInput());AwaitPreparation(queue);var ready=queue.Read();
                Equal(SurfacePreparationPhase.Ready,ready.Phase);Equal(first,ready.Generation);
                True(ready.Surface.RayMesh.Raycast(new Vec3(.1f,.1f,1),new Vec3(0,0,-1))!=null);
                long second=queue.Request(PreparationInput(200));AwaitPreparation(queue);var changed=queue.Read();
                Equal(second,changed.Generation);Equal(SurfacePreparationPhase.Ready,changed.Phase);
                True(ReferenceEquals(ready.Surface.RayMesh,changed.Surface.RayMesh));False(ReferenceEquals(ready.Surface.Coverage,changed.Surface.Coverage));
                Equal(first,ready.Generation);Equal(SurfacePreparationPhase.Ready,ready.Phase);
                var source=PreparationInput();var ids=new ulong[]{1,2,3};
                var mapped=new SurfacePreparationInput(source.Mesh,source.Transform,source.Camera,ids);ids[0]=0;
                queue.Request(mapped);AwaitPreparation(queue);var mappedReady=queue.Read();Equal(SurfacePreparationPhase.Ready,mappedReady.Phase);
                False(ReferenceEquals(changed.Surface.RayMesh,mappedReady.Surface.RayMesh));
                Equal((ulong)1,mapped.LogicalVertexIds[0]);
                var reframed=mapped.WithCamera(PreparationInput(250).Camera);
                True(ReferenceEquals(mapped.LogicalVertexIds,reframed.LogicalVertexIds));
                True(ReferenceEquals(mapped.Mesh,reframed.Mesh));False(mapped.Camera.Equals(reframed.Camera));
                queue.Request(reframed);AwaitPreparation(queue);
                True(ReferenceEquals(mappedReady.Surface.RayMesh,queue.Read().Surface.RayMesh));
            }
        });
        Test("latest pending request replaces intermediate work and stale completion cannot publish",()=>
        {
            using(var entered=new ManualResetEventSlim()) using(var release=new ManualResetEventSlim())
            {
                int calls=0;SurfacePreparationInput last=null;bool cancelled=false;
                using(var queue=new SurfacePreparationQueue((input,ray,token)=>
                {
                    int call=Interlocked.Increment(ref calls);if(call==1) { entered.Set();True(release.Wait(5000));cancelled=token.IsCancellationRequested; }
                    last=input;return BuildPrepared(input,ray); // Deliberately ignores cancellation to test publication fencing.
                }))
                {
                    try
                    {
                        queue.Request(PreparationInput());True(entered.Wait(5000));queue.Request(PreparationInput(200));
                        var latest=PreparationInput(300);long id=queue.Request(latest);Equal(SurfacePreparationPhase.Preparing,queue.Read().Phase);
                        release.Set();AwaitPreparation(queue);var result=queue.Read();
                        Equal(2,calls);True(cancelled);True(ReferenceEquals(latest,last));Equal(id,result.Generation);Equal(SurfacePreparationPhase.Ready,result.Phase);
                    }
                    finally { release.Set(); }
                }
            }
        });
        Test("clear and dispose discard active work even if its builder returns afterwards",()=>
        {
            foreach(bool dispose in new[]{false,true})
            using(var entered=new ManualResetEventSlim()) using(var release=new ManualResetEventSlim())
            using(var queue=new SurfacePreparationQueue((input,ray,token)=> { entered.Set();True(release.Wait(5000));return BuildPrepared(input,ray); }))
            {
                try
                {
                    queue.Request(PreparationInput());True(entered.Wait(5000));
                    if(dispose) queue.Dispose();else queue.Clear();
                    var expected=dispose ? SurfacePreparationPhase.Disposed : SurfacePreparationPhase.Idle;
                    Equal(expected,queue.Read().Phase);True(queue.Read().Surface==null);
                    release.Set();AwaitPreparation(queue);Equal(expected,queue.Read().Phase);True(queue.Read().Surface==null);
                    if(!dispose) { queue.Request(PreparationInput());AwaitPreparation(queue);Equal(SurfacePreparationPhase.Ready,queue.Read().Phase); }
                }
                finally { release.Set(); }
            }
        });
        Test("preparation failures are observed and a later request can recover",()=>
        {
            int calls=0;
            using(var queue=new SurfacePreparationQueue((input,ray,token)=>
            { if(Interlocked.Increment(ref calls)==1) throw new InvalidOperationException("fixture failure");return BuildPrepared(input,ray); }))
            {
                queue.Request(PreparationInput());AwaitPreparation(queue);var failed=queue.Read();
                Equal(SurfacePreparationPhase.Failed,failed.Phase);Equal("fixture failure",failed.Error);True(failed.Surface==null);
                queue.Request(PreparationInput());AwaitPreparation(queue);Equal(SurfacePreparationPhase.Ready,queue.Read().Phase);
            }
        });
    }
}
