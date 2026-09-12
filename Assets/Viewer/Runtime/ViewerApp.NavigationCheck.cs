using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        IEnumerator RunNavigationCheck(string output)
        {
            output = Path.GetFullPath(output); Directory.CreateDirectory(output);
            var failures = new List<string>();
            var checks = new List<string>();
            void Check(bool ok, string message) { (ok ? checks : failures).Add(message); }
            IEnumerator Ready()
            {
                double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 45;
                do { yield return null; } while ((reloadLoop || updateChecking || IsBusy) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline);
                Check(!reloadLoop && !updateChecking && !IsBusy, "load completed within deadline");
            }
            string input = Arg("--navigation-pack");
            AcceptPickedPath(input);
            yield return Ready();
            Check(Active != null && Document != null, "GUI selected pack loaded");
            if (Active != null && Document != null)
            {
                var active = Active; var document = Document;
                string history = string.Join("|", recentPackPaths);
                AcceptPickedPath(null);
                Check(Active == active && Document == document && history == string.Join("|", recentPackPaths), "cancel keeps model, document and history");
                var renderer = Active.Verified.Manifest.renderers.First();
                SetVisible(renderer.rendererId, !renderer.defaultVisible);
                SaveNamedSet("navigation-fixture");
                string savedPath = sessionPath;
                Check(File.Exists(savedPath), "named session saved");
                Dirty = true;
                AcceptPickedPath(input);
                Check(pendingSetAction != null && Active == active, "dirty state requires discard before switching");
                pendingSetAction = null; discardRow.style.display = DisplayStyle.None;
                Dirty = false;
                AcceptPickedPath(Path.Combine(output, "missing.json"));
                yield return Ready();
                Check(Active == active && sessionPath == savedPath && history == string.Join("|", recentPackPaths), "invalid path preserves current session and history");
                AcceptPickedPath(input);
                yield return Ready();
                Check(!Dirty && sessionPath == "" && sessionHash == null, "fresh pack clears saved-set identity");
                Check(Active.Avatar.Renderers[renderer.rendererId].enabled == renderer.defaultVisible, "fresh pack restores default renderer visibility");
                AcceptPickedPath(savedPath);
                yield return Ready();
                Check(sessionPath == savedPath && !Dirty, "saved session restores identity");
                Check(Active.Avatar.Renderers[renderer.rendererId].enabled != renderer.defaultVisible, "saved session restores visibility override");
            }
            CloseNavigationPanels();
            yield return null;
            Check(recentPanel.resolvedStyle.display == DisplayStyle.None && setsPanel.resolvedStyle.display == DisplayStyle.None && settingsPanel.resolvedStyle.display == DisplayStyle.None, "utility panels hidden by default");
            foreach (string id in new[] { "browse-pack", "show-recent", "show-sets", "show-settings", "open-authoring" })
            {
                var b = uiRoot.Q<Button>(id);
                Check(b.worldBound.height >= 30 && b.worldBound.xMin >= 0 && b.worldBound.xMax <= uiRoot.worldBound.xMax + 1, id + " fits window");
            }
            Check(viewport.worldBound.width >= 150 && viewport.worldBound.height >= 200, "viewport has usable area");
            yield return CaptureNavigation(output, "main", failures);
            ToggleNavigationPanel(setsPanel); yield return null;
            Check(setsPanel.resolvedStyle.display == DisplayStyle.Flex, "set controls can be opened");
            yield return CaptureNavigation(output, "sets", failures);
            ToggleNavigationPanel(settingsPanel); yield return null;
            Check(setsPanel.resolvedStyle.display == DisplayStyle.None && settingsPanel.resolvedStyle.display == DisplayStyle.Flex, "only one utility panel shown");
            yield return CaptureNavigation(output, "settings", failures);
            CloseNavigationPanels();
            File.WriteAllText(Path.Combine(output, "report.json"), JsonConvert.SerializeObject(new { passed = failures.Count == 0, failures, checks, width = Screen.width, height = Screen.height }, Formatting.Indented));
            Debug.Log("NYAFORGE_NAVIGATION_CHECK " + (failures.Count == 0 ? "PASS" : "FAIL"));
            Application.Quit(failures.Count == 0 ? 0 : 1);
        }

        IEnumerator CaptureNavigation(string output, string name, List<string> failures)
        {
            WakeRendering();
            var settings = GetComponent<UIDocument>().panelSettings;
            var previousPanel = settings.targetTexture;
            var previousCamera = previewCamera.targetTexture;
            var capture = new RenderTexture(Screen.width, Screen.height, 24);
            var model = new RenderTexture(Math.Max(1, (int)viewport.worldBound.width), Math.Max(1, (int)viewport.worldBound.height), 24);
            capture.Create(); model.Create();
            bool wasEnabled = previewCamera.enabled;
            previewCamera.enabled = false;
            previewCamera.targetTexture = model; previewCamera.rect = new Rect(0, 0, 1, 1); previewCamera.Render();
            var image = new Image { image = model, pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.StretchToFill };
            image.style.position = Position.Absolute; image.style.left = 0; image.style.right = 0; image.style.top = 0; image.style.bottom = 0;
            viewport.Insert(0, image);
            settings.targetTexture = capture;
            for (int i = 0; i < 8; i++) yield return null;
            yield return new WaitForEndOfFrame();
            Texture2D texture = null;
            var previousActive = RenderTexture.active;
            try
            {
                texture = new Texture2D(capture.width, capture.height, TextureFormat.RGBA32, false);
                RenderTexture.active = capture; texture.ReadPixels(new Rect(0, 0, capture.width, capture.height), 0, 0); texture.Apply();
                var colors = new HashSet<Color32>(); var pixels = texture.GetPixels32();
                for (int i = 0; i < pixels.Length; i += 97) colors.Add(pixels[i]);
                if (colors.Count < 9) failures.Add(name + " capture contains no rendered content");
                File.WriteAllBytes(Path.Combine(output, name + ".png"), texture.EncodeToPNG());
            }
            catch (Exception e) { failures.Add(name + ": " + e); }
            finally
            {
                RenderTexture.active = previousActive; settings.targetTexture = previousPanel;
                previewCamera.targetTexture = previousCamera; previewCamera.enabled = wasEnabled; image.RemoveFromHierarchy();
                capture.Release(); model.Release(); Destroy(capture); Destroy(model); if (texture) Destroy(texture);
                UpdateViewport();
            }
        }
    }
}
