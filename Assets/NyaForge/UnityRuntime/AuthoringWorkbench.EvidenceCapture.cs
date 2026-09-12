using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring.Evidence;
using UnityEngine;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout evidencePanel;
        TextField evidenceDirectory;
        Button evidenceCaptureButton,evidenceCancelButton;
        Label evidenceProgress;
        bool evidenceBusy,evidenceCancelled;
        string lastEvidencePath;
        void BuildEvidenceCapture(VisualElement parent)
        {
            evidencePanel=new Foldout { text="モデルの確認画像を保存",value=false,name="evidence-panel" };parent.Add(evidencePanel);
            var help=new Label("選んだ対象を正面・背面・左・右・斜めの5方向で撮影します。中心と倍率は共通。撮影開始時の状態を保存し、制作データは変更しません。");help.style.whiteSpace=WhiteSpace.Normal;evidencePanel.Add(help);BuildEvidenceTarget(evidencePanel);
            evidenceDirectory=new TextField("保存先フォルダ") { value=Path.Combine(Application.persistentDataPath,"Evidence"),name="evidence-directory" };evidenceDirectory.style.flexDirection=FlexDirection.Column;evidencePanel.Add(evidenceDirectory);
            BuildEvidenceCamera(evidencePanel);
            evidenceCaptureButton=Button("5方向の画像と計測を保存",()=>Try(()=> { if(!evidenceBusy) StartCoroutine(CaptureEvidence()); }),"capture-evidence");evidencePanel.Add(evidenceCaptureButton);
            evidenceCancelButton=Button("撮影を中止",()=>evidenceCancelled=true,"cancel-evidence");evidencePanel.Add(evidenceCancelButton);
            evidenceProgress=new Label();evidenceProgress.style.whiteSpace=WhiteSpace.Normal;evidencePanel.Add(evidenceProgress);BuildEvidenceResult(evidencePanel);RefreshEvidenceCapture();
        }
        void RefreshEvidenceCapture()
        {
            if(evidenceCaptureButton==null) return;
            RefreshEvidenceTarget();RefreshEvidenceCamera();
            evidenceCaptureButton.SetEnabled(!evidenceBusy && CanCaptureEvidenceTarget());
            evidenceCancelButton.SetEnabled(evidenceBusy);evidenceDirectory.SetEnabled(!evidenceBusy);RefreshEvidenceResult();
        }
        IEnumerator CaptureEvidence()
        {
            evidenceBusy=true;evidenceCancelled=false;lastEvidencePath=null;RefreshEvidenceCapture();
            string failure=null,path=null;EvaluatedSnapshot snapshot=null;IReadOnlyList<EvidenceView> views=null;var images=new List<EvidenceImage>();
            try
            {
                try { path=Path.GetFullPath(evidenceDirectory.value);snapshot=EvaluatedSnapshot.Acquire(workspace,selectedEvidenceTarget);views=ResolveEvidenceViews(snapshot); }
                catch(Exception e) { failure=e.Message; }
                if(failure==null) for(int i=0;i<views.Count;i++)
                {
                    evidenceProgress.text="撮影 "+(i+1)+" / "+views.Count+" · 文書rev "+snapshot.DocumentRevision;
                    yield return null;
                    if(evidenceCancelled) break;
                    try { images.Add(EvidenceModelCapture.Capture(snapshot,views[i])); }
                    catch(Exception e) { failure=e.Message;break; }
                }
                if(failure==null && !evidenceCancelled)
                {
                    try { lastEvidencePath=EvidenceCaptureStore.Save(path,new EvidenceCaptureSet(images));RecordEvidenceResult(lastEvidencePath); }
                    catch(Exception e) { failure=e.Message; }
                }
                evidenceProgress.text=failure!=null ? "保存できませんでした："+failure : evidenceCancelled ? "撮影を中止しました。撮影セットは保存していません。" : "保存しました（文書rev "+snapshot.DocumentRevision+"）：\n"+lastEvidencePath;
            }
            finally { if(lastEvidencePath!=null && snapshot!=null && snapshot.DocumentId!=workspace.Document.DocumentId) evidenceProgress.text+="\n撮影開始時の文書の結果です。現在の文書とは異なります。";evidenceBusy=false;RefreshEvidenceCapture(); }
        }
    }
}



