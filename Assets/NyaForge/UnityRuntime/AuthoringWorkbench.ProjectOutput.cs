using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout projectOutputPanel;
        /// <summary>
        /// Owns the save/reopen and delivery controls as one panel. The
        /// callbacks still route through the existing Workbench command and
        /// export services; this file only owns their presentation order.
        /// </summary>
        void BuildProjectOutputPanel(VisualElement parent)
        {
            // Saving remains available in the command bar. Expand this longer
            // delivery panel when the user enters the review/output mode;
            // automated acceptance keeps it open for stable control probes.
            projectOutputPanel = new Foldout { text = "3  保存とUnityへの受け渡し", value = IsAutomatedUiVerification, name = "project-output" };
            var panel = projectOutputPanel;
            projectLabel = new Label(); panel.Add(projectLabel);
            projectPath = new TextField("制作フォルダ") { name = "authoring-project-path" };
            projectPath.style.flexDirection = FlexDirection.Column;
            projectPath.tooltip = "project.nyaforge.json と blobs を保存するフォルダ。開く場合もフォルダを指定します。";
            panel.Add(projectPath);
            projectManifestLabel = new Label { name = "authoring-project-manifest-path" };
            projectManifestLabel.style.whiteSpace = WhiteSpace.NoWrap;
            projectManifestLabel.style.overflow = Overflow.Hidden;
            projectManifestLabel.tooltip = "この制作状態の正本ファイル。表示が省略される場合はここへマウスを載せると全文を確認できます。";
            panel.Add(projectManifestLabel);
            var files = Row(panel);
            files.Add(Button("保存", SaveProject, "authoring-save"));
            files.Add(Button("開く", () => ConfirmReplace(OpenProject), "authoring-open"));
            files.Add(Button("Explorerで選ぶ…", BrowseProject, "authoring-project-browse"));
            panel.Add(Button("Unity用に書き出す", Export, "authoring-export"));
            panel.Add(Button("標準GLB（表示形状）", ExportGlbStatic, "authoring-export-glb-static"));
            panel.Add(Button("標準GLB（skin/morph保持）", ExportGlbSkinned, "authoring-export-glb-skinned"));
            panel.Add(Button("拡張GLB（全weight保持）", ExportGlbSkinnedExtended, "authoring-export-glb-skinned-extended"));
            panel.Add(Button("選択衣装をskin packageで出力", ExportSelectedClothingPackage, "authoring-export-clothing-package"));
            var outputHelp = new Label("保存はnative制作状態の再開用、GLB／Unity出力は受け渡し用です。装着情報を含む作品はnative projectへ出力します。標準GLBは互換用4 influence、拡張GLBは全weightを出力します。")
            {
                name = "project-output-help"
            };
            outputHelp.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(outputHelp);
            var vrmMetadata = new Foldout { text = "VRM 1.0 metadata", value = false, name = "authoring-vrm-metadata" };
            vrmName = new TextField("名前") { value = "NyaForge Avatar", name = "authoring-vrm-name" }; vrmMetadata.Add(vrmName);
            vrmAuthors = new TextField("作者（カンマ区切り）") { value = "NyaForge", name = "authoring-vrm-authors" }; vrmMetadata.Add(vrmAuthors);
            vrmLicenseUrl = new TextField("license URL") { value = "", name = "authoring-vrm-license-url" }; vrmMetadata.Add(vrmLicenseUrl);
            vrmRequireCompleteSemantics = new Toggle("入力の未保持情報があれば出力を停止")
            {
                value = false,
                name = "authoring-vrm-require-complete-semantics"
            };
            vrmRequireCompleteSemantics.tooltip = "入力VRM/GLBの診断に保持できない意味情報がある場合、VRM1の公開を止めます。既定ではpartialとしてreportへ記録します。";
            vrmMetadata.Add(vrmRequireCompleteSemantics);
            vrmMetadata.Add(new Label("VRM 1.0のhumanoid必須骨を検査します。license URLは作品の利用条件を指すURLを入力してください。解決できるmorph表情と、詳細が揃ったVRM1のSpringBoneを出力します。material bind・LookAt・FirstPerson・MToon・animationは出力しません。"));
            panel.Add(vrmMetadata);
            panel.Add(Button("VRM 1.0（humanoid）", ExportVrm1, "authoring-export-vrm1"));
            parent.Add(panel);
        }

        void BuildProjectStatus(VisualElement parent)
        {
            confirmRow = new VisualElement { name = "authoring-confirm" };
            confirmRow.style.display = DisplayStyle.None;
            parent.Add(confirmRow);
            status = new Label { name = "authoring-status" };
            status.AddToClassList("status");
            status.style.whiteSpace = WhiteSpace.Normal;
            status.style.overflow = Overflow.Visible;
            status.style.minHeight = 34;
            root.Add(status);
        }
    }
}
