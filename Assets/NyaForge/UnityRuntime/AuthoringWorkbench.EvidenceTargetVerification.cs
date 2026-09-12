using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring.Evidence;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyEvidenceTargetGui(string output,Action<string> completed)
        {
            string failure=null,node=null,before=workspace.Document.StateHash;
            try
            {
                int index=evidenceTargets.FindIndex(t=>t.Kind==EvidenceTargetKind.NodeOutput);
                Check(index>0,"Evidence node output choice missing");
                evidenceTargetField.value=evidenceTargetField.choices[index];node=selectedEvidenceTarget.NodeId;
                Check(CanCaptureEvidenceTarget(),"Resolved node capture disabled");
                evidenceDirectory.value=Path.Combine(output,"evidence-node-gui");
            }
            catch(Exception e) { failure=e.ToString(); }
            yield return null;yield return null;controls.ScrollTo(evidenceCaptureButton);yield return null;yield return null;
            if(failure==null) try { PointerProbe.Click(evidenceCaptureButton);Check(evidenceBusy && !evidenceTargetField.enabledSelf,"Target not locked during capture"); }
            catch(Exception e) { failure=e.ToString(); }
            while(evidenceBusy) yield return null;
            if(failure==null) try
            {
                Check(lastEvidencePath!=null,"Node capture failed: "+evidenceProgress.text);
                var metadata=EvidenceCaptureReader.Read(lastEvidencePath).Metadata.CopyMetadata();
                Check((string)metadata["target"]["nodeId"]==node,"Captured node identity differs");
                Check(workspace.Document.StateHash==before,"Target selection/capture changed document");
            }
            catch(Exception e) { failure=e.ToString(); }
            evidenceTargetField.value=evidenceTargetField.choices[0];
            completed(failure);
        }
    }
}
