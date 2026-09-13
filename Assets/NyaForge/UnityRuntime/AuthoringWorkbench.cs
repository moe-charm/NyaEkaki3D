using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    /// <summary>The first authoring slice: a public static fixture using the same commands as automation.</summary>
    public sealed partial class AuthoringWorkbench : MonoBehaviour
    {
        VisualElement root, view, confirmRow;
        Label status, metrics, selectionLabel, projectLabel, emptyHint;
        TextField projectPath;
        TextField vrmName, vrmAuthors, vrmLicenseUrl;
        IntegerField vertexId;
        ScrollView controls;
        GraphCanvas graphCanvas;
        Image previewImage;
        RenderTexture previewTexture;
        FloatField moveX, moveY, moveZ;
        Toggle layer;
        Button undoButton, redoButton, moveButton;
        Action close, reopen;
        Camera camera;
        GameObject stage;
        OwnedMeshProjection projection;
        MultiObjectProjection objectProjection;
        AuthoringWorkspace workspace;
        AuthoringCommandService commands;
        string savedDirectory;
        readonly HashSet<int> selection = new HashSet<int>();
        bool active, orbiting, allowQuit;
        int orbitButton;
        Vector2 pointerStart, pointerLast;
        Vector3 target;
        Quaternion orbit = Quaternion.Euler(0, 180, 0);
        float distance = .24f;

        bool HasUnsaved => workspace != null && (workspace.IsDirty || saveIncomplete);
        public bool IsOpen => active;

        void Awake() { Application.wantsToQuit += WantsToQuit; }

        public void Open(VisualElement parent, Action closeAction, Action reopenAction)
        {
            close = closeAction; reopen = reopenAction;
            if (root == null)
            {
                BuildUi(parent);
                stage = new GameObject("NyaForge authoring preview");
                AuthoringPreviewLights.Create(stage.transform);
                camera = new GameObject("Authoring camera").AddComponent<Camera>();
                camera.transform.SetParent(stage.transform, false);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.055f, .08f, .11f);
                camera.cullingMask = 1 << OwnedMeshProjection.PreviewLayer;
                camera.nearClipPlane = .001f;
                camera.farClipPlane = 30;
                camera.fieldOfView = 35;
                camera.depth = 10;
                projection = new OwnedMeshProjection(stage.transform);
                objectProjection = new MultiObjectProjection(stage.transform);
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            }
            active = true;
            root.style.display = DisplayStyle.Flex;
            stage.SetActive(true);
            UpdateCamera();
        }

        void BuildUi(VisualElement parent)
        {
            root = new VisualElement { name = "authoring-workbench" };
            root.style.flexGrow = 1;
            root.style.minHeight = 0;
            parent.Add(root);
            var top = Row(root);
            top.style.backgroundColor = new Color(.09f, .14f, .18f);
            top.Add(new Label("NyaForge  /  制作プレビュー") { name = "authoring-title" });
            top.Add(Button("ビューワーに戻る", () =>
            {
                uvPreview?.CancelIslandDrag();paintCanvas?.CancelStroke();ClearSurfacePreparation(); active = false; root.style.display = DisplayStyle.None; stage.SetActive(false); close?.Invoke();
            }, "authoring-close"));
            top.Add(Button("ノード表示 / 非表示", () => { graphCanvas.style.display = graphCanvas.resolvedStyle.display == DisplayStyle.None ? DisplayStyle.Flex : DisplayStyle.None; }, "graph-toggle"));
            var body = new VisualElement();
            body.style.flexDirection = FlexDirection.Row;
            body.style.flexGrow = 1; body.style.flexBasis = 0; body.style.minHeight = 0;
            root.Add(body);
            var left = new VisualElement(); left.style.flexGrow = 1; left.style.minWidth = 150; left.style.minHeight = 0; body.Add(left);
            view = new VisualElement { name = "authoring-viewport" };
            view.style.flexGrow = 1; view.style.minWidth = 150;
            view.style.minHeight = 100; left.Add(view);
            graphCanvas = new GraphCanvas(); graphCanvas.style.display = DisplayStyle.None; left.Add(graphCanvas);
            previewImage = new Image { pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.StretchToFill };
            previewImage.style.position = Position.Absolute;
            previewImage.style.left = previewImage.style.top = previewImage.style.right = previewImage.style.bottom = 0;
            view.Add(previewImage);
            var hint = new Label { name = "selection-hint" };
            hint.AddToClassList("hint"); view.Add(hint);
            emptyHint = new Label("空のプロジェクト\n右の「プレート追加」から形を作れます") { pickingMode = PickingMode.Ignore };
            emptyHint.style.position = Position.Absolute; emptyHint.style.top = 110;
            emptyHint.style.left = 24; emptyHint.style.right = 24; emptyHint.style.fontSize = 22;
            emptyHint.style.unityTextAlign = TextAnchor.MiddleCenter; view.Add(emptyHint);
            var side = controls = new ScrollView { name = "authoring-controls" };
            side.style.width = 356; side.style.flexShrink = 0;
            side.style.paddingLeft = 14; side.style.paddingRight = 14; side.style.paddingTop = 10;
            side.style.backgroundColor = new Color(.11f, .15f, .19f);
            body.Add(side);
            BuildMcp(side);
            side.Add(new Label("1  制作プロジェクト"));
            side.Add(Button("新しい空プロジェクト", () => ConfirmReplace(() => ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null)), "new-project"));
            side.Add(new Label("プレートを追加するか、下の「Planeグラフから始める」で形を作れます。"));
            var fixtures = Row(side);
            fixtures.Add(Button("プレート追加 ×1", () => AddSample(1), "fixture-1"));
            fixtures.Add(Button("プレート追加 ×100", () => AddSample(100), "fixture-100"));
            metrics = new Label { name = "authoring-metrics" }; side.Add(metrics);
            BuildObjectSelection(side);
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
            selectionLabel = new Label { name = "authoring-selection" }; side.Add(selectionLabel);
            var selectRow = Row(side);
            selectRow.Add(Button("すべて選択", () => SelectElements(true), "select-all"));
            selectRow.Add(Button("選択解除", () => SelectElements(false), "select-none"));
            var exactSelection = Row(side);
            vertexId = new IntegerField("頂点ID") { value = 0, name = "authoring-vertex-id" };
            vertexId.style.width = 170; vertexId.style.minHeight = 36; exactSelection.Add(vertexId);
            exactSelection.Add(Button("IDで選択", () => Try(() =>
            {
                if (vertexId.value < 0 || vertexId.value >= projection.Points.Length) throw new InvalidOperationException("頂点IDが範囲外です。");
                Select(new[] { vertexId.value });
            }), "authoring-select-id"));
            moveX = Number(side, "X / 横 (mm)", 10, "offset-x");
            moveY = Number(side, "Y / 上下 (mm)", 0, "offset-y");
            moveZ = Number(side, "Z / 奥行 (mm)", 0, "offset-z");
            moveButton = Button("選択頂点を移動", MoveSelection, "move-vertices"); side.Add(moveButton);
            layer = new Toggle("編集レイヤーを表示") { value = true, name = "authoring-layer" };
            layer.RegisterValueChangedCallback(e => Execute(AuthoringOperation.SetLayerEnabled(e.newValue)));
            side.Add(layer);
            var history = Row(side);
            undoButton = Button("元に戻す", () => Execute(AuthoringOperation.Undo()), "authoring-undo"); history.Add(undoButton);
            redoButton = Button("やり直す", () => Execute(AuthoringOperation.Redo()), "authoring-redo"); history.Add(redoButton);
            BuildEvidenceCapture(side);
            side.Add(new Label("3  保存とUnityへの受け渡し"));
            projectLabel = new Label(); side.Add(projectLabel);
            projectPath = new TextField("制作フォルダ") { name = "authoring-project-path" };
            projectPath.style.flexDirection = FlexDirection.Column;
            projectPath.tooltip = "project.nyaforge.json と blobs を保存するフォルダ。開く場合もフォルダを指定します。";
            side.Add(projectPath);
            var files = Row(side);
            files.Add(Button("保存", SaveProject, "authoring-save"));
            files.Add(Button("開く", () => ConfirmReplace(OpenProject), "authoring-open"));
            files.Add(Button("Explorerで選ぶ…", BrowseProject, "authoring-project-browse"));
            side.Add(Button("Unity用に書き出す", Export, "authoring-export"));
            side.Add(Button("標準GLB（表示形状）", ExportGlbStatic, "authoring-export-glb-static"));
            side.Add(Button("標準GLB（skin/morph保持）", ExportGlbSkinned, "authoring-export-glb-skinned"));
            side.Add(Button("拡張GLB（全weight保持）", ExportGlbSkinnedExtended, "authoring-export-glb-skinned-extended"));
            side.Add(new Label("Unity用出力はnative機能を保持します。装着情報を含む作品はnative projectへ出力されます。標準GLBは互換用4 influence、拡張GLBは全weightを出力します。"));
            var vrmMetadata = new Foldout { text = "VRM 1.0 metadata", value = false, name = "authoring-vrm-metadata" };
            vrmName = new TextField("名前") { value = "NyaForge Avatar", name = "authoring-vrm-name" }; vrmMetadata.Add(vrmName);
            vrmAuthors = new TextField("作者（カンマ区切り）") { value = "NyaForge", name = "authoring-vrm-authors" }; vrmMetadata.Add(vrmAuthors);
            vrmLicenseUrl = new TextField("license URL") { value = "", name = "authoring-vrm-license-url" }; vrmMetadata.Add(vrmLicenseUrl);
            vrmMetadata.Add(new Label("VRM 1.0のhumanoid必須骨を検査します。license URLは作品の利用条件を指すURLを入力してください。表情・LookAt・SpringBoneはこの初期profileでは出力しません。"));
            side.Add(vrmMetadata);
            side.Add(Button("VRM 1.0（humanoid）", ExportVrm1, "authoring-export-vrm1"));
            confirmRow = new VisualElement { name = "authoring-confirm" }; confirmRow.style.display = DisplayStyle.None; side.Add(confirmRow);
            status = new Label { name = "authoring-status" }; status.AddToClassList("status");
            // Keep the footer from growing when a long diagnostic is reported on a
            // narrow window. A changing footer height changes the viewport layout
            // and can invalidate an in-progress camera/paint interaction.
            status.style.whiteSpace = WhiteSpace.NoWrap;
            status.style.overflow = Overflow.Hidden;
            status.style.minHeight = 34;
            root.Add(status);

            view.RegisterCallback<GeometryChangedEvent>(_ => UpdateCamera());
            view.RegisterCallback<PointerDownEvent>(e =>
            {
                if (!active || e.button > 1) return;
                if (RigWeightPaintActive && e.button == 0) { BeginRigWeightStroke(e.position); view.CapturePointer(e.pointerId); e.StopPropagation(); return; }
                if(CutPathPicking) view.Focus();
                orbiting = true; orbitButton = e.button; pointerStart = pointerLast = e.position;
                view.CapturePointer(e.pointerId); e.StopPropagation();
            });
            view.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (RigWeightPaintActive && rigPainting) { UpdateRigWeightStroke(e.position); e.StopPropagation(); return; }
                if (!orbiting) return;
                var delta = (Vector2)e.position - pointerLast;
                pointerLast = e.position;
                if (orbitButton == 0) orbit = Quaternion.AngleAxis(delta.x * .3f, Vector3.up) * orbit * Quaternion.AngleAxis(delta.y * .3f, Vector3.right);
                else target += orbit * new Vector3(-delta.x, delta.y, 0) * distance * .0012f;
                UpdateCamera();
            });
            view.RegisterCallback<PointerUpEvent>(e =>
            {
                if (RigWeightPaintActive && rigPainting && e.button == 0) { EndRigWeightStroke(); view.ReleasePointer(e.pointerId); e.StopPropagation(); return; }
                if (orbiting && orbitButton == 0 && Vector2.Distance(pointerStart, e.position) < 4)
                { if(CutPathPicking) PickCutPathPoint(e.position);else PickVertex(e.position,e.shiftKey); }
                orbiting = false; view.ReleasePointer(e.pointerId);
            });
            view.RegisterCallback<PointerCaptureOutEvent>(_ => { orbiting = false; if (rigPainting) EndRigWeightStroke(); });
            view.RegisterCallback<WheelEvent>(e =>
            {
                distance = Mathf.Clamp(distance * Mathf.Exp(e.delta.y * .045f), .02f, 20f); UpdateCamera(); e.StopPropagation();
            });
        }

        static VisualElement Row(VisualElement parent)
        {
            var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row; row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center; row.style.marginTop = 6; row.style.marginBottom = 6;
            parent.Add(row); return row;
        }
        static Button Button(string text, Action action, string name) => new Button(action) { text = text, name = name };
        static FloatField Number(VisualElement parent, string label, float value, string name)
        {
            var field = new FloatField(label) { value = value, name = name };
            field.style.minHeight = 36; field.style.marginTop = 4; parent.Add(field); return field;
        }

        void MoveSelection()
        {
            var ids = selection.OrderBy(i => i).ToArray();
            var delta = new Vec3(moveX.value / 1000, moveY.value / 1000, moveZ.value / 1000);
            if (IsGraph)
            {
                if (activeEditContext == null) { SetStatus("編集可能なEditMeshを選択してください。"); return; }
                var value = DisplayedGraphValue();
                if (value?.Polygon != null)
                {
                    var stableIds = SelectedPolygonVertices();
                    Execute(AuthoringOperation.TranslatePolygonVertices(activeEditContext, stableIds, delta));
                }
                else Execute(AuthoringOperation.TranslateGraphVertices(activeEditContext, ids, delta));
            }
            else Execute(AuthoringOperation.TranslateVertices(ids, delta));
        }

        void Execute(params AuthoringOperation[] operations)
        {
            Try(() =>
            {
                ClearSpringPlayback(true);
                var result = ExecuteMeasured(operations);
                if (!result.Success) { Refresh(); throw new InvalidOperationException(result.Code + ": " + result.Message); }
                if (operations.Length == 1 && (operations[0].Kind == "history.undo" || operations[0].Kind == "history.redo"))
                    RefreshSecondaryMotionAttachmentFromWorkspace();
                var guiWatch=measureCommands ? System.Diagnostics.Stopwatch.StartNew() : null;
                selection.RemoveWhere(i => i < 0 || i >= projection.Points.Length);
                projection.Select(selection); Refresh();
                SetStatus(result.EvaluationComplete ? "編集を反映しました。元に戻す・やり直すで確認できます。" : "編集を保存可能な状態で保持しました。接続またはノードの診断を確認してください。");
                if(guiWatch!=null) commandMeasurement.guiMs=guiWatch.Elapsed.TotalMilliseconds;
            });
        }

        void Select(IEnumerable<int> indices)
        {
            selection.Clear(); foreach (int i in indices) selection.Add(i);
            projection.Select(selection); Refresh();
        }

        void PickVertex(Vector2 panelPosition, bool add)
        {
            if (faceMode.value && faceMode.enabledSelf) { PickFace(panelPosition, add); return; }
            int closest = -1; float best = 18;
            // Projection.Points are local to the preview root. Hit testing must use
            // the world coordinates after a rigid accessory attachment is applied.
            var points = projection.WorldPoints;
            for (int i = 0; i < points.Length; i++)
            {
                var projected = camera.WorldToViewportPoint(points[i]);
                var d = Vector2.Distance(panelPosition, VertexPanelPoint(points[i]));
                if (projected.z > 0 && d < best) { closest = i; best = d; }
            }
            if (closest < 0) return;
            if (!add) selection.Clear();
            if (add && selection.Contains(closest)) selection.Remove(closest); else selection.Add(closest);
            projection.Select(selection); Refresh();
        }

        Vector2 VertexPanelPoint(Vector3 point)
        {
            var projected = camera.WorldToViewportPoint(point);
            var rect = view.worldBound;
            return new Vector2(rect.x + projected.x * rect.width, rect.y + (1 - projected.y) * rect.height);
        }

        void Refresh()
        {
            if (workspace == null) return;
            if (springPlayback != null && (workspace != springWorkspace || workspace.Document.StateHash != springDocumentHash || workspace.Attachments.ContentHash != springMetadataHash)) ClearSpringPlayback(true);
            RefreshGraphEditing();
            RefreshObjectSelection();
            objectProjection?.Refresh(workspace.Document, ResolveAttachmentPoseForObject);
            RefreshAttachmentControls();
            RefreshSourceSkinDisplayProjection();
            RefreshAttachmentProjection();
            graphCanvas.Bind(workspace, operations => Execute(operations), node => Try(() => SelectEditStage(editStageIds.IndexOf(node))));
            var displayed = DisplayedGraphValue();
            var data = displayed?.Mesh;
            bool staticProfile = !workspace.Document.IsEmpty && workspace.Document.ActiveObject.IsStaticProfile;
            emptyHint.style.display = data == null ? DisplayStyle.Flex : DisplayStyle.None;
            emptyHint.text = displayed?.Polygon?.Faces.Count==0 ? "点を置いて、最初の面を作れます" : workspace.Document.IsEmpty ? "空のプロジェクト\n右の「プレート追加」から形を作れます" : "グラフの評価が未完了です\n接続とノードを確認してください";
            metrics.text = displayed?.Polygon?.Faces.Count==0 ? displayed.Polygon.Vertices.Count+" 編集点 / 面なし" : data == null ? (workspace.Document.IsEmpty ? "空のプロジェクト — 形を追加して始める" : "評価未完了 — 表示できる結果がありません") : data.VertexCount + " 頂点 / " + data.TriangleCount + " △\n単位: m  ·  元scale: " + displayed.Transform.Scale;
            if (!workspace.Preview.IsComplete) metrics.text += "\n最終評価未完了 / 文書 rev " + workspace.Document.DocumentRevision + (projection.PreviewNodeId == "" && workspace.Preview.IsStale ? " / 表示 rev " + workspace.Preview.OutputRevision : "");
            root.Q<Button>("fixture-1").SetEnabled(workspace.Document.IsEmpty);
            root.Q<Button>("fixture-100").SetEnabled(workspace.Document.IsEmpty);
            layer.SetEnabled(staticProfile);
            selectionLabel.text = selection.Count == 0 ? "点をクリックして選んでください" : "選択: " + selection.Count + " 頂点  [" + string.Join(", ", selection.OrderBy(i => i).Take(12)) + "]";
            moveButton.SetEnabled((staticProfile || activeEditContext != null) && selection.Count > 0);
            undoButton.SetEnabled(workspace.CanUndo); redoButton.SetEnabled(workspace.CanRedo);
            layer.SetValueWithoutNotify(staticProfile && workspace.Document.LayerEnabled);
            projectLabel.text = HasUnsaved ? "● 未保存の変更があります" : "保存済み";
            RefreshFaceEditing();
            RefreshSolidify();
            RefreshUv();
            RefreshPaint();
            RefreshMaterials();RefreshRig();RefreshMorph();RefreshEvidenceCapture();
            RefreshModelImport();
            RefreshPhysBonesStatus();
            RefreshSecondaryMotionStatus();
            RefreshSpringPlayback();
            RefreshValidation();
        }

        void Frame()
        {
            var points = projection.FramingPoints.Concat(objectProjection?.FramingPoints ?? Enumerable.Empty<Vector3>()).ToArray();
            if (points.Length == 0) { target=Vector3.zero;distance=.5f;orbit=Quaternion.Euler(0,180,0);UpdateCamera();return; }
            var bounds = new Bounds(points[0], Vector3.zero);
            foreach (var p in points) bounds.Encapsulate(p);
            target = bounds.center;
            distance = Mathf.Max(.1f, bounds.size.magnitude * 2.4f);
            orbit = Quaternion.Euler(0, 180, 0); UpdateCamera();
        }

        void UpdateCamera()
        {
            ClearCutPathHover();
            if (!camera || view?.panel == null) return;
            CancelSurfaceStroke();
            var panel = root.panel.visualTree;
            float sx = Screen.width / Math.Max(1, panel.resolvedStyle.width);
            float sy = Screen.height / Math.Max(1, panel.resolvedStyle.height);
            var rect = view.worldBound;
            int width = Mathf.Clamp(Mathf.RoundToInt(rect.width * sx), 1, 4096);
            int height = Mathf.Clamp(Mathf.RoundToInt(rect.height * sy), 1, 4096);
            if (!previewTexture || previewTexture.width != width || previewTexture.height != height)
            {
                camera.targetTexture = null;
                if (previewTexture) { previewTexture.Release(); Destroy(previewTexture); }
                previewTexture = new RenderTexture(width, height, 24) { name = "NyaForge authoring viewport" };
                previewTexture.Create(); camera.targetTexture = previewTexture;
                previewImage.image = previewTexture;
            }
            camera.transform.position = target + orbit * new Vector3(0, 0, -distance);
            camera.transform.rotation = orbit;
            RefreshSurfacePreparation();
        }

        void Try(Action action)
        {
            try { action(); }
            catch (Exception error) { SetStatus(error.Message); Debug.LogWarning("[NyaForge authoring] " + error); }
        }
        void SetStatus(string text)
        {
            status.text = text ?? "";
            // The footer stays one line on compact windows; keep the complete
            // diagnostic available through the native tooltip for inspection.
            status.tooltip = status.text;
            Debug.Log("[NyaForge authoring] " + status.text);
        }
        bool WantsToQuit()
        {
            if (allowQuit || !HasUnsaved) return true;
            if (!active) reopen?.Invoke();
            confirmRow.Clear(); confirmRow.style.display = DisplayStyle.Flex;
            confirmRow.Add(new Label("制作データに未保存の変更があります。終了方法を選んでください。"));
            confirmRow.Add(Button("保存して終了", () => { if (TrySaveForExit()) { allowQuit = true; Application.Quit(); } }, "authoring-save-quit"));
            confirmRow.Add(Button("変更を破棄して終了", () => { allowQuit = true; Application.Quit(); }, "authoring-discard-quit"));
            confirmRow.Add(Button("キャンセル", () => confirmRow.style.display = DisplayStyle.None, "authoring-cancel-quit"));
            controls.schedule.Execute(() => controls.ScrollTo(confirmRow));
            return false;
        }
        void OnDestroy()
        {
            StopMcp();
            Application.wantsToQuit -= WantsToQuit;
            surfacePreparationWatch?.Pause();surfacePreparation.Dispose();
            CancelSurfaceStroke();
            projection?.Dispose();
            objectProjection?.Dispose();
            boundaryHighlight?.Dispose();
            bridgeHighlight?.Dispose();
            faceCreateOutline?.Dispose();edgeCutLine?.Dispose();cutPathLine?.Dispose();cutHoverEdge?.Dispose();
            paintCanvas?.Dispose();
            if (stage != null) Destroy(stage);
            if (previewTexture) { previewTexture.Release(); Destroy(previewTexture); }
        }
    }
}








