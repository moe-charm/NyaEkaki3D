using System;
using System.Collections;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Evidence;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        IEnumerator VerifyEvidenceGui(string output,Action<string> completed)
        {
            var previous=workspace;string previousPath=savedDirectory,failure=null,before=null;
            try
            {
                try { ReplaceWorkspace(AuthoringWorkspace.CreateFixture(),null);before=workspace.Document.StateHash;evidencePanel.value=true;evidenceDirectory.value=Path.Combine(output,"evidence-gui"); }
                catch(Exception e) { failure=e.ToString(); }
                yield return null;yield return null;controls.ScrollTo(evidenceCaptureButton);yield return null;yield return null;
                if(failure==null) try { PointerProbe.Click(evidenceCaptureButton);Check(evidenceBusy && !evidenceCaptureButton.enabledSelf,"Evidence did not start exclusively"); }
                catch(Exception e) { failure=e.ToString(); }
                while(evidenceBusy) yield return null;
                if(failure==null) try
                {
                    Check(lastEvidencePath!=null,"Evidence GUI did not save: "+evidenceProgress.text);
                    Check(successfulEvidencePath==lastEvidencePath && evidenceResultPath.value==lastEvidencePath && evidenceOpenFolder.enabledSelf && evidenceCopyPath.enabledSelf,"Saved evidence result controls missing");
                    Check(EvidenceResultDirectory()==Path.GetDirectoryName(lastEvidencePath),"Evidence result directory differs");
                    var result=EvidenceCaptureReader.Read(lastEvidencePath);Check(result.Images.Count==5,"Evidence GUI view count differs");
                    Check(result.Images.Select(i=>i.View.Position).Distinct().Count()==5 && result.Images.Select(i=>i.View.OrthographicSize).Distinct().Count()==1,"Evidence presets do not share scale or unique positions");
                    Check(workspace.Document.StateHash==before,"Evidence GUI changed document");
                    SetStatus("確認画像5方向と計測を保存。制作データは未変更。");
                }
                catch(Exception e) { failure=e.ToString(); }
                if(failure==null) yield return WorkbenchCapture.Write(GetComponent<UIDocument>(),camera,Path.Combine(output,"evidence-gui.png"),error=>failure=error);
                if(failure==null) yield return VerifyEvidenceTargetGui(output,error=>failure=error);
                if(failure==null) yield return VerifyEvidenceCameraGui(output,error=>failure=error);
                if(failure==null) yield return VerifyEvidenceLifecycle(output,error=>failure=error);
                completed(failure);
            }
            finally { evidenceCancelled=true;evidencePanel.value=false;ReplaceWorkspace(previous,previousPath); }
        }
    }
}


