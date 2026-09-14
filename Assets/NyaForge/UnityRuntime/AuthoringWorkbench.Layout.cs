using System;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Builds the authoring shell and composes feature panels. Feature
        /// behavior remains in its focused partial; this file owns only the
        /// layout order and shared viewport/control containers.
        /// </summary>
        void BuildUi(VisualElement parent)
        {
            root = new VisualElement { name = "authoring-workbench" };
            // Authoring replaces the viewer's existing root children at runtime.
            // Pin the workbench to the full panel instead of relying on a
            // flex-grow-only child; when it is opened directly from a Player
            // command line, the otherwise empty parent can resolve its content
            // width from the viewport and push the controls column off-screen.
            root.style.position = Position.Absolute;
            root.style.left = root.style.top = 0;
            root.style.right = root.style.bottom = 0;
            // PanelSettings.scale enlarges layout pixels to the physical
            // drawable surface. Reserve the corresponding logical width once
            // for the actual workbench; dividing by the DPI factor twice
            // leaves an avoidable blank strip and makes the controls tiny.
            // Injected panel-space probes stay at 1:1.
            bool injectedUiProbe = Environment.GetCommandLineArgs().Any(a => a == "--authoring-check-output" || a == "--navigation-check-output");
            bool dynamicDpiBounds = !injectedUiProbe && Screen.dpi > 96f;
            Action refreshDpiBounds = () =>
            {
                if (!dynamicDpiBounds || root == null) return;
                float dpiScale = Mathf.Clamp(Screen.dpi / 96f, 1f, 2f);
                root.style.right = StyleKeyword.Auto;
                root.style.bottom = StyleKeyword.Auto;
                root.style.width = Screen.width / dpiScale;
                root.style.height = Screen.height / dpiScale;
            };
            refreshDpiBounds();
            root.style.flexGrow = 1;
            root.style.minHeight = 0;
            parent.Add(root);
            if (dynamicDpiBounds)
            {
                // Screen dimensions can change after the workbench is opened
                // (window resize or monitor move). Recompute the logical bounds
                // instead of leaving the initial DPI-sized rectangle fixed.
                parent.RegisterCallback<GeometryChangedEvent>(_ => refreshDpiBounds());
            }

            var top = Row(root);
            top.style.backgroundColor = new Color(.09f, .14f, .18f);
            top.Add(new Label("NyaForge  /  制作プレビュー") { name = "authoring-title" });
            top.Add(Button("ビューワーに戻る", () =>
            {
                uvPreview?.CancelIslandDrag();
                paintCanvas?.CancelStroke();
                ClearSurfacePreparation();
                active = false;
                root.style.display = DisplayStyle.None;
                stage.SetActive(false);
                close?.Invoke();
            }, "authoring-close"));
            top.Add(Button("ノード表示 / 非表示", () =>
            {
                graphCanvas.style.display = graphCanvas.resolvedStyle.display == DisplayStyle.None
                    ? DisplayStyle.Flex
                    : DisplayStyle.None;
            }, "graph-toggle"));
            // Keep the navigation labels readable on a narrow DPI-scaled
            // window. Row() already wraps; disabling shrink makes the two
            // controls move to a second line instead of truncating text.
            top.Q<Label>("authoring-title").style.flexShrink = 0;
            top.Query<Button>().ForEach(button => button.style.flexShrink = 0);

            BuildCommandBar(root);
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1;
            body.style.flexBasis = 0;
            body.style.minHeight = 0;
            root.Add(body);

            var left = new VisualElement();
            left.style.flexGrow = 1;
            left.style.flexBasis = 0;
            left.style.flexShrink = 1;
            left.style.minWidth = 150;
            left.style.minHeight = 0;
            body.Add(left);
            view = new VisualElement { name = "authoring-viewport" };
            view.style.flexGrow = 1;
            view.style.minWidth = 150;
            view.style.minHeight = 100;
            left.Add(view);
            graphCanvas = new GraphCanvas();
            graphCanvas.style.display = DisplayStyle.None;
            left.Add(graphCanvas);
            previewImage = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.StretchToFill };
            previewImage.style.position = Position.Absolute;
            previewImage.style.left = previewImage.style.top = previewImage.style.right = previewImage.style.bottom = 0;
            view.Add(previewImage);
            var hint = new Label { name = "selection-hint" };
            hint.AddToClassList("hint");
            view.Add(hint);
            emptyHint = new Label("空の制作プロジェクト\n上部の「モデルを追加」または「基本形状を追加」から始めます") { pickingMode = PickingMode.Ignore };
            emptyHint.style.position = Position.Absolute;
            emptyHint.style.top = 110;
            emptyHint.style.left = 24;
            emptyHint.style.right = 24;
            emptyHint.style.fontSize = 22;
            emptyHint.style.unityTextAlign = TextAnchor.MiddleCenter;
            view.Add(emptyHint);

            var side = controls = new ScrollView { name = "authoring-controls" };
            side.style.width = 356;
            side.style.flexBasis = 356;
            side.style.flexGrow = 0;
            side.style.flexShrink = 0;
            side.style.paddingLeft = 14;
            side.style.paddingRight = 14;
            side.style.paddingTop = 10;
            side.style.backgroundColor = new Color(.11f, .15f, .19f);
            // The authoring pane scrolls vertically. A horizontal bar steals
            // the last row on compact/high-DPI windows and signals that a
            // control has escaped the pane; all long labels are wrapped.
            side.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            side.contentContainer.style.minWidth = 0;
            body.Add(side);

            BuildMcp(side);
            side.Add(new Label("1  制作プロジェクト"));
            side.Add(Button("新しい空プロジェクト", () => ConfirmReplace(() => ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null)), "new-project"));
            side.Add(Button("モデルを開く…", () =>
            {
                if (modelImportPanel != null) modelImportPanel.value = true;
                if (!modelPickerOpen) StartCoroutine(PickModel());
            }, "authoring-open-model"));
            side.Add(new Label("上部の「モデルを追加」または「基本形状を追加」から制作を始めます。"));
            var fixturesPanel = new Foldout { text = "開発者向け確認用fixture", value = false, name = "developer-fixtures" };
            fixturesPanel.Add(new Label("通常の制作には使いません。尺度・回帰確認用のプレートです。"));
            var fixtures = Row(fixturesPanel);
            fixtures.Add(Button("確認用プレート ×1", () => AddSample(1), "fixture-1"));
            fixtures.Add(Button("確認用プレート ×100", () => AddSample(100), "fixture-100"));
            side.Add(fixturesPanel);
            metrics = new Label { name = "authoring-metrics" };
            side.Add(metrics);
            BuildObjectSelection(side);
            BuildWorkModeNavigator(side);
            BuildAttachments(side);
            side.Add(Button("メッシュ全体を表示", Frame, "authoring-frame"));
            side.Add(new Label("2  形状を編集"));
            BuildGraphEditing(side);
            BuildModelImport(side);
            BuildFaceEditing(side);
            BuildSolidify(side);
            BuildUv(side);
            BuildPaint(side);
            BuildMaterials(side);
            BuildRig(side);
            BuildMorph(side);
            BuildValidation(side);
            selectionLabel = new Label { name = "authoring-selection" };
            side.Add(selectionLabel);
            var selectRow = Row(side);
            selectRow.Add(Button("すべて選択", () => SelectElements(true), "select-all"));
            selectRow.Add(Button("選択解除", () => SelectElements(false), "select-none"));
            var exactSelection = Row(side);
            vertexId = new IntegerField("頂点ID") { value = 0, name = "authoring-vertex-id" };
            vertexId.style.width = 170;
            vertexId.style.minHeight = 36;
            exactSelection.Add(vertexId);
            exactSelection.Add(Button("IDで選択", () => Try(() =>
            {
                if (vertexId.value < 0 || vertexId.value >= projection.Points.Length) throw new InvalidOperationException("頂点IDが範囲外です。");
                Select(new[] { vertexId.value });
            }), "authoring-select-id"));
            moveX = Number(side, "X / 横 (mm)", 10, "offset-x");
            moveY = Number(side, "Y / 上下 (mm)", 0, "offset-y");
            moveZ = Number(side, "Z / 奥行 (mm)", 0, "offset-z");
            moveButton = Button("選択頂点を移動", MoveSelection, "move-vertices");
            side.Add(moveButton);
            layer = new Toggle("編集レイヤーを表示") { value = true, name = "authoring-layer" };
            layer.RegisterValueChangedCallback(e => Execute(AuthoringOperation.SetLayerEnabled(e.newValue)));
            side.Add(layer);
            var history = Row(side);
            undoButton = Button("元に戻す", () => Execute(AuthoringOperation.Undo()), "authoring-undo");
            history.Add(undoButton);
            redoButton = Button("やり直す", () => Execute(AuthoringOperation.Redo()), "authoring-redo");
            history.Add(redoButton);
            BuildEvidenceCapture(side);
            BuildProjectOutputPanel(side);
            BuildProjectStatus(side);
            BindViewportInteraction(hint);
        }
    }
}
