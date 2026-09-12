using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyEvidenceCameraGui(string output,Action<string> completed)
        {
            string failure=null;EvidenceCaptureRecord baseline=null;
            try
            {
                evidenceCameraSource.value=successfulEvidencePath;baseline=EvidenceCaptureReader.Read(successfulEvidencePath);
                evidenceReuseCamera.value=true;
                Execute(AuthoringOperation.TranslateVertices(new[]{0,4},new Vec3(.03f,0,0)));
                evidenceDirectory.value=Path.Combine(output,"evidence-fixed-camera");
            }
            catch(Exception e) { failure=e.ToString(); }
            yield return null;yield return null;controls.ScrollTo(evidenceCaptureButton);yield return null;yield return null;
            if(failure==null) try { PointerProbe.Click(evidenceCaptureButton);Check(evidenceBusy && !evidenceCameraSource.enabledSelf,"Comparison camera source not locked"); }
            catch(Exception e) { failure=e.ToString(); }
            while(evidenceBusy) yield return null;
            if(failure==null) try
            {
                Check(lastEvidencePath!=null,"Fixed camera capture failed: "+evidenceProgress.text);
                var after=EvidenceCaptureReader.Read(lastEvidencePath);bool changed=false;
                Check(after.Images.Count==baseline.Images.Count,"Comparison view count differs");
                for(int i=0;i<after.Images.Count;i++)
                {
                    var a=after.Images[i].View;var b=baseline.Images[i].View;
                    Check(a.Position.Equals(b.Position) && a.Target.Equals(b.Target) && a.Up.Equals(b.Up) && a.OrthographicSize==b.OrthographicSize && a.Near==b.Near && a.Far==b.Far && a.Width==b.Width && a.Height==b.Height,"Comparison camera refitted");
                    changed|=after.Images[i].PngHash!=baseline.Images[i].PngHash;
                }
                Check(changed,"Edited model did not change comparison images");
                evidenceCameraSource.value=Path.Combine(output,"missing.capture.json");
                bool rejected=false;try { ResolveEvidenceViews(EvaluatedSnapshot.Acquire(workspace)); } catch(Exception) { rejected=true; }
                Check(rejected,"Missing comparison source silently refitted");
            }
            catch(Exception e) { failure=e.ToString(); }
            evidenceReuseCamera.value=false;ReplaceWorkspace(AuthoringWorkspace.CreateFixture(),null);completed(failure);
        }
    }
}
