using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Checks the normal, non-harness entry screen. The existing full
        /// verification intentionally opens every panel for pointer probes;
        /// this probe keeps that compatibility boundary separate from the
        /// user-facing collapsed layout.
        /// </summary>
        public void RunStartupProbe(string output)
        {
            StartCoroutine(StartupProbe(output));
        }

        IEnumerator StartupProbe(string output)
        {
            output = Path.GetFullPath(output);
            Directory.CreateDirectory(output);
            var failures = new List<string>();
            void Check(bool condition, string message)
            {
                if (!condition) failures.Add(message);
            }

            for (int i = 0; i < 12; i++) yield return null;
            Check(active && root != null, "authoring workbench did not open");
            Check(!IsAutomatedUiVerification, "startup probe unexpectedly entered the automation compatibility branch");
            Check(graphDetailsPanel != null && !graphDetailsPanel.value, "graph details panel is expanded on the normal entry screen");
            Check(projectOutputPanel != null && !projectOutputPanel.value, "project output panel is expanded on the normal entry screen");
            Check(shapeCreationPanel != null && shapeCreationPanel.value, "shape creation entry is not expanded on the normal entry screen");
            var save = root?.Q<Button>("command-save");
            var shape = root?.Q<Button>("command-add-shape");
            Check(save != null && save.worldBound.height >= 30, "top command save is not visible");
            Check(shape != null && shape.worldBound.height >= 30, "top shape command is not visible");
            var reopen = root?.Q<Button>("authoring-open-project-empty");
            Check(emptyProjectEntryPanel != null && emptyProjectEntryPanel.resolvedStyle.display != DisplayStyle.None, "empty project reopen entry is hidden");
            Check(reopen != null && reopen.worldBound.height >= 30, "empty project reopen button is not visible");
            Check(root?.worldBound.width > 0 && root?.worldBound.height > 0, "authoring root has no laid out bounds");

            string screenshotPath = Path.Combine(output, "authoring-startup.png");
            Texture2D screenshot = null;
            bool screenshotHasVisiblePixels = false;
            yield return new WaitForEndOfFrame();
            try
            {
                screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                Check(screenshot != null, "authoring startup screenshot was not captured");
                if (screenshot != null)
                {
                    var pixels = screenshot.GetPixels32();
                    for (int i = 0; i < pixels.Length; i++)
                    {
                        var pixel = pixels[i];
                        if (Mathf.Max(pixel.r, Mathf.Max(pixel.g, pixel.b)) > 8)
                        {
                            screenshotHasVisiblePixels = true;
                            break;
                        }
                    }
                    Check(screenshotHasVisiblePixels, "authoring startup screenshot is entirely black");
                    File.WriteAllBytes(screenshotPath, screenshot.EncodeToPNG());
                }
            }
            catch (Exception error) { failures.Add("screenshot: " + error.Message); }
            finally { if (screenshot != null) Destroy(screenshot); }

            var report = new
            {
                schemaVersion = 1,
                passed = failures.Count == 0,
                failures,
                normalEntry = true,
                graphDetailsExpanded = graphDetailsPanel?.value ?? true,
                projectOutputExpanded = projectOutputPanel?.value ?? true,
                shapeCreationExpanded = shapeCreationPanel?.value ?? false,
                commandSaveVisible = save != null && save.worldBound.height >= 30,
                commandShapeVisible = shape != null && shape.worldBound.height >= 30,
                emptyProjectReopenVisible = reopen != null && reopen.worldBound.height >= 30,
                screenshotHasVisiblePixels,
                width = Screen.width,
                height = Screen.height,
                screenshot = File.Exists(screenshotPath) ? screenshotPath : null,
                unityVersion = Application.unityVersion,
                scope = "Normal authoring entry layout only; this does not prove manual mouse, DPI, IME, avatar fit, VRChat, or appearance acceptance."
            };
            File.WriteAllText(Path.Combine(output, "report.json"), JsonConvert.SerializeObject(report, Formatting.Indented));
            Debug.Log("NYAFORGE_AUTHORING_STARTUP_PROBE " + (failures.Count == 0 ? "PASS" : "FAIL"));
            // The normal window asks before quitting when the live session is
            // dirty. A probe has already written its report and must exit
            // deterministically instead of opening the user confirmation UI.
            allowQuit = true;
            Application.Quit(failures.Count == 0 ? 0 : 1);
        }
    }
}
