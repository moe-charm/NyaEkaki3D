using System;
using System.Collections;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Rendering;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        IEnumerator RunStartupProbe(string output)
        {
            double entryAtSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble;
            string entryAtUtc = DateTime.UtcNow.ToString("O");
            bool splashFinishedAtEntry = SplashScreen.isFinished;
            bool empty = Arg("--startup-empty") == "true";
            string failure = null;
            double? readyActiveAtSeconds = null, firstReadyEndOfFrameAtSeconds = null;
            string readyActiveAtUtc = null, firstReadyEndOfFrameAtUtc = null;
            PerformanceMemory memoryBeforeScreenshot = null;
            object metadata = null;
            int screenshotWidth = 0, screenshotHeight = 0;
            bool uiAttached = false;
            try { output = Path.GetFullPath(output); Directory.CreateDirectory(output); }
            catch (Exception e)
            {
                Debug.LogError("VIEWER_STARTUP_PROBE_FAILED " + e.Message);
                if (Arg("--startup-exit") == "true") Application.Quit(1);
                yield break;
            }

            if (!empty)
            {
                double deadline = entryAtSeconds + 60;
                while ((Active?.Avatar == null || IsBusy || reloadLoop) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                if (Active?.Avatar == null || IsBusy || reloadLoop) failure = "Model readiness timed out: " + LastErrorCode + " " + Status;
                else
                {
                    readyActiveAtSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble;
                    readyActiveAtUtc = DateTime.UtcNow.ToString("O");
                }
            }
            else if (Active != null || reloadLoop || IsBusy) failure = "Empty startup requested but a pack load was started.";

            if (failure == null)
            {
                yield return new WaitForEndOfFrame();
                firstReadyEndOfFrameAtSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble;
                firstReadyEndOfFrameAtUtc = DateTime.UtcNow.ToString("O");
                // Read process/Unity allocation before allocating a CPU screenshot
                // texture and PNG buffer, so those are not counted as startup RAM.
                memoryBeforeScreenshot = CapturePerformanceMemory();
                metadata = CapturePerformanceMetadata();
                uiAttached = uiRoot != null && uiRoot.panel != null && uiRoot.resolvedStyle.width > 0 && uiRoot.resolvedStyle.height > 0;
                Texture2D screenshot = null;
                try
                {
                    if (Application.platform != RuntimePlatform.WindowsPlayer) throw new InvalidOperationException("Startup probe requires the normal Windows Player.");
                    if (!uiAttached) throw new InvalidOperationException("UI panel was not attached and laid out at the first ready frame.");
                    if (empty && Active != null) throw new InvalidOperationException("Empty startup loaded a model before the ready frame.");
                    screenshot = ScreenCapture.CaptureScreenshotAsTexture();
                    if (!screenshot) throw new InvalidOperationException("Screenshot readback returned no texture.");
                    screenshotWidth = screenshot.width; screenshotHeight = screenshot.height;
                    var png = screenshot.EncodeToPNG();
                    if (png == null || png.Length == 0) throw new InvalidOperationException("Screenshot PNG encoding failed.");
                    File.WriteAllBytes(Path.Combine(output, "startup.png"), png);
                }
                catch (Exception e) { failure = e.ToString(); }
                finally { if (screenshot) Destroy(screenshot); }
            }
            var report = new
            {
                schemaVersion = 1,
                recordedAtUtc = DateTime.UtcNow.ToString("O"),
                measurementCompleted = failure == null, failure,
                mode = empty ? "empty" : "loaded",
                processId = System.Diagnostics.Process.GetCurrentProcess().Id,
                entryAtUtc, entryAtSeconds, splashFinishedAtEntry,
                readyActiveAtUtc, readyActiveAtSeconds,
                firstReadyEndOfFrameAtUtc, firstReadyEndOfFrameAtSeconds,
                splashFinishedAtReadyFrame = SplashScreen.isFinished,
                uiAttached, screenshotWidth, screenshotHeight,
                memoryBeforeScreenshot, metadata,
                pack = empty ? null : Active?.Verified.Manifest.revision,
                manifestSha256 = empty ? null : Active?.Verified.Hash,
                notes = new[]
                {
                    "Unity realtime starts after OS process launch; external launch timing must be measured separately.",
                    "UTC timestamps allow correlation with the external launch record; they are not a monotonic duration clock.",
                    "First ready end-of-frame means the Unity frame reached WaitForEndOfFrame with UI and optional avatar ready. Physical monitor presentation is not measured.",
                    "Memory was sampled immediately before screenshot readback and PNG allocation. No forced GC or warmup delay.",
                    "Empty mode requires Start to skip opening packs. This probe reports measurements and does not assert startup or memory budgets."
                }
            };
            try { File.WriteAllText(Path.Combine(output, "startup.json"), JsonConvert.SerializeObject(report, Formatting.Indented)); }
            catch (Exception e) { failure = "Writing startup.json: " + e; }
            Debug.Log("VIEWER_STARTUP_PROBE_FINISHED " + (failure == null ? "RECORDED" : "FAILED") + " mode=" + (empty ? "empty" : "loaded"));
            if (Arg("--startup-exit") == "true") Application.Quit(failure == null ? 0 : 1);
        }
    }
}
