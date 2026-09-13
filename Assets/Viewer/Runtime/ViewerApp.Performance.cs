using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Profiling;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        sealed class PerformanceSegment
        {
            public string name;
            public double requestedSeconds, elapsedSeconds, startTimeSeconds, endTimeSeconds;
            public int focusedFrames, playingFrames, reloadsAtStart, reloadsAtEnd;
            public readonly List<double> frameTimesMs = new List<double>();
            public readonly List<PerformanceMemory> memorySamples = new List<PerformanceMemory>();

            public object Summary()
            {
                var sorted = frameTimesMs.OrderBy(x => x).ToArray();
                double? Percentile(double p) => sorted.Length == 0 ? (double?)null : sorted[Math.Max(0, (int)Math.Ceiling(p * sorted.Length) - 1)];
                return new
                {
                    name, requestedSeconds, elapsedSeconds, startTimeSeconds, endTimeSeconds,
                    sampleCount = sorted.Length,
                    framesPerSecond = elapsedSeconds > 0 ? (double?)sorted.Length / elapsedSeconds : null,
                    p50Ms = Percentile(.5), p95Ms = Percentile(.95), maxMs = Percentile(1),
                    percentileMethod = "nearest-rank",
                    focusedFrames, playingFrames, reloadsAtStart, reloadsAtEnd,
                    frameTimesMs, memorySamples
                };
            }
        }

        sealed class PerformanceMemory
        {
            public double atSeconds;
            public long unityAllocatedBytes, unityReservedBytes, managedHeapUsedBytes, managedHeapReservedBytes;
            public long? workingSetBytes, peakWorkingSetBytes, privateBytes;
            public string processMemorySource, processMemoryError;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PerformanceMemoryCounters
        {
            public uint cb, pageFaultCount;
            public UIntPtr peakWorkingSetSize, workingSetSize, quotaPeakPagedPoolUsage, quotaPagedPoolUsage;
            public UIntPtr quotaPeakNonPagedPoolUsage, quotaNonPagedPoolUsage, pagefileUsage, peakPagefileUsage, privateUsage;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct PerformancePowerStatus
        {
            public byte acLineStatus, batteryFlag, batteryLifePercent, systemStatusFlag;
            public uint batteryLifeTime, batteryFullLifeTime;
        }

        [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
        static extern IntPtr PerformanceCurrentProcess();
        [DllImport("psapi.dll", EntryPoint = "GetProcessMemoryInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PerformanceGetProcessMemory(IntPtr process, ref PerformanceMemoryCounters counters, uint size);
        [DllImport("kernel32.dll", EntryPoint = "GetSystemPowerStatus", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool PerformanceGetPowerStatus(out PerformancePowerStatus status);
        [DllImport("user32.dll", EntryPoint = "GetDpiForWindow")]
        static extern uint PerformanceGetWindowDpi(IntPtr window);

        static PerformanceMemory CapturePerformanceMemory()
        {
            var sample = new PerformanceMemory
            {
                atSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble,
                unityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                unityReservedBytes = Profiler.GetTotalReservedMemoryLong(),
                managedHeapUsedBytes = Profiler.GetMonoUsedSizeLong(),
                managedHeapReservedBytes = Profiler.GetMonoHeapSizeLong()
            };
            try
            {
                var counters = new PerformanceMemoryCounters { cb = (uint)Marshal.SizeOf<PerformanceMemoryCounters>() };
                if (!PerformanceGetProcessMemory(PerformanceCurrentProcess(), ref counters, counters.cb))
                    throw new System.ComponentModel.Win32Exception(Marshal.GetLastWin32Error());
                if (counters.workingSetSize.ToUInt64() == 0) throw new InvalidOperationException("Windows reported a zero working set.");
                sample.workingSetBytes = checked((long)counters.workingSetSize.ToUInt64());
                sample.peakWorkingSetBytes = checked((long)counters.peakWorkingSetSize.ToUInt64());
                sample.privateBytes = checked((long)counters.privateUsage.ToUInt64());
                sample.processMemorySource = "Windows GetProcessMemoryInfo / PROCESS_MEMORY_COUNTERS_EX";
            }
            catch (Exception nativeError)
            {
                try
                {
                    using var process = System.Diagnostics.Process.GetCurrentProcess();
                    process.Refresh();
                    if (process.WorkingSet64 <= 0) throw new InvalidOperationException("Process.WorkingSet64 is unavailable or zero.");
                    sample.workingSetBytes = process.WorkingSet64;
                    sample.peakWorkingSetBytes = process.PeakWorkingSet64;
                    sample.privateBytes = process.PrivateMemorySize64;
                    sample.processMemorySource = "System.Diagnostics.Process after Refresh";
                    sample.processMemoryError = "Native fallback: " + nativeError.Message;
                }
                catch (Exception fallbackError)
                {
                    sample.processMemorySource = "unavailable";
                    sample.processMemoryError = nativeError.Message + " / " + fallbackError.Message;
                }
            }
            return sample;
        }

        object CapturePerformanceMetadata()
        {
            uint? windowDpi = null;
            object power = null;
            try
            {
                using var process = System.Diagnostics.Process.GetCurrentProcess();
                process.Refresh();
                if (process.MainWindowHandle != IntPtr.Zero)
                {
                    uint dpi = PerformanceGetWindowDpi(process.MainWindowHandle);
                    if (dpi > 0) windowDpi = dpi;
                }
            }
            catch { /* Null records an unavailable measurement, not 100% scaling. */ }
            try
            {
                if (PerformanceGetPowerStatus(out var status))
                    power = new { acLineStatus = (int)status.acLineStatus, batteryFlag = (int)status.batteryFlag,
                        batteryLifePercent = status.batteryLifePercent == 255 ? (int?)null : status.batteryLifePercent,
                        batterySaver = status.systemStatusFlag != 0, note = "AC: 0 offline, 1 online, 255 unknown; batteryFlag 128 means no battery." };
            }
            catch { }
            return new
            {
                platform = Application.platform.ToString(), unityVersion = Application.unityVersion,
                isEditor = Application.isEditor, developmentBuild = Debug.isDebugBuild,
                profilerEnabled = Profiler.enabled, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                graphicsApiVersion = SystemInfo.graphicsDeviceVersion,
                gpuName = SystemInfo.graphicsDeviceName, gpuVendor = SystemInfo.graphicsDeviceVendor,
                gpuDeviceId = SystemInfo.graphicsDeviceID, gpuVendorId = SystemInfo.graphicsDeviceVendorID,
                gpuCapacityMiB = SystemInfo.graphicsMemorySize > 0 ? (int?)SystemInfo.graphicsMemorySize : null,
                gpuMemoryUsageBytes = (long?)null, gpuMemoryUsageNote = "Not measured; capacity is not current usage.",
                cpuName = SystemInfo.processorType, logicalProcessors = SystemInfo.processorCount,
                cpuReportedFrequencyMHz = SystemInfo.processorFrequency,
                systemMemoryMiB = SystemInfo.systemMemorySize, operatingSystem = SystemInfo.operatingSystem,
                windowWidth = Screen.width, windowHeight = Screen.height, fullScreenMode = Screen.fullScreenMode.ToString(),
                reportedScreenDpi = Screen.dpi > 0 ? (float?)Screen.dpi : null, windowDpi,
                primaryDisplayRefreshHz = Screen.currentResolution.refreshRateRatio.value,
                targetFrameRate = Application.targetFrameRate, vSyncCount = QualitySettings.vSyncCount,
                antiAliasing = QualitySettings.antiAliasing, activeColorSpace = QualitySettings.activeColorSpace.ToString(),
                runInBackground = Application.runInBackground, focused,
                renderPipeline = Active?.Verified.Manifest.toolchain.renderPipeline,
                preview = Document?.preview, power
            };
        }

        IEnumerator MeasurePerformanceSegment(PerformanceSegment segment)
        {
            segment.reloadsAtStart = CompletedReloads;
            segment.startTimeSeconds = TimeSeconds;
            double start = UnityEngine.Time.realtimeSinceStartupAsDouble, previous = start, nextMemory = start + 1;
            segment.memorySamples.Add(CapturePerformanceMemory());
            while (UnityEngine.Time.realtimeSinceStartupAsDouble - start < segment.requestedSeconds)
            {
                yield return null;
                double now = UnityEngine.Time.realtimeSinceStartupAsDouble;
                segment.frameTimesMs.Add(Math.Max(0, (now - previous) * 1000));
                previous = now;
                if (focused) segment.focusedFrames++;
                if (IsPlaying) segment.playingFrames++;
                if (now >= nextMemory)
                {
                    segment.memorySamples.Add(CapturePerformanceMemory());
                    nextMemory = now + 1;
                }
            }
            segment.elapsedSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble - start;
            segment.endTimeSeconds = TimeSeconds;
            segment.reloadsAtEnd = CompletedReloads;
            segment.memorySamples.Add(CapturePerformanceMemory());
        }

        IEnumerator RunPerformance(string output)
        {
            string failure = null, restorationError = null;
            SessionDocument before = null;
            bool beforeDirty = false, restored = false;
            string performanceClipId = null;
            object metadata = null;
            var playback = new PerformanceSegment { name = "foreground-playback", requestedSeconds = 60 };
            var idle = new PerformanceSegment { name = "stopped-idle", requestedSeconds = 5 };
            double entryAtSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble;
            double? readyObservedAtSeconds = null;
            try { output = Path.GetFullPath(output); Directory.CreateDirectory(output); }
            catch (Exception e) { Debug.LogError("VIEWER_PERFORMANCE_FAILED " + e.Message); if (Arg("--performance-exit") == "true") Application.Quit(1); yield break; }
            double deadline = entryAtSeconds + 180;
            while ((Active?.Avatar == null || IsBusy || reloadLoop) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            try
            {
                if (Application.platform != RuntimePlatform.WindowsPlayer) throw new InvalidOperationException("Run performance capture in the Windows Player.");
                if (Active?.Avatar == null || IsBusy || reloadLoop) throw new InvalidOperationException("Initial load did not finish: " + LastErrorCode + " " + Status);
                readyObservedAtSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble;
                // The production avatar packs normally include pose-arms-up,
                // while the small synthetic fixture intentionally contains
                // only rest and neck-tilt. Measure the first stable clip when
                // the preferred pose is unavailable and record the choice so
                // reports never pretend that a different pose was measured.
                var performanceClip = Active.Verified.Manifest.clips.FirstOrDefault(c => c.clipId == "pose-arms-up")
                    ?? Active.Verified.Manifest.clips.FirstOrDefault(c => c.required)
                    ?? Active.Verified.Manifest.clips.FirstOrDefault();
                if (performanceClip == null || string.IsNullOrWhiteSpace(performanceClip.clipId))
                    throw new InvalidOperationException("The pack has no clip available for performance measurement.");
                performanceClipId = performanceClip.clipId;
                before = Snapshot(); beforeDirty = Dirty;
                Pause();
                Edit(s => { s.motion.clipId = performanceClipId; s.motion.timeSeconds = 0; s.motion.speed = 1; s.motion.loop = true; s.preview.mode = "original"; s.preview.lightPresetId = "studio"; });
                CameraPreset("all"); CameraPreset("front");
                if (Document.motion.clipId != performanceClipId) throw new InvalidOperationException("Playback setup was rejected: " + Status);
                IsPlaying = true;
                SetStatus("性能を測定しています · 再生60秒、停止5秒");
            }
            catch (Exception e) { failure = e.ToString(); }
            if (failure == null)
            {
                yield return new WaitForSecondsRealtime(2); // Warm pose/shader/UI work is outside the measured 60 seconds.
                metadata = CapturePerformanceMetadata();
                yield return MeasurePerformanceSegment(playback);
                Pause();
                yield return MeasurePerformanceSegment(idle);
                yield return new WaitForEndOfFrame();
                try { ScreenCapture.CaptureScreenshot(Path.Combine(output, "performance.png")); }
                catch (Exception e) { failure = "Screenshot: " + e; }
                yield return new WaitForSecondsRealtime(1);
            }
            if (before != null)
            {
                try
                {
                    if (Active?.Avatar == null || IsBusy || Active.Verified.Hash != before.pack.manifestSha256)
                        throw new InvalidOperationException("Pack changed during measurement; original snapshot was not applied to a different pack.");
                    Active.Avatar.Apply(before, before.motion.timeSeconds);
                    Document = before; TimeSeconds = before.motion.timeSeconds; IsPlaying = false; Dirty = beforeDirty;
                    ApplyCameraAndLight(); RebuildControls(); restored = true;
                }
                catch (Exception e) { restorationError = e.ToString(); }
            }
            bool validForegroundPlayback = playback.frameTimesMs.Count > 0 &&
                playback.focusedFrames == playback.frameTimesMs.Count && playback.playingFrames == playback.frameTimesMs.Count &&
                playback.reloadsAtStart == playback.reloadsAtEnd && playback.elapsedSeconds >= playback.requestedSeconds;
            bool validStoppedIdle = idle.frameTimesMs.Count > 0 && idle.playingFrames == 0 && idle.startTimeSeconds == idle.endTimeSeconds &&
                idle.reloadsAtStart == idle.reloadsAtEnd && idle.focusedFrames == idle.frameTimesMs.Count;
            var report = new
            {
                schemaVersion = 1, recordedAtUtc = DateTime.UtcNow.ToString("O"),
                measurementCompleted = failure == null && restorationError == null,
                failure, restorationError, snapshotRestored = restored, restoredPlaying = IsPlaying,
                startup = new { entryAtSeconds, readyObservedAtSeconds,
                    clock = "Unity Time.realtimeSinceStartupAsDouble; excludes OS process launch before Unity initialization.",
                    readiness = "Observed Active avatar with no reload in progress; not an OS-launch or first-present measurement." },
                metadata, pack = before?.pack,
                performanceClipId,
                validForegroundPlayback, validStoppedIdle,
                playback = playback.Summary(), idle = idle.Summary(),
                notes = new[] { "Frame cadence uses actual coroutine resume intervals from a monotonic clock, including stalls.",
                    "No forced GC or asset unloading during measurement. Process memory samples are approximately one second apart.",
                    "Budget pass/fail is intentionally not asserted; foreground validity and metadata must be checked before comparison.",
                    "This captures one startup observation and one playback run, not the required ten-startup series or twenty-update test.",
                    "The preferred pose-arms-up clip is used when present; otherwise the first required or available clip is recorded in performanceClipId." }
            };
            try { File.WriteAllText(Path.Combine(output, "performance.json"), JsonConvert.SerializeObject(report, Formatting.Indented)); }
            catch (Exception e) { failure = "Writing performance.json: " + e; }
            bool completed = failure == null && restorationError == null;
            Debug.Log("VIEWER_PERFORMANCE_FINISHED " + (completed ? "RECORDED" : "FAILED") + " foregroundPlayback=" + validForegroundPlayback);
            SetStatus(completed ? "性能測定を保存しました" : "性能測定の記録に失敗しました", completed ? "" : "PERFORMANCE_FAILED");
            if (Arg("--performance-exit") == "true") Application.Quit(completed ? 0 : 1);
        }
    }
}
