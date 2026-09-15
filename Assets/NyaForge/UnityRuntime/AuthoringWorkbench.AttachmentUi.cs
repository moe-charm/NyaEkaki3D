using System.Collections.Generic;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Builds only the attachment controls. State is declared in
        /// AttachmentState; operations remain in Attachments.
        /// </summary>
        void BuildAttachments(VisualElement parent)
        {
            attachmentPanel = new Foldout { text = "小物をボーンへ装着", value = false, name = "object-attachment" };
            attachmentStatus = new Label { name = "object-attachment-status" };
            attachmentStatus.style.whiteSpace = WhiteSpace.Normal;
            attachmentPanel.Add(attachmentStatus);
            var guide = new Label("手順：①衣装／小物を制作対象で選ぶ → ②対象avatarとBoneIdを選ぶ → ③剛体装着または衣装skin-bind → ④fit・weightを確認 → ⑤保存")
            {
                name = "object-attachment-guide"
            };
            guide.style.whiteSpace = WhiteSpace.Normal;
            attachmentPanel.Add(guide);
            attachmentPanel.Add(new Label("1  装着先を選択"));
            attachmentTarget = new DropdownField("アバター対象", new List<string> { "対象なし" }, 0) { name = "object-attachment-target" };
            attachmentTarget.RegisterValueChangedCallback(e =>
            {
                // The dropdown shows a friendly label, while the graph stores
                // the stable object ID. Keep both concerns explicit here.
                int index = attachmentTarget.choices.IndexOf(e.newValue);
                attachmentTargetChoice = index >= 0 && index < attachmentTargetIds.Count ? attachmentTargetIds[index] : null;
                RefreshAttachmentControls();
            });
            attachmentPanel.Add(attachmentTarget);
            attachmentPanel.Add(new Label("2  装着方法とBoneId"));
            attachmentBone = new DropdownField("BoneId", new List<string> { "対象なし" }, 0) { name = "object-attachment-bone" };
            attachmentPanel.Add(attachmentBone);
            attachmentOffsetX = Number(attachmentPanel, "bone local X (mm)", 0, "object-attachment-offset-x");
            attachmentOffsetY = Number(attachmentPanel, "bone local Y (mm)", 0, "object-attachment-offset-y");
            attachmentOffsetZ = Number(attachmentPanel, "bone local Z (mm)", 0, "object-attachment-offset-z");
            attachmentApply = Button("この小物を装着", ApplyAttachment, "object-attachment-apply");
            attachmentRemove = Button("装着を解除", RemoveAttachment, "object-attachment-remove");
            attachmentPanel.Add(new Label("剛体小物：1本のBoneIdへ固定"));
            accessorySkinBind = Button("衣装をavatar骨格へskin-bind（Root初期化）", BindAccessoryToAvatar, "object-skin-bind");
            accessoryPolygonMaterialize = Button("Polygon造形をskin衣装へ派生", MaterializePolygonAccessory, "object-polygon-materialize");
            attachmentPanel.Add(new Label("衣装化：複数boneへweightを設定"));
            accessoryAutoWeight = Button("衣装の自動weight初期化（骨近傍）", TransferAccessoryWeights, "object-skin-auto-weight");
            accessorySurfaceWeight = Button("衣装の自動weight初期化（avatar表面）", TransferAccessorySurfaceWeights, "object-skin-surface-weight");
            attachmentPanel.Add(new Label("3  fit・weightを調整"));
            accessoryFitSummary = new Label { name = "object-surface-fit-summary" };
            // Keep the compact controls column at its existing height. The
            // visible text is deliberately short enough for a narrow panel;
            // RefreshAccessoryFitSummary puts the full target identity and
            // counts in the tooltip for inspection without shifting controls.
            accessoryFitSummary.style.whiteSpace = WhiteSpace.NoWrap;
            accessoryFitSummary.tooltip = "現在のfit／weight対象、選択範囲、測定結果の有効性を表示します。面や頂点の指定を変えたら再測定してください。";
            attachmentPanel.Add(accessoryFitSummary);
            accessoryFitOffsetMm = Number(attachmentPanel, "avatar表面からのfit offset (mm)", 2, "object-surface-fit-offset-mm");
            accessoryFitMaxDistanceMm = Number(attachmentPanel, "surface fit最大距離 (mm)", 50, "object-surface-fit-max-distance-mm");
            accessorySurfaceTriangleIds = new TextField("avatar面ID（カンマ区切り・空欄=全て）") { name = "object-surface-triangle-ids" };
            accessorySurfaceTriangleIds.tooltip = "avatarのrest meshを三角形の通し番号で限定します。面IDはsubmesh順に0から数え、空欄なら全三角形を対象にします。fitとweightで同じ領域を使います。";
            attachmentPanel.Add(accessorySurfaceTriangleIds);
            accessorySurfaceTriangleIds.RegisterValueChangedCallback(_ => RefreshAccessoryFitSummary());
            accessorySurfacePickMode = new Toggle("クリックでavatar面を選択（Shiftで追加）") { name = "object-surface-pick-mode" };
            accessorySurfacePickMode.tooltip = "有効にするとビューポートのavatar面をクリックして領域を作ります。衣装頂点のクリック選択は一時停止します。";
            attachmentPanel.Add(accessorySurfacePickMode);
            accessoryClearSurfaceSelection = Button("avatar面領域を解除（全三角形）", ClearSurfaceTriangleSelection, "object-surface-clear-selection");
            attachmentPanel.Add(accessoryClearSurfaceSelection);
            accessoryClothingVertexIds = new TextField("衣装頂点ID（カンマ区切り・空欄=全て）") { name = "object-surface-clothing-vertex-ids" };
            accessoryClothingVertexIds.tooltip = "衣装EditMeshの頂点IDを限定します。空欄なら全頂点を対象にし、指定時は未選択頂点の位置・weightを保持します。";
            attachmentPanel.Add(accessoryClothingVertexIds);
            accessoryClothingVertexIds.RegisterValueChangedCallback(_ => RefreshAccessoryFitSummary());
            accessoryUseSelectedVertices = Button("現在の衣装頂点選択を適用対象にする", UseSelectedClothingVertices, "object-surface-use-selected-vertices");
            attachmentPanel.Add(accessoryUseSelectedVertices);
            accessorySurfaceFit = Button("衣装をavatar表面へfit", FitAccessoryToAvatarSurface, "object-surface-fit");
            accessorySurfaceInspect = Button("fit状態を測定（変更なし）", InspectAccessorySurfaceFit, "object-surface-inspect");
            accessoryPoseCopy = Button("avatarの現在poseを衣装へコピー", CopyAvatarPose, "object-skin-pose-copy");
            attachmentPanel.Add(attachmentApply); attachmentPanel.Add(attachmentRemove); attachmentPanel.Add(accessorySkinBind); attachmentPanel.Add(accessoryPolygonMaterialize); attachmentPanel.Add(accessoryAutoWeight); attachmentPanel.Add(accessorySurfaceWeight); attachmentPanel.Add(accessorySurfaceFit); attachmentPanel.Add(accessorySurfaceInspect); attachmentPanel.Add(accessoryPoseCopy);
            var helpPanel = new Foldout { text = "操作説明（詳細）", value = false, name = "object-attachment-help" };
            var help = new Label("明示したstable BoneIdへ剛体追従します。衣装skin-bindは選択avatarの骨格をコピーし、全頂点をRootへ初期化してRig panelでweight paintできます。Polygon造形をskin衣装へ派生すると、元のPolygon graphを残したまま編集結果をMeshSourceへ確定し、新しい衣装objectを作成します。自動weight初期化（骨近傍）はrest骨segmentへの距離から最大4本を選ぶ簡易初期値です。avatar表面が評価できる場合は、表面上の最近三角形から既存avatar weightを補間するavatar表面方式を推奨します。avatar面IDを指定するとfitとweightの対象面を同じ領域へ限定できます。衣装頂点IDを指定すると未選択頂点の位置・weightを保持できます。空欄は全てを対象にします。どちらも必ず動作確認・Rig panelで手修正してください。skin-bind後はavatarの現在poseをボタンで衣装へコピーして保存できます。名前で推測せず、装着offsetは基準姿勢のbone localメートルで保存します。fit状態の測定では最近面の法線に対する裏側候補も表示しますが、交差や貫通ゼロを保証する検査ではありません。");
            help.style.whiteSpace = WhiteSpace.Normal; helpPanel.Add(help); attachmentPanel.Add(helpPanel);
            parent.Add(attachmentPanel);
        }
    }
}
