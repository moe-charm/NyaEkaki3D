using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        void SetupUi()
        {
            var settings = ScriptableObject.CreateInstance<PanelSettings>();
            // Match the logical Player resolution to the actual drawable
            // surface. ConstantPixelSize leaves a 1600px UI on a Windows
            // display whose DPI-scaled client surface is narrower, clipping
            // the authoring controls column when launched directly.
            settings.scaleMode = PanelScaleMode.ConstantPixelSize;
            settings.referenceResolution = new Vector2Int(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            // Unity reports the requested logical Player size while Windows
            // presents a DPI-scaled drawable surface. Normalize the panel to
            // the reference 96-DPI pixel grid so a 144-DPI 1080px window does
            // not clip its right-hand controls.
            // The headless authoring probes inject panel-space pointer events;
            // keep their 1:1 coordinate contract while the real Player uses
            // the Windows DPI factor for its drawable surface.
            bool injectedUiProbe = Environment.GetCommandLineArgs().Any(a => a == "--authoring-check-output" || a == "--navigation-check");
            settings.scale = injectedUiProbe ? 1f : Mathf.Clamp(Mathf.Max(96f, Screen.dpi) / 96f, 1f, 2f);
            settings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("ViewerTheme");
            var doc = gameObject.AddComponent<UIDocument>(); doc.panelSettings = settings;
            uiRoot = doc.rootVisualElement;
            uiRoot.style.position = Position.Absolute;
            uiRoot.style.left = 0; uiRoot.style.top = 0; uiRoot.style.right = 0; uiRoot.style.bottom = 0;
            uiRoot.style.color = new Color(.9f, .93f, .96f);
            uiRoot.style.fontSize = 17;
            var font = Font.CreateDynamicFontFromOSFont(new[] { "Yu Gothic", "Meiryo", "Arial" }, 16);
            uiRoot.style.unityFont = font;
            uiRoot.styleSheets.Add(Resources.Load<StyleSheet>("Viewer"));
            uiRoot.RegisterCallback<PointerDownEvent>(_ => WakeRendering(), TrickleDown.TrickleDown);
            uiRoot.RegisterCallback<WheelEvent>(_ => WakeRendering(), TrickleDown.TrickleDown);
            uiRoot.RegisterCallback<KeyDownEvent>(_ => WakeRendering(), TrickleDown.TrickleDown);

            var toolbar = new VisualElement { name = "toolbar" }; toolbar.AddToClassList("toolbar"); uiRoot.Add(toolbar);
            var brand = new Label("NyaForge"); brand.AddToClassList("brand"); toolbar.Add(brand);
            toolbar.Add(MakeButton("パックを開く…", BrowsePack, "browse-pack"));
            toolbar.Add(MakeButton("最近", () => ToggleNavigationPanel(recentPanel), "show-recent"));
            toolbar.Add(MakeButton("確認セット", () => ToggleNavigationPanel(setsPanel), "show-sets"));
            toolbar.Add(MakeButton("設定", () => ToggleNavigationPanel(settingsPanel), "show-settings"));
            activePackLabel = new Label("パックを開いて始める") { name = "active-pack-label" }; toolbar.Add(activePackLabel);
            toolbar.Add(MakeButton("制作へ", OpenAuthoring, "open-authoring"));

            recentPanel = MakeNavigationPanel("recent-panel"); BuildPackPicker(recentPanel);
            settingsPanel = MakeNavigationPanel("settings-panel");
            setsPanel = MakeNavigationPanel("sets-panel");

            BuildSetsUi();
            var maintenance = new VisualElement(); maintenance.AddToClassList("file-row"); settingsPanel.Add(maintenance);
            maintenance.Add(MakeButton("更新を確認", CheckUpdate, "check-update"));
            maintenance.Add(MakeButton("再読込", () => { if (Active != null) RequestReload(Active.Verified.Path); }, "reload"));
            var advanced = new Foldout { text = "詳細：パスを指定する", value = false, name = "advanced-files" }; settingsPanel.Add(advanced);
            var files = new VisualElement(); files.AddToClassList("file-row"); advanced.Add(files);
            pathField = new TextField { name = "open-path", value = System.IO.Path.Combine(LibraryPath, "current.StandaloneWindows64.json") };
            pathField.style.flexGrow = 1; files.Add(pathField);
            files.Add(MakeButton("パスを開く", () => AcceptPickedPath(pathField.value), "open"));
            savePathField = new TextField { name = "save-path", value = sessionPath };
            savePathField.style.flexGrow = 1; files.Add(savePathField);
            files.Add(MakeButton("別名保存", () => Save(savePathField.value), "save-as"));
            BuildUpdateUi(uiRoot);

            var body = new VisualElement(); body.AddToClassList("body"); uiRoot.Add(body);
            partsPanel = new ScrollView { name = "parts" }; partsPanel.AddToClassList("sidebar"); body.Add(partsPanel);
            viewport = new VisualElement { name = "viewport" }; viewport.style.flexGrow = 1; body.Add(viewport);
            var views = new VisualElement { name = "view-controls" }; viewport.Add(views);
            views.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            views.RegisterCallback<WheelEvent>(e => e.StopPropagation());
            views.Add(MakeButton("全体", () => CameraPreset("all"), "frame-all"));
            views.Add(MakeButton("首・肩", () => CameraPreset("neck"), "frame-neck"));
            foreach (var pair in new[] { ("正面", "front"), ("背面", "back"), ("左", "left"), ("右", "right") })
                views.Add(MakeButton(pair.Item1, () => CameraPreset(pair.Item2), "view-" + pair.Item2));
            var hint = new Label("ドラッグ: 回転  ·  右ドラッグ: 移動  ·  ホイール: 拡大"); hint.AddToClassList("hint"); viewport.Add(hint);
            var right = new VisualElement(); right.AddToClassList("sidebar"); right.style.width = 320; body.Add(right);
            right.Add(new Label("シェイプキー調整"));
            searchField = new TextField { name = "morph-search", value = "Shrink" };
            searchField.RegisterValueChangedCallback(_ => RebuildMorphs()); right.Add(searchField);
            morphPanel = new ScrollView(); morphPanel.style.flexGrow = 1; right.Add(morphPanel);
            var mode = modeDropdown = new DropdownField("表示", new System.Collections.Generic.List<string> { "original", "unlit", "bodyDiagnostic" }, 0);
            string ModeLabel(string v) => v == "unlit" ? "陰影なし" : v == "bodyDiagnostic" ? "身体を透過" : "元の材質";
            mode.formatListItemCallback = ModeLabel; mode.formatSelectedValueCallback = ModeLabel;
            mode.RegisterValueChangedCallback(e => Edit(s => s.preview.mode = e.newValue)); right.Add(mode);
            var light = lightDropdown = new DropdownField("照明", new System.Collections.Generic.List<string> { "studio", "outdoor", "dark" }, 0);
            string LightLabel(string v) => v == "outdoor" ? "屋外" : v == "dark" ? "暗め" : "スタジオ";
            light.formatListItemCallback = LightLabel; light.formatSelectedValueCallback = LightLabel;
            light.RegisterValueChangedCallback(e => Edit(s => s.preview.lightPresetId = e.newValue)); right.Add(light);

            var transport = new VisualElement(); transport.AddToClassList("transport"); uiRoot.Add(transport);
            clipDropdown = new DropdownField { name = "clip", label = "ポーズ" }; clipDropdown.style.width = 230; clipDropdown.style.flexShrink = 0;
            clipDropdown.RegisterValueChangedCallback(e =>
            {
                if (reflecting || Active == null) return;
                var clip = Active.Verified.Manifest.clips.FirstOrDefault(c => c.clipId == e.newValue);
                if (clip != null) { Pause(); Edit(s => { s.motion.clipId = clip.clipId; s.motion.timeSeconds = 0; }); ReflectTransport(); }
            });
            transport.Add(clipDropdown);
            transport.Add(MakeButton("再生／停止", () => { if (Active != null && !IsBusy) { if (IsPlaying) Pause(); else IsPlaying = true; } }, "play"));
            var playbackDetails = new Foldout { text = "再生の詳細", value = false }; settingsPanel.Add(playbackDetails);
            var playbackRow = new VisualElement(); playbackRow.AddToClassList("file-row"); playbackDetails.Add(playbackRow);
            playbackRow.Add(MakeButton("◀ 1", () => StepFrame(-1), "step-back"));
            playbackRow.Add(MakeButton("1 ▶", () => StepFrame(1), "step-forward"));
            var speed = speedSlider = new Slider("速度", .1f, 2f) { value = 1, showInputField = true }; speed.style.width = 190;
            speed.RegisterValueChangedCallback(e => Edit(s => s.motion.speed = e.newValue)); playbackRow.Add(speed);
            var loop = loopToggle = new Toggle("ループ") { value = true }; loop.RegisterValueChangedCallback(e => Edit(s => s.motion.loop = e.newValue)); playbackRow.Add(loop);
            timeSlider = new Slider { name = "time", lowValue = 0, highValue = 2, showInputField = true }; timeSlider.style.flexGrow = 1;
            timeSlider.RegisterValueChangedCallback(e => { if (!reflecting) Seek(e.newValue); }); transport.Add(timeSlider);
            timingLabel = new Label("0.000 s"); timingLabel.style.width = 105; transport.Add(timingLabel);
            statusLabel = new Label(Status); statusLabel.AddToClassList("status"); uiRoot.Add(statusLabel);

            viewport.RegisterCallback<GeometryChangedEvent>(_ => UpdateViewport());
            viewport.RegisterCallback<PointerDownEvent>(e =>
            {
                if (Document == null || IsBusy) return;
                dragging = true; dragButton = e.button; dragStart = e.position;
                viewport.CapturePointer(e.pointerId); e.StopPropagation();
            });
            viewport.RegisterCallback<PointerUpEvent>(e => { dragging = false; viewport.ReleasePointer(e.pointerId); });
            viewport.RegisterCallback<PointerCaptureOutEvent>(_ => dragging = false);
            viewport.RegisterCallback<PointerMoveEvent>(e =>
            {
                if (!dragging || Document == null || IsBusy) return;
                Vector2 delta = (Vector2)e.position - dragStart; dragStart = e.position;
                Edit(s => {
                var c = s.camera;
                var q = new Quaternion(c.orientationQuat[0], c.orientationQuat[1], c.orientationQuat[2], c.orientationQuat[3]);
                if (dragButton == 0)
                {
                    var rotated = Quaternion.AngleAxis(delta.x * .3f, Vector3.up) * q * Quaternion.AngleAxis(delta.y * .3f, Vector3.right);
                    c.orientationQuat = new[] { rotated.x, rotated.y, rotated.z, rotated.w };
                }
                else
                {
                    Vector3 shift = q * new Vector3(-delta.x, delta.y, 0) * c.distanceMeters * .0012f;
                    c.targetMeters[0] += shift.x; c.targetMeters[1] += shift.y; c.targetMeters[2] += shift.z;
                }
                });
            });
            viewport.RegisterCallback<WheelEvent>(e =>
            {
                if (Document == null || IsBusy) return;
                Edit(s => s.camera.distanceMeters = Mathf.Clamp(s.camera.distanceMeters * Mathf.Exp(e.delta.y * .045f), .08f, 20));
                e.StopPropagation();
            });
        }
        static Button MakeButton(string text, Action action, string id) => new Button(action) { text = text, name = id };
        void UpdateViewport()
        {
            var rect = viewport.worldBound;
            float sx = Screen.width / Math.Max(1, uiRoot.resolvedStyle.width);
            float sy = Screen.height / Math.Max(1, uiRoot.resolvedStyle.height);
            previewCamera.pixelRect = new Rect(rect.x * sx, Screen.height - rect.yMax * sy, Math.Max(1, rect.width * sx), Math.Max(1, rect.height * sy));
        }
        void StepFrame(int direction)
        {
            if (Document == null || Active == null) return;
            double rate = Active.Verified.Manifest.clips.First(c => c.clipId == Document.motion.clipId).sampleRate;
            Seek(TimeSeconds + direction / rate);
        }
        void RebuildControls()
        {
            partsPanel.Clear(); partsPanel.Add(new Label("パーツ表示"));
            partsPanel.Add(MakeButton("身体だけ表示", () => SetBodyVisibility(true), "body-only"));
            partsPanel.Add(MakeButton("パック既定の表示に戻す", () => SetBodyVisibility(false), "visibility-default"));
            foreach (var row in Active.Verified.Manifest.renderers.OrderBy(r => r.category).ThenBy(r => r.displayName))
            {
                bool visible = Document.visibilityOverrides.FirstOrDefault(v => v.rendererId == row.rendererId)?.visible ?? row.defaultVisible;
                var toggle = new Toggle(row.displayName) { value = visible, name = "part-" + row.rendererId };
                toggle.RegisterValueChangedCallback(e => SetVisible(row.rendererId, e.newValue)); partsPanel.Add(toggle);
            }
            reflecting = true;
            clipDropdown.choices = Active.Verified.Manifest.clips.Select(c => c.clipId).ToList();
            string ClipLabel(string id)
            {
                if (Active == null) return id;
                var clip = Active.Verified.Manifest.clips.FirstOrDefault(c => c.clipId == id);
                if (clip == null) return id;
                return Active.Verified.Manifest.clips.Count(c => c.displayName == clip.displayName) > 1 ? clip.displayName + " (" + id + ")" : clip.displayName;
            }
            clipDropdown.formatListItemCallback = ClipLabel;
            clipDropdown.formatSelectedValueCallback = ClipLabel;
            clipDropdown.SetValueWithoutNotify(Document.motion.clipId);
            modeDropdown.SetValueWithoutNotify(Document.preview.mode);
            lightDropdown.SetValueWithoutNotify(Document.preview.lightPresetId);
            speedSlider.SetValueWithoutNotify((float)Document.motion.speed);
            loopToggle.SetValueWithoutNotify(Document.motion.loop);
            reflecting = false;
            savePathField.SetValueWithoutNotify(sessionPath);
            RebuildMorphs(); ReflectTransport();
        }
        void RebuildMorphs()
        {
            morphPanel?.Clear();
            if (Active == null || Document == null) return;
            string query = searchField.value ?? "";
            foreach (var b in Active.Verified.Manifest.morphBindings.Where(b => b.shapeName.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
            {
                var box = new VisualElement(); box.AddToClassList("morph-box");
                var label = new Label(b.shapeName); label.tooltip = b.rendererId; box.Add(label);
                var row = new VisualElement(); row.style.flexDirection = FlexDirection.Row;
                var current = Document.morphOverrides.FirstOrDefault(x => x.rendererId == b.rendererId && x.shapeName == b.shapeName);
                var slider = new Slider(b.minWeight, b.maxWeight) { value = current?.weight ?? b.defaultWeight, showInputField = true };
                slider.style.flexGrow = 1;
                slider.RegisterValueChangedCallback(e => SetMorph(b.rendererId, b.shapeName, e.newValue)); row.Add(slider);
                row.Add(MakeButton("自動", () => { if (IsBusy || Active?.Avatar == null) return; SetMorph(b.rendererId, b.shapeName, null); slider.SetValueWithoutNotify(Active.Avatar.MorphValue(b.rendererId, b.shapeName)); }, "reset-" + b.shapeName));
                box.Add(row); morphPanel.Add(box);
            }
            foreach (var unresolved in Document.unresolvedOverrides) morphPanel.Add(new Label("保留: " + unresolved.shapeName));
        }
        void ReflectTransport()
        {
            if (Document == null || Active == null) return;
            foreach (var row in Active.Verified.Manifest.renderers)
                partsPanel.Q<Toggle>("part-" + row.rendererId)?.SetValueWithoutNotify(Document.visibilityOverrides.FirstOrDefault(v => v.rendererId == row.rendererId)?.visible ?? row.defaultVisible);
            reflecting = true;
            clipDropdown.SetValueWithoutNotify(Document.motion.clipId);
            modeDropdown.SetValueWithoutNotify(Document.preview.mode);
            lightDropdown.SetValueWithoutNotify(Document.preview.lightPresetId);
            speedSlider.SetValueWithoutNotify((float)Document.motion.speed);
            loopToggle.SetValueWithoutNotify(Document.motion.loop);
            timeSlider.highValue = (float)Active.Verified.Manifest.clips.First(c => c.clipId == Document.motion.clipId).durationSeconds;
            timeSlider.SetValueWithoutNotify((float)TimeSeconds);
            timingLabel.text = TimeSeconds.ToString("F3") + " s";
            reflecting = false;
        }
        void ReflectAvailability()
        {
            ReflectNavigation();
            ReflectSetsUi();
            bool ready = !IsBusy && Active?.Avatar != null;
            partsPanel?.SetEnabled(ready); morphPanel?.SetEnabled(ready);
            clipDropdown?.SetEnabled(ready); timeSlider?.SetEnabled(ready);
            speedSlider?.SetEnabled(ready); loopToggle?.SetEnabled(ready);
            modeDropdown?.SetEnabled(ready); lightDropdown?.SetEnabled(ready);
            uiRoot?.Q<Button>("play")?.SetEnabled(ready);
            uiRoot?.Q<Button>("step-back")?.SetEnabled(ready);
            uiRoot?.Q<Button>("step-forward")?.SetEnabled(ready);
        }
    }
}
