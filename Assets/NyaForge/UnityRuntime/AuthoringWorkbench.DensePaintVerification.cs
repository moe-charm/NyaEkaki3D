using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        [Serializable] sealed class DensePaintReport
        {
            public string unityVersion,failure;
            public int triangles;
            public double preparationWaitMs;
            public DensePaintPerformance performance;
            public List<DensePaintStroke> strokes=new List<DensePaintStroke>();
        }
        [Serializable] sealed class DensePaintPerformance
        {
            public string sampleKind="synthetic dense-paint fixture";
            public int frameSamples;
            public double frameBudgetMs=250,frameMinMs,frameAverageMs,frameP95Ms,frameMaxMs;
            public bool frameBudgetExceeded;
            public string status="not_sampled";
            public int targetFrameRate,vSyncCount,renderFrameInterval;
            public string graphicsDevice;
            public long managedHeapBefore,managedHeapAfter,managedAllocatedBytesBefore,managedAllocatedBytesAfter;
            public bool managedAllocatedBytesAvailable;
            public long unityAllocatedBytesBefore,unityAllocatedBytesAfter,unityReservedBytesBefore,unityReservedBytesAfter;
            public int gcCollection0Before,gcCollection0After;
        }
        [Serializable] sealed class DensePaintStroke
        {
            public int index,samples,rawPoints,retainedPoints;
            public float screenLength;
            public double downMs,moveTotalMs,moveMaxMs,upMs;
            public long managedHeapBefore,managedHeapAfter;
            public WorkbenchCommandMeasurement command;
        }
        IEnumerator VerifyDensePaint(string directory,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory;
            var report=new DensePaintReport { unityVersion=Application.unityVersion };
            try
            {
                try
                {
                    var mesh=SurfacePaintFixtures.Grid(256);report.triangles=mesh.Faces.Count*2;
                    string source=Guid.NewGuid().ToString("D"),paint=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
                    var graph=new AuthoringGraph(Guid.NewGuid().ToString("D"),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),
                        GraphNode.Paint(paint),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",paint,"mesh"),new GraphEdge(source,"mesh",output,"mesh"),new GraphEdge(paint,"image",output,"baseColor")},output);
                    var context=PaintEditing.Context(graph,paint);
                    var layer=new PaintLayer(Guid.NewGuid().ToString("D"),"Dense paint",new PaintImage(256,256,new Rgba32(0,0,0,0)));
                    graph=graph.ReplaceNode(GraphNode.LayeredPaint(paint,new PaintLayers(256,256,new[]{layer}),context.UvHash,context.MeshDomain));
                    ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);Execute(AuthoringOperation.AddGraph(graph));
                    paintPanel.value=true;surfacePaintMode.value=true;Frame();
                    paintRadius.SetValueWithoutNotify(4);paintOpacity.SetValueWithoutNotify(1);paintPalette.SetValueWithoutNotify(paintPalette.choices[0]);
                }
                catch(Exception e) { report.failure=e.ToString(); }
                yield return null;yield return null;
                if(report.failure==null) yield return SampleDensePaintFrames(report,30);
                if(report.failure==null) try
                {
                    float span=Vector2.Distance(VertexPanelPoint(new Vector3(-.08f,0,0)),VertexPanelPoint(new Vector3(.08f,0,0)));
                    distance*=span/(view.worldBound.width*.8f);UpdateCamera();
                }
                catch(Exception e) { report.failure=e.ToString(); }
                yield return null;yield return null;
                for(int stroke=0;stroke<2 && report.failure==null;stroke++)
                {
                    var preparationWatch=Stopwatch.StartNew();
                    yield return WaitSurfaceReady(error=>report.failure=error);
                    report.preparationWaitMs+=preparationWatch.Elapsed.TotalMilliseconds;
                    if(report.failure!=null) break;
                    try
                    {
                        float y=stroke==0 ? -.013f : .013f;
                        var a=VertexPanelPoint(new Vector3(-.08f,y,0));var b=VertexPanelPoint(new Vector3(.08f,y,0));
                        var metric=new DensePaintStroke { index=stroke,screenLength=Vector2.Distance(a,b),managedHeapBefore=GC.GetTotalMemory(false) };report.strokes.Add(metric);
                        Check(view.worldBound.Contains(a) && view.worldBound.Contains(b),"Dense stroke lies outside viewport");
                        string before=workspace.Document.StateHash;long revision=workspace.Document.DocumentRevision;
                        var displayMesh=projection.DisplayMesh;var displayObject=projection.DisplayObject;
                        var watch=Stopwatch.StartNew();PointerProbe.Down(view,a);metric.downMs=watch.Elapsed.TotalMilliseconds;
                        for(int step=1;step<=16;step++)
                        {
                            watch.Restart();PointerProbe.Move(view,Vector2.Lerp(a,b,step/16f));double elapsed=watch.Elapsed.TotalMilliseconds;
                            metric.moveTotalMs+=elapsed;metric.moveMaxMs=Math.Max(metric.moveMaxMs,elapsed);
                            Check(surfaceStroke!=null,"Dense stroke was cancelled during input");
                        }
                        metric.samples=surfaceStroke.RaySampleCount;metric.rawPoints=surfaceStroke.PointCount;
                        var path=surfaceStroke.Snapshot();metric.retainedPoints=path.PointCount;
                        Check(path.Sections.Count==1 && path.PointCount<=4 && metric.rawPoints>1024,"Dense stroke simplification did not retain one straight path");
                        Check(workspace.Document.StateHash==before && projection.HasPaintPreview,"Dense preview was not isolated");
                        watch.Restart();measureCommands=true;
                        try { PointerProbe.Up(view,b); } finally { measureCommands=false; }
                        metric.upMs=watch.Elapsed.TotalMilliseconds;metric.command=commandMeasurement;metric.managedHeapAfter=GC.GetTotalMemory(false);
                        string after=workspace.Document.StateHash;
                        Check(after!=before && workspace.Document.DocumentRevision==revision+1,"Dense stroke did not commit once");
                        Check(projection.DisplayMesh==displayMesh && projection.DisplayObject==displayObject,"Paint commit rebuilt unchanged display geometry");
                        int row=(int)((y+.05f)/.1f*256);
                        for(int x=27;x<229;x++) Check(CurrentPaintLayer().Image.GetPixel(x,row).A>0,"Dense stroke has a raster gap");
                        Execute(AuthoringOperation.Undo());Check(workspace.Document.StateHash==before,"Dense Undo differs");
                        Execute(AuthoringOperation.Redo());Check(workspace.Document.StateHash==after,"Dense Redo differs");
                        Check(projection.DisplayMesh==displayMesh && projection.DisplayObject==displayObject,"Paint Undo/Redo rebuilt unchanged display geometry");
                    }
                    catch(Exception e) { report.failure=e.ToString(); }
                    yield return null;yield return null;
                }
                if(report.failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(directory,"dense-paint.png"),error=>report.failure=error);
                File.WriteAllText(Path.Combine(directory,"dense-paint-profile.json"),JsonUtility.ToJson(report,true));
                completed(report.failure);
            }
            finally { CancelSurfaceStroke();surfacePaintMode.SetValueWithoutNotify(false);paintPanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }

        IEnumerator SampleDensePaintFrames(DensePaintReport report,int count)
        {
            var performance=new DensePaintPerformance(); report.performance=performance;
            performance.targetFrameRate=Application.targetFrameRate;
            performance.vSyncCount=QualitySettings.vSyncCount;
            performance.renderFrameInterval=UnityEngine.Rendering.OnDemandRendering.renderFrameInterval;
            performance.graphicsDevice=SystemInfo.graphicsDeviceName;
            performance.managedHeapBefore=GC.GetTotalMemory(false);
            performance.managedAllocatedBytesBefore=GC.GetAllocatedBytesForCurrentThread();
            performance.unityAllocatedBytesBefore=Profiler.GetTotalAllocatedMemoryLong();
            performance.unityReservedBytesBefore=Profiler.GetTotalReservedMemoryLong();
            performance.gcCollection0Before=GC.CollectionCount(0);
            var samples=new List<double>(count);
            for(int i=0;i<3;i++) yield return null;
            for(int i=0;i<count;i++)
            {
                var watch=Stopwatch.StartNew();
                yield return null;
                double elapsed=watch.Elapsed.TotalMilliseconds;
                // Time.unscaledDeltaTime is the Player's frame interval. The
                // stopwatch is retained as a fallback for headless/early frames.
                double frameMs=Time.unscaledDeltaTime>0 ? Time.unscaledDeltaTime*1000.0 : elapsed;
                samples.Add(frameMs);
            }
            performance.frameSamples=samples.Count;
            if(samples.Count>0)
            {
                samples.Sort();
                performance.frameMinMs=samples[0];
                performance.frameMaxMs=samples[samples.Count-1];
                performance.frameAverageMs=samples.Average();
                int p95=Math.Min(samples.Count-1,(int)Math.Ceiling(samples.Count*.95)-1);
                performance.frameP95Ms=samples[Math.Max(0,p95)];
                performance.frameBudgetExceeded=samples.Any(sample=>sample>performance.frameBudgetMs);
                performance.status=performance.frameBudgetExceeded ? "over_budget_observed" : "within_observed_budget";
            }
            performance.managedHeapAfter=GC.GetTotalMemory(false);
            performance.managedAllocatedBytesAfter=GC.GetAllocatedBytesForCurrentThread();
            performance.managedAllocatedBytesAvailable=performance.managedAllocatedBytesBefore>0 || performance.managedAllocatedBytesAfter>0;
            performance.unityAllocatedBytesAfter=Profiler.GetTotalAllocatedMemoryLong();
            performance.unityReservedBytesAfter=Profiler.GetTotalReservedMemoryLong();
            performance.gcCollection0After=GC.CollectionCount(0);
        }
    }
}
