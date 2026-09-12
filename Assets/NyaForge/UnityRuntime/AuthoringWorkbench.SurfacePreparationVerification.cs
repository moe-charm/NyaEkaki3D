using System;
using System.Collections;
using NyaForge.Authoring.Paint;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator WaitSurfaceReady(Action<string> failed=null)
        {
            float deadline=Time.realtimeSinceStartup+15;
            do
            {
                if(ReadySurface()!=null) yield break;
                if(surfacePreparation.Read().Phase==SurfacePreparationPhase.Failed) break;
                yield return null;
            } while(Time.realtimeSinceStartup<deadline);
            failed?.Invoke("Surface preparation did not become Ready: "+surfacePreparationLabel.text);
        }
        IEnumerator VerifySurfaceLifetimeSteps(string directory,Action<string> completed)
            =>VerifySurfaceSteps(VerifySurfaceLifetimes(directory),completed);
        IEnumerator VerifySurfaceSteps(IEnumerator steps,Action<string> completed)
        {
            string failure=null;
            try
            {
                while(true)
                {
                    bool more=false;object next=null;
                    try { more=steps.MoveNext();if(more) next=steps.Current; }
                    catch(Exception error) { failure=error.ToString(); }
                    if(!more || failure!=null) break;
                    yield return next;
                }
            }
            finally { (steps as IDisposable)?.Dispose(); }
            completed(failure);
        }
    }
}
