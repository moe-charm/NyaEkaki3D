using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp : MonoBehaviour
    {
        public static ViewerApp Instance { get; private set; }
        public SessionDocument Document { get; private set; }
        public ActivePack Active { get; private set; }
        public bool IsBusy { get; private set; }
        public bool IsPlaying { get; private set; }
        public double TimeSeconds { get; private set; }
        public string Status { get; private set; } = "パックを開いてください";
        public string LastErrorCode { get; private set; } = "";
        public string CompatibilityToken { get; private set; }
        public string LibraryPath { get; private set; }
        public bool Dirty { get; private set; }
        string sessionPath, sessionHash;
        Camera previewCamera;
        Light keyLight;
        VisualElement uiRoot, partsPanel, morphPanel, viewport;
        TextField pathField, savePathField, searchField;
        Label statusLabel, timingLabel;
        Slider timeSlider;
        DropdownField clipDropdown;
        DropdownField modeDropdown, lightDropdown;
        Slider speedSlider;
        Toggle loopToggle;
        bool reflecting, focused = true;
        float nextUiUpdate;
        float renderAwakeUntil;
        Vector2 dragStart;
        int dragButton;
        bool dragging;
        public event Action StateChanged;

        void Awake()
        {
            Instance = this;
            Application.targetFrameRate = 60;
            // Finish in-flight local reload/recovery when the window loses focus.
            // Playback is paused by OnApplicationFocus; Update caps background at 5 fps.
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            CompatibilityToken = Resources.Load<TextAsset>("ViewerCompatibility")?.text.Trim();
            if (string.IsNullOrEmpty(CompatibilityToken)) throw new InvalidOperationException("Build compatibility metadata missing");
            LibraryPath = Arg("--library") ?? Path.Combine(Application.persistentDataPath, "Packs");
            InitializeSets();
            SetupScene(); SetupUi();
        }
        IEnumerator Start()
        {
            yield return null;
            var startup = Arg("--session") ?? Arg("--manifest");
            if (Arg("--startup-empty") != "true")
            {
                if (startup != null) OpenPath(startup);
                else OpenStartupState();
            }
            if (Arg("--smoke-output") != null) StartCoroutine(RunSmoke(Arg("--smoke-output")));
            if (Arg("--acceptance-output") != null) StartCoroutine(RunAcceptance(Arg("--acceptance-output")));
            if (Arg("--performance-output") != null) StartCoroutine(RunPerformance(Arg("--performance-output")));
            if (Arg("--revision-output") != null) StartCoroutine(RunRevisionAcceptance(Arg("--revision-output")));
            if (Arg("--visual-output") != null) StartCoroutine(RunVisualChecks(Arg("--visual-output")));
            if (Arg("--update-output") != null) StartCoroutine(RunUpdateNoticeCheck(Arg("--update-output")));
            if (Arg("--startup-output") != null) StartCoroutine(RunStartupProbe(Arg("--startup-output")));
            if (Arg("--sets-output") != null) StartCoroutine(RunSetsCheck(Arg("--sets-output")));
        }
        public static string Arg(string key)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++) if (args[i] == key) return args[i + 1];
            return null;
        }
        void SetupScene()
        {
            // Clear the entire window before the viewport camera. Foldouts can
            // move its pixelRect; without this, the old UI/viewport pixels remain.
            var background = new GameObject("WindowBackground").AddComponent<Camera>();
            background.depth = -100; background.cullingMask = 0;
            background.clearFlags = CameraClearFlags.SolidColor;
            background.backgroundColor = new Color(.095f, .12f, .16f);
            previewCamera = Camera.main;
            if (!previewCamera) { var go = new GameObject("PreviewCamera"); previewCamera = go.AddComponent<Camera>(); go.tag = "MainCamera"; }
            previewCamera.clearFlags = CameraClearFlags.SolidColor;
            previewCamera.backgroundColor = new Color(.095f, .12f, .16f);
            previewCamera.nearClipPlane = .015f; previewCamera.farClipPlane = 50;
            var light = new GameObject("StudioKey"); keyLight = light.AddComponent<Light>(); keyLight.type = LightType.Directional;
            keyLight.intensity = 1; light.transform.rotation = Quaternion.Euler(30, -30, 0);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.6f, .6f, .65f);
            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "StudioFloor"; floor.transform.localScale = Vector3.one * .6f;
            var floorMaterial = new Material(Shader.Find("Standard")); floorMaterial.color = new Color(.18f, .2f, .24f);
            floor.GetComponent<Renderer>().sharedMaterial = floorMaterial;
            Destroy(floor.GetComponent<Collider>());
            previewCamera.transform.position = new Vector3(0, .9f, -2.8f); previewCamera.transform.LookAt(new Vector3(0, .9f, 0));
        }
        void Update()
        {
            ReflectAvailability();
            Application.targetFrameRate = !focused ? 5 : IsPlaying || reloadLoop || dragging || UnityEngine.Time.unscaledTime < renderAwakeUntil ? 60 : 15;
            UnityEngine.Rendering.OnDemandRendering.renderFrameInterval = !focused && !reloadLoop && UnityEngine.Time.unscaledTime >= renderAwakeUntil ? 12 : 1;
            if (Document != null && Active?.Avatar != null && !IsBusy && focused)
            {
                if (IsPlaying)
                {
                    var c = Active.Verified.Manifest.clips.First(x => x.clipId == Document.motion.clipId);
                    TimeSeconds += UnityEngine.Time.unscaledDeltaTime * Document.motion.speed;
                    if (TimeSeconds > c.durationSeconds)
                    {
                        if (Document.motion.loop) TimeSeconds %= c.durationSeconds;
                        else { TimeSeconds = c.durationSeconds; Pause(); }
                    }
                    Active.Avatar.Apply(Document, TimeSeconds);
                }
                if (UnityEngine.Time.unscaledTime >= nextUiUpdate)
                {
                    nextUiUpdate = UnityEngine.Time.unscaledTime + .15f;
                    ReflectTransport();
                }
            }
        }
        void OnApplicationFocus(bool value)
        {
            bool wasPlaying = IsPlaying;
            focused = value;
            if (!value) Pause();
            Application.targetFrameRate = value ? 60 : 5;
            Debug.Log("VIEWER_FOCUS " + value + " wasPlaying=" + wasPlaying + " playing=" + IsPlaying + " time=" + TimeSeconds.ToString("R") + " cap=" + Application.targetFrameRate);
        }
        void OnDestroy() { Active?.Dispose(); if (Instance == this) Instance = null; }
        public void SetStatus(string value, string code = "")
        {
            WakeRendering();
            Status = value; LastErrorCode = code;
            if (statusLabel != null) statusLabel.text = (Dirty ? "● " : "") + value;
            Debug.Log("[Viewer] " + code + " " + value);
        }
        void WakeRendering() => renderAwakeUntil = UnityEngine.Time.unscaledTime + .5f;
        public void Error(Exception e)
        {
            var actual = e is AggregateException aggregate ? aggregate.GetBaseException() : e;
            SetStatus(actual.Message, actual is ContractException ce ? ce.Code : "UNEXPECTED_ERROR");
            Debug.LogException(actual);
        }
        public void Edit(Action<SessionDocument> change)
        {
            if (Document == null || IsBusy || Active?.Avatar == null) return;
            try
            {
                var candidate = Snapshot(); change(candidate); Validation.Session(candidate);
                Validation.Reconcile(candidate, Active.Verified.Manifest);
                Active.Avatar.Apply(candidate, candidate.motion.timeSeconds);
                Document = candidate; TimeSeconds = candidate.motion.timeSeconds; Dirty = true;
                ApplyCameraAndLight(); ReflectTransport(); SetStatus("調整しました"); StateChanged?.Invoke();
            }
            catch (Exception e) { Error(e); }
        }
        public SessionDocument Snapshot()
        {
            var snapshot = JsonFiles.Clone(Document); snapshot.motion.timeSeconds = TimeSeconds; return snapshot;
        }
        public void SetVisible(string id, bool visible)
        {
            Edit(s => s.visibilityOverrides = s.visibilityOverrides.Where(v => v.rendererId != id).Concat(new[] { new VisibilityOverride { rendererId = id, visible = visible } }).ToArray());
        }
        public void SetMorph(string id, string shape, float? weight)
        {
            // Controls may still deliver a queued event while the old avatar is
            // being unloaded, or while its snapshot is retained in Recovery.
            if (Document == null || IsBusy || Active?.Avatar == null) return;
            var b = Active.Verified.Manifest.morphBindings.FirstOrDefault(x => x.rendererId == id && x.shapeName == shape);
            if (b == null) { SetStatus("対象のシェイプキーがありません", "REQUIRED_BINDING_MISSING"); return; }
            if (weight.HasValue)
            {
                if (!Validation.Finite(weight.Value) || weight.Value < b.minWeight || weight.Value > b.maxWeight) { SetStatus("シェイプキー値が範囲外です", "MORPH_RANGE_INVALID"); return; }
            }
            Edit(s =>
            {
                var values = s.morphOverrides.Where(v => v.rendererId != id || v.shapeName != shape).ToList();
                if (weight.HasValue) values.Add(new MorphOverride { rendererId = id, shapeName = shape, weight = weight.Value });
                s.morphOverrides = values.ToArray();
            });
        }
        public void Pause()
        {
            IsPlaying = false;
            if (Document != null)
            {
                if (Document.motion.timeSeconds != TimeSeconds) Dirty = true;
                Document.motion.timeSeconds = TimeSeconds;
            }
            if (statusLabel != null) statusLabel.text = (Dirty ? "● " : "") + Status;
        }
        public void Seek(double seconds)
        {
            if (Active == null || Document == null) return;
            Pause();
            var clip = Active.Verified.Manifest.clips.First(c => c.clipId == Document.motion.clipId);
            Edit(s => s.motion.timeSeconds = Math.Max(0, Math.Min(seconds, clip.durationSeconds)));
            ReflectTransport();
        }
        public void Save(string path)
        {
            if (Document == null || IsBusy) return;
            try
            {
                path = Path.GetFullPath(path);
                var snapshot = Snapshot();
                // Runtime pack references are absolute, including the retained recovery
                // snapshot when neither the replacement nor the old bundle can load.
                string manifestPath = Active?.Verified.Path ?? Document.pack.manifestPath;
                snapshot.pack.manifestPath = Path.GetRelativePath(Path.GetDirectoryName(path), manifestPath).Replace('\\', '/');
                Validation.Session(snapshot);
                string expected = string.Equals(path, sessionPath, StringComparison.OrdinalIgnoreCase) ? sessionHash : null;
                string hash = JsonFiles.AtomicWrite(path, snapshot, expected);
                Document.motion.timeSeconds = snapshot.motion.timeSeconds;
                sessionPath = path; sessionHash = hash; Dirty = false;
                RefreshSetChoices();
                savePathField?.SetValueWithoutNotify(path);
                SetStatus(Path.IsPathRooted(snapshot.pack.manifestPath) ? "保存しました。パックは絶対パス参照です。別の場所へ移すときは参照先を確認してください。" : "保存しました");
            }
            catch (Exception e) { Error(e); }
        }
        public void OpenPath(string path)
        {
            CancelPendingUpdateCheck(); // Even a failed newer open cancels a stale pointer result.
            try
            {
                path = Path.GetFullPath(path.Trim().Trim('"'));
                if (Path.GetFileName(path).StartsWith("current.", StringComparison.Ordinal))
                {
                    OpenCurrentPointer(path);
                }
                else if (path.EndsWith(".viewer.json", StringComparison.OrdinalIgnoreCase))
                {
                    var session = JsonFiles.Read<SessionDocument>(path, out var loadedHash); Validation.Session(session);
                    string manifest = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path), session.pack.manifestPath));
                    RequestReload(manifest, session, session.pack.manifestSha256, path, loadedHash);
                }
                else RequestReload(path);
                pathField?.SetValueWithoutNotify(path);
            }
            catch (Exception e) { Error(e); }
        }
        public void CheckUpdate() => BeginUpdateCheck();
        void ApplyCameraAndLight()
        {
            if (Document == null) return;
            var c = Document.camera;
            var q = new Quaternion(c.orientationQuat[0], c.orientationQuat[1], c.orientationQuat[2], c.orientationQuat[3]).normalized;
            var target = new Vector3(c.targetMeters[0], c.targetMeters[1], c.targetMeters[2]);
            previewCamera.transform.rotation = q;
            previewCamera.transform.position = target - q * Vector3.forward * c.distanceMeters;
            previewCamera.fieldOfView = c.verticalFovDegrees;
            string lighting = Document.preview.lightPresetId;
            keyLight.intensity = lighting == "dark" ? .25f : lighting == "outdoor" ? 1.3f : 1;
            RenderSettings.ambientLight = lighting == "dark" ? new Color(.12f,.13f,.18f) : lighting == "outdoor" ? new Color(.75f,.8f,.9f) : new Color(.6f,.6f,.65f);
        }
        public void CameraPreset(string name)
        {
            Edit(s =>
            {
                // RadDollV3 source measurement: Neck origin is 0.9918 m high.
                if (name == "neck") { s.camera.targetMeters = new[] { 0f, .99f, 0f }; s.camera.distanceMeters = .65f; }
                else if (name == "all") FrameAvatar(s, Active.Avatar);
                else
                {
                    float yaw = name == "back" ? 0 : name == "left" ? 90 : name == "right" ? -90 : 180;
                    Quaternion q = Quaternion.Euler(0, yaw, 0); s.camera.orientationQuat = new[] { q.x, q.y, q.z, q.w };
                }
            });
        }
        void FrameAvatar(SessionDocument state, AvatarInstance avatar)
        {
            var visible = avatar.Renderers.Values.Where(r => r.enabled && r.gameObject.activeInHierarchy).ToArray();
            if (visible.Length == 0) return;
            var bounds = visible[0].bounds;
            foreach (var r in visible.Skip(1)) bounds.Encapsulate(r.bounds);
            state.camera.targetMeters = new[] { bounds.center.x, bounds.center.y, bounds.center.z };
            // Enclose the bounds for the selected orientation, leaving a margin.
            var q = new Quaternion(state.camera.orientationQuat[0],state.camera.orientationQuat[1],state.camera.orientationQuat[2],state.camera.orientationQuat[3]);
            var inverse = Quaternion.Inverse(q); Vector3 extents = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                var local = inverse * new Vector3((i & 1) == 0 ? -bounds.extents.x : bounds.extents.x, (i & 2) == 0 ? -bounds.extents.y : bounds.extents.y, (i & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                extents = Vector3.Max(extents, new Vector3(Mathf.Abs(local.x),Mathf.Abs(local.y),Mathf.Abs(local.z)));
            }
            float halfFov = Mathf.Tan(state.camera.verticalFovDegrees * Mathf.Deg2Rad * .5f);
            state.camera.distanceMeters = Mathf.Max(extents.y / halfFov, extents.x / (halfFov * Mathf.Max(.1f,previewCamera.aspect))) * 1.12f + extents.z;
        }
    }
}
