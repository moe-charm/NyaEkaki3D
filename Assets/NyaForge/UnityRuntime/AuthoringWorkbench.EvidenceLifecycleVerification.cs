using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyEvidenceLifecycle(string output,Action<string> completed)
        {
            string failure=null,before=workspace.Document.StateHash;string cancelled=Path.Combine(output,"evidence-cancelled"),blocked=Path.Combine(output,"evidence-blocked-file");
            string previousResult=successfulEvidencePath;evidenceDirectory.value=cancelled;
            yield return null;yield return null;controls.ScrollTo(evidenceCancelButton);yield return null;yield return null;
            try { PointerProbe.Click(evidenceCaptureButton);PointerProbe.Click(evidenceCancelButton); }
            catch(Exception e) { failure=e.ToString(); }
            while(evidenceBusy) yield return null;
            if(failure==null) try
            {
                Check(successfulEvidencePath==previousResult && evidenceOpenFolder.enabledSelf,"Cancellation erased last successful result");
                Check(lastEvidencePath==null && !Directory.Exists(cancelled) && evidenceProgress.text.Contains("中止"),"Cancelled capture was published");
                Check(workspace.Document.StateHash==before && evidenceCaptureButton.enabledSelf,"Cancel changed document or blocked retry");
                File.WriteAllText(blocked,"preserve this file");evidenceDirectory.value=blocked;PointerProbe.Click(evidenceCaptureButton);
            }
            catch(Exception e) { failure=e.ToString(); }
            while(evidenceBusy) yield return null;
            if(failure==null) try
            {
                Check(successfulEvidencePath==previousResult && evidenceCopyPath.enabledSelf,"Failure erased last successful result");
                Check(lastEvidencePath==null && evidenceProgress.text.Contains("保存できません") && File.ReadAllText(blocked)=="preserve this file","Save failure replaced existing data or reported success");
                Check(evidenceCaptureButton.enabledSelf && workspace.Document.StateHash==before,"Failure did not restore controls");
                evidenceDirectory.value=Path.Combine(output,"evidence-edit-during-capture");PointerProbe.Click(evidenceCaptureButton);
                Execute(AuthoringOperation.TranslateVertices(new[]{0,4},new Vec3(.03f,0,0)));
                Check(workspace.Document.StateHash!=before,"Live edit fixture did not change");
            }
            catch(Exception e) { failure=e.ToString(); }
            while(evidenceBusy) yield return null;
            if(failure==null) try
            {
                var read=EvidenceCaptureReader.Read(lastEvidencePath);Check((string)read.Metadata.CopyMetadata()["stateHash"]==before,"Live edit leaked into capture metadata");
                Check(workspace.Document.StateHash!=before,"Capture rolled back live edit");
                string capturedDocument=workspace.Document.DocumentId;
                evidenceDirectory.value=Path.Combine(output,"evidence-switch-during-capture");PointerProbe.Click(evidenceCaptureButton);
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(),null);
                // Persist the expected old document identity locally for the assertion after the job.
                before=capturedDocument;
            }
            catch(Exception e) { failure=e.ToString(); }
            while(evidenceBusy) yield return null;
            if(failure==null) try
            {
                Check(workspace.Document.IsEmpty && !evidenceCaptureButton.enabledSelf,"Capture switched the current workspace back");
                var read=EvidenceCaptureReader.Read(lastEvidencePath);Check((string)read.Metadata.CopyMetadata()["documentId"]==before,"Workspace switch retargeted capture");
                Check(evidenceProgress.text.Contains("現在の文書とは異なります"),"Old document capture completion is ambiguous");
            }
            catch(Exception e) { failure=e.ToString(); }
            completed(failure);
        }
    }
}

