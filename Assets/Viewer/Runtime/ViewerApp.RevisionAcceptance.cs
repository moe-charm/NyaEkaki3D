using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        sealed class RevisionRun
        {
            public readonly JArray samples = new JArray();
            public readonly List<string> checks = new List<string>();
            public readonly string startedUtc = DateTime.UtcNow.ToString("O");
            public string seriesPath, seriesSha256, failure;
            public int attempt, initialCompletedReloads;
            public double startedSeconds;
            public JObject baselineCounts;
            public SessionDocument expectedState;
        }

        // Own MoveNext so exceptions in setup, individual assertions, or report
        // creation become a terminal failure report rather than a lost coroutine.
        IEnumerator RunRevisionAcceptance(string output)
        {
            var run = new RevisionRun { startedSeconds = UnityEngine.Time.realtimeSinceStartupAsDouble };
            var operation = RevisionAcceptanceSteps(output, run);
            while (true)
            {
                bool moved = false;
                object current = null;
                try { moved = operation.MoveNext(); if (moved) current = operation.Current; }
                catch (Exception e) { run.failure = e.ToString(); }
                if (run.failure != null || !moved) break;
                yield return current;
            }
            try { (operation as IDisposable)?.Dispose(); }
            catch (Exception e) { run.failure = (run.failure ?? "") + "\n" + e; }
            try { WriteRevisionReport(output, run, run.failure == null ? "passed" : "failed"); }
            catch (Exception e) { run.failure = (run.failure ?? "") + "\nReport write failed: " + e; Debug.LogException(e); }
            Debug.Log("VIEWER_REVISION_ACCEPTANCE_FINISHED " + (run.failure == null ? "PASS" : "FAIL") + " " + run.samples.Count + "/20");
            if (run.failure != null) Debug.LogError(run.failure);
            if (Arg("--revision-exit") == "true") Application.Quit(run.failure == null ? 0 : 1);
        }

        IEnumerator RevisionAcceptanceSteps(string output, RevisionRun run)
        {
            Directory.CreateDirectory(output);
            if (Application.isEditor || Debug.isDebugBuild || Application.platform != RuntimePlatform.WindowsPlayer)
                throw new InvalidOperationException("A22 must run in a normal nondevelopment Windows Player.");
            if (SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Direct3D11)
                throw new InvalidOperationException("A22 Windows pack verification requires Direct3D11.");
            run.seriesPath = Path.GetFullPath(Arg("--revision-series") ?? throw new ArgumentException("--revision-series is required"));
            run.seriesSha256 = JsonFiles.Sha256(run.seriesPath);
            var input = JObject.Parse(File.ReadAllText(run.seriesPath));
            var revisions = input["revisions"] as JArray;
            if ((int?)input["schemaVersion"] != 1 || (int?)input["completedCount"] != 20 ||
                (bool?)input["sourceRestored"] != true || revisions == null || revisions.Count != 20 ||
                (string)input["status"] != "built-awaiting-runtime-verification")
                throw new InvalidOperationException("The source report must prove 20 completed builds with the original FBX restored.");
            foreach (string name in new[] { "revision", "inputSha256", "importedGeometrySha256", "manifestSha256", "contentBundleSha256" })
                if (revisions.Select(row => (string)row[name]).Any(string.IsNullOrWhiteSpace) ||
                    revisions.Select(row => (string)row[name]).Distinct(StringComparer.Ordinal).Count() != 20)
                    throw new InvalidOperationException("The series must have 20 unique " + name + " values.");
            run.checks.Add("20-distinct-input-geometry-manifest-content-build-records");
            WriteRevisionReport(output, run, "waiting-for-initial-pack");
            double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 180;
            while ((Active == null || reloadLoop || IsBusy) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (Active?.Avatar == null || reloadLoop || IsBusy)
                throw new InvalidOperationException("Initial external load did not become ready: " + LastErrorCode + " " + Status);
            Pause();
            var collar = Active.Verified.Manifest.renderers.Single(row => row.path.EndsWith("/PrototypeCollar", StringComparison.Ordinal));
            var ribbon = Active.Verified.Manifest.renderers.Single(row => row.path.EndsWith("/PrototypeRibbon", StringComparison.Ordinal));
            var morph = Active.Verified.Manifest.morphBindings.First(row => row.shapeName == "Shrink_Neck");
            SetVisible(collar.rendererId, true);
            SetVisible(ribbon.rendererId, true);
            SetMorph(morph.rendererId, morph.shapeName, 15);
            Edit(state =>
            {
                state.motion.clipId = "pose-arms-up"; state.motion.timeSeconds = .5;
                state.motion.speed = 1; state.motion.loop = false;
                state.preview.mode = "original"; state.preview.lightPresetId = "studio";
            });
            CameraPreset("front"); CameraPreset("neck"); Pause();
            if (LastErrorCode != "" || IsPlaying || TimeSeconds != .5 ||
                Document.motion.clipId != "pose-arms-up" || Math.Abs(Active.Avatar.MorphValue(morph.rendererId, morph.shapeName) - 15) > .01)
                throw new InvalidOperationException("Could not prepare the fixed paused pose/morph for the test.");
            run.expectedState = Snapshot();
            run.initialCompletedReloads = CompletedReloads;
            var expectedTransforms = RevisionTransforms();
            var seenRuntimeGeometry = new HashSet<string>(StringComparer.Ordinal);
            var seenCollarGeometry = new HashSet<string>(StringComparer.Ordinal);
            var seenContent = new HashSet<string>(StringComparer.Ordinal);
            var seenManifests = new HashSet<string>(StringComparer.Ordinal);
            run.checks.Add("paused-arms-up-0.5s-neck15-collar-visible-neck-camera-prepared");
            for (int i = 0; i < revisions.Count; i++)
            {
                var revision = revisions[i];
                if ((int?)revision["index"] != i + 1) throw new InvalidOperationException("Series indices must be 1..20 in order.");
                run.attempt = i + 1;
                WriteRevisionReport(output, run, "switching");
                string manifestPath = Path.GetFullPath((string)revision["manifestPath"]);
                string expectedManifestHash = (string)revision["manifestSha256"];
                int beforeCompleted = CompletedReloads;
                var beforeMemory = RevisionMemory();
                double start = UnityEngine.Time.realtimeSinceStartupAsDouble;
                RequestReload(manifestPath, null, expectedManifestHash);
                deadline = start + 180;
                long sampledPeakAllocation = Profiler.GetTotalAllocatedMemoryLong();
                double nextSample = start;
                while ((reloadLoop || IsBusy) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline)
                {
                    if (UnityEngine.Time.realtimeSinceStartupAsDouble >= nextSample)
                    {
                        sampledPeakAllocation = Math.Max(sampledPeakAllocation, Profiler.GetTotalAllocatedMemoryLong());
                        nextSample = UnityEngine.Time.realtimeSinceStartupAsDouble + .1;
                    }
                    yield return null;
                }
                double ready = UnityEngine.Time.realtimeSinceStartupAsDouble;
                if (reloadLoop || IsBusy || Active?.Avatar == null || CompletedReloads != beforeCompleted + 1 ||
                    Active.Verified.Manifest.revision != (string)revision["revision"] || LastErrorCode != "")
                    throw new InvalidOperationException("Revision " + (i + 1) + " failed or timed out: " + LastErrorCode + " " + Status);
                AssertRevisionState(run.expectedState, expectedTransforms, collar.rendererId, morph);
                // At least one real second with normal rendering and natural managed
                // collection. Never force GC or UnloadUnusedAssets to improve results.
                double settledAfter = ready + 1;
                while (UnityEngine.Time.realtimeSinceStartupAsDouble < settledAfter) yield return null;
                AssertRevisionState(run.expectedState, expectedTransforms, collar.rendererId, morph);
                var manifest = Active.Verified.Manifest;
                if (Active.Verified.Hash != expectedManifestHash || !seenManifests.Add(Active.Verified.Hash))
                    throw new InvalidOperationException("Runtime manifest differs from the built series or is repeated.");
                string contentHash = manifest.bundles.Single(bundle => bundle.id == "content").sha256;
                if (contentHash != (string)revision["contentBundleSha256"] || !seenContent.Add(contentHash))
                    throw new InvalidOperationException("Runtime loaded content hash differs from the series or is repeated.");
                string geometryHash = RevisionGeometryHash(false);
                string collarHash = RevisionGeometryHash(true);
                if (geometryHash != (string)revision["importedGeometrySha256"] || !seenRuntimeGeometry.Add(geometryHash) || !seenCollarGeometry.Add(collarHash))
                    throw new InvalidOperationException("Actual loaded garment vertices differ from the built input or were reused.");
                var memory = RevisionMemory();
                var counts = RevisionCounts();
                if (run.baselineCounts == null) run.baselineCounts = (JObject)counts.DeepClone();
                AssertRevisionCounts(counts, run.baselineCounts);
                run.samples.Add(new JObject
                {
                    ["index"] = i + 1, ["revision"] = manifest.revision,
                    ["manifestPath"] = manifestPath, ["manifestSha256"] = Active.Verified.Hash,
                    ["contentBundleSha256"] = contentHash, ["loadedGeometrySha256"] = geometryHash,
                    ["loadedCollarGeometrySha256"] = collarHash,
                    ["completedReloads"] = CompletedReloads,
                    ["requestToReadySeconds"] = ready - start,
                    ["settleSeconds"] = UnityEngine.Time.realtimeSinceStartupAsDouble - ready,
                    ["sampledPeakUnityAllocationDuringReload"] = sampledPeakAllocation,
                    ["memoryBeforeSwitch"] = beforeMemory, ["memoryAfterNaturalSettle"] = memory,
                    ["counts"] = counts,
                    ["sessionStatePreserved"] = true, ["actualBoneTransformsPreserved"] = true,
                    ["actualMorphWeight"] = Active.Avatar.MorphValue(morph.rendererId, morph.shapeName)
                });
                WriteRevisionReport(output, run, "running");
                if (i == 0 || i == 9 || i == 19)
                {
                    string screenshotPath = Path.Combine(output, "revision-" + (i + 1).ToString("00") + ".png");
                    WakeRendering();
                    yield return new WaitForEndOfFrame();
                    ScreenCapture.CaptureScreenshot(screenshotPath);
                    deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 10;
                    do { yield return null; }
                    while ((!File.Exists(screenshotPath) || new FileInfo(screenshotPath).Length == 0) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline);
                    if (!File.Exists(screenshotPath) || new FileInfo(screenshotPath).Length == 0)
                        throw new IOException("Screenshot was not produced: " + screenshotPath);
                    ((JObject)run.samples[i])["screenshot"] = screenshotPath;
                }
                Debug.Log("VIEWER_REVISION_ACCEPTED " + (i + 1) + "/20 " + manifest.revision);
            }
            if (JsonFiles.Sha256(run.seriesPath) != run.seriesSha256) throw new InvalidOperationException("Source series report changed during verification.");
            run.checks.Add("20-actual-external-revisions-loaded-and-reapplied");
            run.checks.Add("20-unique-runtime-collar-and-garment-vertex-hashes");
            run.checks.Add("camera-pose-time-morph-visibility-preserved-through-all-switches");
            run.checks.Add("exactly-two-live-bundles-and-bounded-resource-counts-after-natural-settle");
            run.checks.Add("first-middle-last-player-screenshots-written");
        }

        void WriteRevisionReport(string output, RevisionRun run, string status)
        {
            Directory.CreateDirectory(output);
            var report = new JObject
            {
                ["schemaVersion"] = 1, ["status"] = status,
                ["success"] = status == "passed" && run.samples.Count == 20,
                ["startedUtc"] = run.startedUtc,
                ["elapsedSeconds"] = UnityEngine.Time.realtimeSinceStartupAsDouble - run.startedSeconds,
                ["sourceSeriesPath"] = run.seriesPath, ["sourceSeriesSha256"] = run.seriesSha256,
                ["currentAttempt"] = run.attempt, ["completedCount"] = run.samples.Count,
                ["initialCompletedReloads"] = run.initialCompletedReloads, ["completedReloads"] = CompletedReloads,
                ["checks"] = JArray.FromObject(run.checks), ["failure"] = run.failure,
                ["currentErrorCode"] = LastErrorCode, ["currentStatus"] = Status,
                ["activeRevision"] = Active?.Verified.Manifest.revision,
                ["unityVersion"] = Application.unityVersion, ["isEditor"] = Application.isEditor,
                ["developmentBuild"] = Debug.isDebugBuild,
                ["graphicsApi"] = SystemInfo.graphicsDeviceType.ToString(), ["gpu"] = SystemInfo.graphicsDeviceName,
                ["expectedState"] = run.expectedState == null ? JValue.CreateNull() : JObject.FromObject(run.expectedState),
                ["revisions"] = run.samples.DeepClone(),
                ["forcedGcOrUnloadUnusedAssets"] = false,
                ["scope"] = "A22: 20 actual external revisions with fixed state, real loaded mesh identity, and naturally settled resource samples. Request-to-ready includes validation, load, pose/morph reapplication, and UI refresh. Count bounds do not prove a working-set plateau, a frame-time budget, or the full performance gate."
            };
            string path = Path.Combine(output, "revision-acceptance.json");
            string temp = path + ".tmp";
            File.WriteAllText(temp, report.ToString(Formatting.Indented));
            if (File.Exists(path)) File.Replace(temp, path, null); else File.Move(temp, path);
        }

        Dictionary<string, (Vector3 position, Quaternion rotation, Vector3 scale)> RevisionTransforms()
        {
            var root = Active.Avatar.Root.transform;
            string PathOf(Transform item)
            {
                if (item == root) return "";
                var names = new List<string>();
                for (var t = item; t && t != root; t = t.parent) names.Insert(0, t.name);
                return string.Join("/", names);
            }
            return root.GetComponentsInChildren<Transform>(true).ToDictionary(PathOf,
                transform => (transform.localPosition, transform.localRotation, transform.localScale), StringComparer.Ordinal);
        }

        void AssertRevisionState(SessionDocument expected,
            Dictionary<string, (Vector3 position, Quaternion rotation, Vector3 scale)> expectedTransforms,
            string collarId, MorphBinding morph)
        {
            var actualJson = JObject.FromObject(Snapshot()); actualJson.Remove("pack");
            var expectedJson = JObject.FromObject(expected); expectedJson.Remove("pack");
            if (!JToken.DeepEquals(actualJson, expectedJson) || IsPlaying || TimeSeconds != .5)
                throw new InvalidOperationException("Paused session values changed during revision replacement.");
            var camera = expected.camera;
            var orientation = new Quaternion(camera.orientationQuat[0], camera.orientationQuat[1], camera.orientationQuat[2], camera.orientationQuat[3]).normalized;
            var position = new Vector3(camera.targetMeters[0], camera.targetMeters[1], camera.targetMeters[2]) - orientation * Vector3.forward * camera.distanceMeters;
            if (Vector3.Distance(previewCamera.transform.position, position) > .00001f ||
                Quaternion.Angle(previewCamera.transform.rotation, orientation) > .05f ||
                Mathf.Abs(previewCamera.fieldOfView - camera.verticalFovDegrees) > .001f)
                throw new InvalidOperationException("Actual camera transform differs from the preserved state.");
            foreach (var row in Active.Verified.Manifest.renderers)
            {
                bool visible = expected.visibilityOverrides.FirstOrDefault(value => value.rendererId == row.rendererId)?.visible ?? row.defaultVisible;
                var renderer = Active.Avatar.Renderers[row.rendererId];
                if (renderer.enabled != visible || visible && !renderer.gameObject.activeInHierarchy)
                    throw new InvalidOperationException("Actual visibility differs after revision replacement: " + row.rendererId);
            }
            if (!Active.Avatar.Renderers[collarId].enabled || Math.Abs(Active.Avatar.MorphValue(morph.rendererId, morph.shapeName) - 15) > .01)
                throw new InvalidOperationException("Actual collar visibility or neck morph was lost.");
            var transforms = RevisionTransforms();
            if (transforms.Count != expectedTransforms.Count) throw new InvalidOperationException("Transform count changed.");
            foreach (var pair in expectedTransforms)
                if (!transforms.TryGetValue(pair.Key, out var actual) ||
                    Vector3.Distance(actual.position, pair.Value.position) > .00001f ||
                    Vector3.Distance(actual.scale, pair.Value.scale) > .00001f ||
                    Quaternion.Angle(actual.rotation, pair.Value.rotation) > .05f)
                    throw new InvalidOperationException("Actual posed transform changed: " + pair.Key);
        }

        string RevisionGeometryHash(bool collarOnly)
        {
            var rows = Active.Verified.Manifest.renderers.Where(row => row.path.EndsWith("/PrototypeCollar", StringComparison.Ordinal) ||
                !collarOnly && row.path.EndsWith("/PrototypeRibbon", StringComparison.Ordinal));
            var renderers = rows.Select(row => Active.Avatar.Renderers[row.rendererId] as SkinnedMeshRenderer).OrderBy(renderer => renderer.name, StringComparer.Ordinal).ToArray();
            if (renderers.Length != (collarOnly ? 1 : 2) || renderers.Any(renderer => !renderer))
                throw new InvalidOperationException("Expected runtime garment renderers are missing.");
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                foreach (var renderer in renderers)
                {
                    var mesh = renderer.sharedMesh;
                    if (!mesh || !mesh.isReadable || mesh.vertexCount == 0)
                        throw new InvalidOperationException("Actual runtime garment mesh must be readable for identity verification.");
                    writer.Write(renderer.name); writer.Write(mesh.vertexCount);
                    foreach (var point in mesh.vertices) { writer.Write(point.x); writer.Write(point.y); writer.Write(point.z); }
                }
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }

        JObject RevisionCounts()
        {
            return new JObject
            {
                ["loadedBundles"] = AssetBundle.GetAllLoadedAssetBundles().Count(),
                ["activePackBundles"] = Active.Bundles.Count,
                ["avatarRenderers"] = Active.Avatar.Renderers.Count,
                ["avatarDistinctMaterials"] = Active.Avatar.Renderers.Values.SelectMany(renderer => renderer.sharedMaterials).Distinct().Count(),
                // Unity 2022.3 does not expose FindObjectsByType<T> with the
                // FindObjectsInactive overload. The includeInactive overload
                // is available in both Unity 2022 LTS and Unity 6 and keeps
                // this resource-count probe semantically equivalent.
                ["sceneRenderers"] = UnityEngine.Object.FindObjectsOfType<Renderer>(true).Length,
                ["allLoadedRenderers"] = Resources.FindObjectsOfTypeAll<Renderer>().Length,
                ["allLoadedMaterials"] = Resources.FindObjectsOfTypeAll<Material>().Length,
                ["allLoadedGameObjects"] = Resources.FindObjectsOfTypeAll<GameObject>().Length,
                ["scenePreviewAvatars"] = UnityEngine.Object.FindObjectsOfType<Transform>(true).Count(t => t.name == "PreviewAvatar")
            };
        }

        static void AssertRevisionCounts(JObject counts, JObject baseline)
        {
            if ((int)counts["loadedBundles"] != 2 || (int)counts["activePackBundles"] != 2 || (int)counts["scenePreviewAvatars"] != 1)
                throw new InvalidOperationException("A revision switch retained duplicate bundles or avatars.");
            foreach (string name in new[] { "avatarRenderers", "avatarDistinctMaterials", "sceneRenderers", "allLoadedRenderers" })
                if ((int)counts[name] != (int)baseline[name]) throw new InvalidOperationException("Resource count changed: " + name);
            // UI font/material caches may warm after revision labels change. This
            // bounded allowance is explicit and is not a managed-memory plateau.
            if ((int)counts["allLoadedMaterials"] > (int)baseline["allLoadedMaterials"] + 8 ||
                (int)counts["allLoadedGameObjects"] > (int)baseline["allLoadedGameObjects"] + 32)
                throw new InvalidOperationException("Material or GameObject count exceeded the bounded warm-up allowance.");
        }

        [StructLayout(LayoutKind.Sequential)]
        struct RevisionProcessMemory
        {
            public uint size, pageFaultCount;
            public UIntPtr peakWorkingSetSize, workingSetSize, quotaPeakPagedPoolUsage, quotaPagedPoolUsage;
            public UIntPtr quotaPeakNonPagedPoolUsage, quotaNonPagedPoolUsage, pagefileUsage, peakPagefileUsage, privateUsage;
        }
        [DllImport("kernel32.dll", EntryPoint = "GetCurrentProcess")]
        static extern IntPtr RevisionCurrentProcess();
        [DllImport("psapi.dll", EntryPoint = "GetProcessMemoryInfo", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool RevisionGetProcessMemoryInfo(IntPtr process, ref RevisionProcessMemory counters, uint size);

        static JObject RevisionMemory()
        {
            var result = new JObject
            {
                ["unityAllocatedBytes"] = Profiler.GetTotalAllocatedMemoryLong(),
                ["unityReservedBytes"] = Profiler.GetTotalReservedMemoryLong(),
                ["managedUsedBytes"] = Profiler.GetMonoUsedSizeLong(),
                ["managedHeapBytes"] = Profiler.GetMonoHeapSizeLong(),
                ["workingSetBytes"] = JValue.CreateNull(), ["privateCommitBytes"] = JValue.CreateNull(),
                ["workingSetSource"] = "unavailable; external Windows process sampling required"
            };
            try
            {
                var counters = new RevisionProcessMemory { size = (uint)Marshal.SizeOf<RevisionProcessMemory>() };
                if (RevisionGetProcessMemoryInfo(RevisionCurrentProcess(), ref counters, counters.size) && counters.workingSetSize.ToUInt64() > 0)
                {
                    result["workingSetBytes"] = counters.workingSetSize.ToUInt64();
                    result["privateCommitBytes"] = counters.privateUsage.ToUInt64();
                    result["workingSetSource"] = "Windows GetProcessMemoryInfo PROCESS_MEMORY_COUNTERS_EX";
                }
                else result["workingSetReadError"] = Marshal.GetLastWin32Error();
            }
            catch (Exception e) { result["workingSetReadError"] = e.GetType().Name + ": " + e.Message; }
            return result;
        }
    }
}
