using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Evidence;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Toggle evidenceReuseCamera;
        TextField evidenceCameraSource;
        Button evidenceUseLastCamera;
        void BuildEvidenceCamera(VisualElement parent)
        {
            evidenceReuseCamera=new Toggle("保存済み撮影セットと同じカメラで比較") { name="evidence-reuse-camera" };parent.Add(evidenceReuseCamera);
            evidenceReuseCamera.RegisterValueChangedCallback(_=>RefreshEvidenceCapture());
            evidenceCameraSource=new TextField("比較元の .capture.json") { name="evidence-camera-source" };
            evidenceCameraSource.style.flexDirection=FlexDirection.Column;parent.Add(evidenceCameraSource);
            evidenceUseLastCamera=Button("最後に保存したセットを比較元にする",()=>
            {
                evidenceCameraSource.value=successfulEvidencePath;evidenceReuseCamera.value=true;
            },"evidence-use-last-camera");parent.Add(evidenceUseLastCamera);
            var help=new Label("比較時は保存済みの全カメラ・倍率・解像度を再利用します。形が大きく変わると画面外にはみ出す場合があります。元の撮影セット一式を残してください。");
            help.style.whiteSpace=WhiteSpace.Normal;parent.Add(help);
        }
        void RefreshEvidenceCamera()
        {
            evidenceReuseCamera.SetEnabled(!evidenceBusy);evidenceCameraSource.SetEnabled(!evidenceBusy);
            evidenceUseLastCamera.SetEnabled(!evidenceBusy && !string.IsNullOrEmpty(successfulEvidencePath));
            evidenceCaptureButton.text=evidenceReuseCamera.value ? "同じカメラで画像と計測を保存" : "5方向の画像と計測を保存";
        }
        IReadOnlyList<EvidenceView> ResolveEvidenceViews(EvaluatedSnapshot snapshot)
        {
            if(!evidenceReuseCamera.value) return EvidenceViewPresets.FiveViews(snapshot);
            // Verify the complete package before accepting its cameras; never silently refit on failure.
            return EvidenceCaptureReader.Read(evidenceCameraSource.value).Images.Select(i=>i.View).ToArray();
        }
    }
}
