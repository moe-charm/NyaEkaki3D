using System;
using System.Threading;
using System.Threading.Tasks;

namespace NyaForge.Authoring.Paint
{
    /// <summary>One worker plus one replaceable pending request. Only the latest generation may publish.</summary>
    public sealed class SurfacePreparationQueue : IDisposable
    {
        readonly object gate=new object();
        readonly Func<SurfacePreparationInput,SurfacePaintMesh,CancellationToken,PreparedSurface> build;
        SurfacePreparationStatus status=new SurfacePreparationStatus(0,SurfacePreparationPhase.Idle);
        SurfacePreparationInput pending,cachedInput;
        SurfacePaintMesh cachedRay;
        CancellationTokenSource active;
        bool running,disposed;
        long generation,pendingGeneration;
        Task worker=Task.CompletedTask;
        public SurfacePreparationQueue():this(Build) { }
        internal SurfacePreparationQueue(Func<SurfacePreparationInput,SurfacePaintMesh,CancellationToken,PreparedSurface> builder)
        { build=builder ?? throw new ArgumentNullException(nameof(builder)); }
        public SurfacePreparationStatus Read() { lock(gate) return status; }
        internal Task WorkerCompletion { get { lock(gate) return worker; } }
        public long Request(SurfacePreparationInput input)
        {
            if(input==null) throw new ArgumentNullException(nameof(input));
            lock(gate)
            {
                if(disposed) throw new ObjectDisposedException(nameof(SurfacePreparationQueue));
                pending=input;pendingGeneration=checked(++generation);active?.Cancel();
                status=new SurfacePreparationStatus(generation,SurfacePreparationPhase.Preparing);
                if(!running) { running=true;worker=Task.Run(Work); }
                return generation;
            }
        }
        public void Clear()
        {
            lock(gate)
            {
                if(disposed) return;
                generation=checked(generation+1);pending=null;cachedInput=null;cachedRay=null;active?.Cancel();
                status=new SurfacePreparationStatus(generation,SurfacePreparationPhase.Idle);
            }
        }
        public void Dispose()
        {
            lock(gate)
            {
                if(disposed) return;disposed=true;pending=null;cachedInput=null;cachedRay=null;active?.Cancel();
                status=new SurfacePreparationStatus(generation,SurfacePreparationPhase.Disposed);
            }
        }
        void Work()
        {
            while(true)
            {
                SurfacePreparationInput input;SurfacePaintMesh reusable;long id;CancellationTokenSource cancellation;
                lock(gate)
                {
                    if(disposed || pending==null) { running=false;return; }
                    input=pending;id=pendingGeneration;pending=null;
                    reusable=input.SameSurface(cachedInput) ? cachedRay : null;
                    cancellation=active=new CancellationTokenSource();
                }
                PreparedSurface result=null;string error=null;
                try
                {
                    result=build(input,reusable,cancellation.Token);
                    if(result==null || result.RayMesh==null || result.Coverage==null) throw new InvalidOperationException("Surface preparation returned no complete result.");
                }
                catch(OperationCanceledException) { error="Surface preparation cancelled."; }
                catch(Exception exception) { error=exception.Message; }
                lock(gate)
                {
                    active=null;
                    if(!disposed && id==generation)
                    {
                        if(error==null && !cancellation.IsCancellationRequested)
                        { cachedInput=input;cachedRay=result.RayMesh;status=new SurfacePreparationStatus(id,SurfacePreparationPhase.Ready,result); }
                        else status=new SurfacePreparationStatus(id,SurfacePreparationPhase.Failed,error:error ?? "Surface preparation cancelled.");
                    }
                }
                cancellation.Dispose();
            }
        }
        static PreparedSurface Build(SurfacePreparationInput input,SurfacePaintMesh reusable,CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var ray=reusable ?? new SurfacePaintMesh(input.Mesh,input.Transform,input.LogicalVertexIds);
            token.ThrowIfCancellationRequested();var coverage=input.Camera.BuildCoverage(input.Mesh,input.Transform);
            token.ThrowIfCancellationRequested();return new PreparedSurface(ray,coverage);
        }
    }
}
