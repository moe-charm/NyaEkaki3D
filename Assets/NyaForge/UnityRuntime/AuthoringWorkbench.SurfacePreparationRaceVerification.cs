using System;
using System.Collections;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator WaitPreparationProbe(Func<bool> condition)
        {
            float deadline=Time.realtimeSinceStartup+5;
            while(!condition() && Time.realtimeSinceStartup<deadline) yield return null;
        }
        // Uses the masked GUI fixture, preserving its committed document across each controlled race.
        IEnumerator VerifySurfacePreparationRaces()
        {
            var originalQueue=surfacePreparation;var originalWorkspace=workspace;var originalPath=savedDirectory;
            string state=workspace.Document.StateHash;
            foreach(string scenario in new[]{"camera","mode","workspace","failure"})
            {
                using(var probe=new ControlledSurfacePreparation(scenario=="failure"))
                {
                    try
                    {
                        surfacePaintMode.value=false;ClearSurfacePreparation();surfacePreparation=probe.Queue;
                        surfacePaintMode.value=true;Frame();
                        yield return WaitPreparationProbe(()=>probe.Entered);
                        Check(probe.Entered && probe.Queue.Read().Phase==SurfacePreparationPhase.Preparing,"Controlled worker did not start: "+scenario);
                        long first=surfacePreparationGeneration;
                        for(int i=0;i<8;i++) yield return null;
                        Check(surfacePreparationGeneration==first && probe.Calls==1,"Unchanged GUI re-requested preparation");
                        var point=VertexPanelPoint(Vector3.zero);
                        PointerProbe.Down(view,point);PointerProbe.Move(view,point+new Vector2(6,0));PointerProbe.Up(view,point);
                        Check(surfaceStroke==null && !projection.HasPaintPreview && workspace.Document.StateHash==state,"Preparing pointer changed paint");
                        Check(surfacePreparationLabel.text.Contains("準備中"),"Preparing status missing");
                        if(scenario=="camera")
                        {
                            for(int i=0;i<3;i++) { distance*=1.02f;UpdateCamera(); }
                            long latest=surfacePreparationGeneration;
                            Check(latest>first && ReadySurface()==null,"Camera change exposed stale prepared surface");
                            probe.Release();yield return WaitSurfaceReady();
                            Check(ReadySurface()!=null && probe.Calls==2 && surfacePreparationGeneration==latest,"Camera requests did not coalesce to latest");
                            Check(surfacePreparationInput.Camera.Equals(SurfaceCameraCapture.Capture(camera,view.worldBound)),"Wrong camera generation became ready");
                        }
                        else if(scenario=="mode")
                        {
                            surfacePaintMode.value=false;probe.Release();
                            yield return WaitPreparationProbe(()=>probe.Queue.WorkerCompletion.IsCompleted);
                            Check(probe.Queue.WorkerCompletion.IsCompleted && probe.Queue.Read().Phase==SurfacePreparationPhase.Idle && ReadySurface()==null,"Old worker survived mode disable");
                            surfacePaintMode.value=true;yield return WaitSurfaceReady();
                            Check(ReadySurface()!=null && surfacePreparationGeneration>first,"Mode re-enable did not prepare anew");
                        }
                        else if(scenario=="workspace")
                        {
                            ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);probe.Release();
                            yield return WaitPreparationProbe(()=>probe.Queue.WorkerCompletion.IsCompleted);
                            Check(workspace.Document.IsEmpty && probe.Queue.WorkerCompletion.IsCompleted && probe.Queue.Read().Phase==SurfacePreparationPhase.Idle && ReadySurface()==null,"Replaced workspace accepted old worker");
                            ReplaceWorkspace(originalWorkspace,originalPath);surfacePaintMode.value=true;yield return WaitSurfaceReady();
                            Check(ReadySurface()!=null && workspace.Document.StateHash==state,"Workspace restoration failed to prepare");
                        }
                        else
                        {
                            probe.Release();yield return WaitPreparationProbe(()=>probe.Queue.Read().Phase==SurfacePreparationPhase.Failed);
                            RefreshSurfacePreparation();
                            Check(probe.Queue.Read().Phase==SurfacePreparationPhase.Failed && surfacePreparationLabel.text.Contains("verification preparation failure") &&
                                surfacePreparationRetry.style.display.value==DisplayStyle.Flex,"Failure or retry control missing");
                            for(int i=0;i<8;i++) yield return null;
                            Check(probe.Calls==1,"Failure retried without user action");
                            controls.ScrollTo(surfacePreparationRetry);yield return null;yield return null;
                            PointerProbe.Click(surfacePreparationRetry);yield return WaitSurfaceReady();
                            Check(ReadySurface()!=null && probe.Calls==2 && surfacePreparationRetry.style.display.value==DisplayStyle.None,"Retry button did not recover");
                        }
                        Check(workspace.Document.StateHash==state && surfaceStroke==null && !projection.HasPaintPreview,"Race mutated committed paint: "+scenario);
                        PointerProbe.Up(view,point);Check(workspace.Document.StateHash==state,"Late up replayed preparing pointer");
                    }
                    finally
                    {
                        surfacePaintMode.SetValueWithoutNotify(false);ClearSurfacePreparation();surfacePreparation=originalQueue;
                        if(workspace!=originalWorkspace) ReplaceWorkspace(originalWorkspace,originalPath);
                        surfacePaintMode.value=true;Frame();
                    }
                }
            }
        }
    }
}
