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
    public sealed partial class ViewerApp
    {
        IEnumerator RunSetsCheck(string output)
        {
            var checks = new List<string>(); string failure = null;
            void Check(bool condition, string text) { if (!condition) throw new Exception(text); checks.Add(text); }
            double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 60;
            while ((Active?.Avatar == null || reloadLoop) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            try
            {
                Directory.CreateDirectory(output);
                Check(Arg("--settings-root") != null && settingsRoot != Application.persistentDataPath, "isolated settings required");
                Check(Active?.Avatar != null && !reloadLoop, "player ready");
                Check(Document.visibilityOverrides.Length == 0 && preferences.startupMode == "pack", "default startup ignores legacy last session");
                var motion = JsonFiles.Encode(Document.motion); var camera = JsonFiles.Encode(Document.camera);
                SetBodyVisibility(true);
                Check(Document.visibilityOverrides.Length == Active.Verified.Manifest.renderers.Length && Active.Verified.Manifest.renderers.All(r => Active.Avatar.Renderers[r.rendererId].enabled == (r.category == "body")), "body only applies to actual renderers");
                Check(motion == JsonFiles.Encode(Document.motion) && camera == JsonFiles.Encode(Document.camera), "visibility action preserves pose and camera");
                SaveNamedSet("素体確認");
                Check(!Dirty && File.Exists(sessionPath), "Japanese named set saved");
                var savedPath = sessionPath; var savedHash = JsonFiles.Sha256(savedPath);
                Check(preferences.startupMode == "pack" && StartupSession() == "", "save does not change startup mode");
                SetBodyVisibility(false);
                Check(Dirty && Document.visibilityOverrides.Length == 0, "restore visibility clears overrides");
                bool switched = false; ConfirmSetChange(() => switched = true);
                Check(!switched && pendingSetAction != null, "dirty switch is deferred");
                pendingSetAction = null; discardRow.style.display = DisplayStyle.None;
                Check(JsonFiles.Sha256(savedPath) == savedHash, "cancel leaves saved set untouched");
                SaveNamedSet("素体確認");
                Check(Dirty && JsonFiles.Sha256(savedPath) == savedHash, "duplicate name refuses overwrite");
                SaveNamedSet("../escape");
                Check(Dirty && sessionPath == savedPath, "invalid name refused");
                startupModePicker.SetValueWithoutNotify("set");
                startupSetPicker.SetValueWithoutNotify(setPaths.First(x => x.Value == savedPath).Key);
                SaveStartupPreferences();
                var reloaded = JsonFiles.Read<ViewerPreferences>(preferencesPath);
                Check(reloaded.startupMode == "set" && reloaded.startupSetPath == savedPath, "explicit startup set persisted");
                SavePreviousState();
                Check(JsonFiles.Read<SessionDocument>(previousPath).visibilityOverrides.Length == 0 && JsonFiles.Sha256(savedPath) == savedHash && Dirty, "previous snapshot separate from named save and dirty");
                startupModePicker.SetValueWithoutNotify("previous"); SaveStartupPreferences();
                Check(StartupSession() == previousPath, "previous startup path selected");
                startupModePicker.SetValueWithoutNotify("pack"); SaveStartupPreferences();
                OpenSet(savedPath);
            }
            catch (Exception e) { failure = e.ToString(); }
            deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 60;
            while (reloadLoop && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (failure == null) try
            {
                Check(!reloadLoop && !Dirty && Document.visibilityOverrides.Length == Active.Verified.Manifest.renderers.Length, "named set reload restores body view");
                OpenSet("");
            } catch (Exception e) { failure = e.ToString(); }
            deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 60;
            while (reloadLoop && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (failure == null) try
            {
                Check(!reloadLoop && !Dirty && sessionPath == "" && Document.visibilityOverrides.Length == 0, "default set reload clears named identity and overrides");
                Check(Active.Verified.Manifest.renderers.All(r => Active.Avatar.Renderers[r.rendererId].enabled == r.defaultVisible), "default actual visibility restored");
                uiRoot.Q<Foldout>("startup-settings").value = true;
                ReflectSetsUi(); SetStatus("確認セット検証完了");
            } catch (Exception e) { failure = e.ToString(); }
            for (int i = 0; i < 5; i++) yield return null;
            yield return new WaitForEndOfFrame();
            try
            {
                var texture = ScreenCapture.CaptureScreenshotAsTexture();
                File.WriteAllBytes(Path.Combine(output, "sets-ui.png"), texture.EncodeToPNG()); Destroy(texture);
                File.WriteAllText(Path.Combine(output, "sets-check.json"), Newtonsoft.Json.JsonConvert.SerializeObject(new { success = failure == null, checks, failure }, Newtonsoft.Json.Formatting.Indented));
            } catch (Exception e) { failure = e.ToString(); Debug.LogException(e); }
            Debug.Log("VIEWER_SETS_CHECK " + (failure ?? "PASS"));
            Application.Quit(failure == null ? 0 : 1);
        }
    }
}
