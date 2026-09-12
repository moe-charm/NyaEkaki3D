using System;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        string successfulEvidencePath;
        TextField evidenceResultPath;
        Button evidenceOpenFolder,evidenceCopyPath;
        void BuildEvidenceResult(VisualElement parent)
        {
            evidenceResultPath=new TextField("最後に保存した撮影セット") { isReadOnly=true,multiline=true,name="evidence-result-path" };
            evidenceResultPath.style.flexDirection=FlexDirection.Column;evidenceResultPath.style.whiteSpace=WhiteSpace.Normal;parent.Add(evidenceResultPath);
            evidenceOpenFolder=Button("保存先フォルダを開く",()=>Try(()=>
            {
                string directory=EvidenceResultDirectory();
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName=directory,UseShellExecute=true });
            }),"evidence-open-folder");parent.Add(evidenceOpenFolder);
            evidenceCopyPath=Button("撮影セットのパスをコピー",()=>Try(()=>
            {
                EvidenceResultDirectory();GUIUtility.systemCopyBuffer=successfulEvidencePath;
            }),"evidence-copy-path");parent.Add(evidenceCopyPath);
            RefreshEvidenceResult();
        }
        string EvidenceResultDirectory()
        {
            if(string.IsNullOrEmpty(successfulEvidencePath) || !File.Exists(successfulEvidencePath)) throw new InvalidOperationException("最後に保存した撮影セットが見つかりません。保存先を確認してください。");
            return Path.GetDirectoryName(successfulEvidencePath);
        }
        void RecordEvidenceResult(string path)
        {
            successfulEvidencePath=path;RefreshEvidenceResult();
        }
        void RefreshEvidenceResult()
        {
            if(evidenceResultPath==null) return;
            evidenceResultPath.SetValueWithoutNotify(successfulEvidencePath ?? "");
            bool exists=!string.IsNullOrEmpty(successfulEvidencePath) && File.Exists(successfulEvidencePath);
            evidenceOpenFolder.SetEnabled(exists);evidenceCopyPath.SetEnabled(exists);
        }
    }
}
