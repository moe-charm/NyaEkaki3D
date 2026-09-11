using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        sealed class VisualPoseRow
        {
            public string path;
            public float[] localPosition, localRotation, localScale, worldPosition, worldRotation;
        }
        sealed class VisualMeshRow
        {
            public string rendererId, path, category, boundsSource;
            public bool visible;
            public int vertices;
            public float[] worldMin, worldMax, rendererLossyScale;
            public string[] shaders;
        }
        sealed class VisualImageRow
        {
            public string name, image, session, metrics, clipId, mode, framing;
            public double timeSeconds;
            public int width, height, changedTransformsFromRest;
            public float maximumRotationFromRestDegrees;
        }
        readonly List<VisualImageRow> visualImages = new List<VisualImageRow>();
        readonly List<string> visualChecks = new List<string>();
        readonly Dictionary<string, string> visualSourceHashes = new Dictionary<string, string>();
        Dictionary<string, Quaternion> visualRestRotations;
        Dictionary<string, Vector3> visualRestPositions;
        object visualDuplicateMorph = new { status = "not-run" };
        string visualPhase = "startup";

        IEnumerator RunVisualChecks(string output)
        {
            string failure = null, restorationError = null;
            SessionDocument before = null; bool beforeDirty = false;
            string beforeSessionPath = null, beforeSessionHash = null;
            var routines = new Stack<IEnumerator>();
            try
            {
                output = Path.GetFullPath(output); Directory.CreateDirectory(output);
                if (Arg("--choker-check") == "true")
                {
                    VisualRequire(Arg("--settings-root") != null && string.Equals(settingsRoot, Path.Combine(output, "settings"), StringComparison.OrdinalIgnoreCase),
                        "Choker checks require --settings-root <output>/settings; user settings are never a test destination");
                    VisualRequire(!Directory.Exists(setsDirectory) || !Directory.EnumerateFileSystemEntries(setsDirectory).Any(),
                        "Use a fresh choker output/settings directory; existing sets will not be overwritten");
                }
                routines.Push(VisualChecksSuite(output));
            }
            catch (Exception e) { failure = e.ToString(); }
            while (routines.Count > 0 && failure == null)
            {
                bool more = false; object yielded = null;
                try
                {
                    if (before == null && Active?.Avatar != null && !IsBusy && !reloadLoop)
                    { before = Snapshot(); beforeDirty = Dirty; beforeSessionPath = sessionPath; beforeSessionHash = sessionHash; }
                    more = routines.Peek().MoveNext(); if (more) yielded = routines.Peek().Current;
                }
                catch (Exception e) { failure = e.ToString(); }
                if (failure != null) break;
                if (!more) { (routines.Pop() as IDisposable)?.Dispose(); continue; }
                if (yielded is IEnumerator nested) routines.Push(nested); else yield return yielded;
            }
            while (routines.Count > 0) (routines.Pop() as IDisposable)?.Dispose();
            if (before != null)
            {
                try
                {
                    VisualRequire(Active?.Avatar != null && !IsBusy && Active.Verified.Hash == before.pack.manifestSha256, "Pack changed during visual capture; original state cannot safely be restored");
                    Active.Avatar.Apply(before, before.motion.timeSeconds);
                    Document = before; TimeSeconds = before.motion.timeSeconds; IsPlaying = false; Dirty = beforeDirty;
                    sessionPath = beforeSessionPath; sessionHash = beforeSessionHash;
                    ApplyCameraAndLight(); RebuildControls();
                }
                catch (Exception e) { restorationError = e.ToString(); }
            }
            try
            {
                foreach (var file in visualSourceHashes)
                    VisualRequire(File.Exists(file.Key) && JsonFiles.Sha256(file.Key) == file.Value, "Original source file changed: " + file.Key);
            }
            catch (Exception e) { failure = (failure == null ? "" : failure + "\n") + e; }
            bool recorded = failure == null && restorationError == null;
            var report = new
            {
                captureCompleted = recorded, phase = visualPhase, failure, restorationError,
                unity = Application.unityVersion, graphicsApi = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName,
                developmentBuild = Debug.isDebugBuild, sourcePack = before?.pack, images = visualImages,
                scriptChecks = visualChecks, duplicateMorphRendererIsolation = visualDuplicateMorph,
                originalSourceHashesVerified = visualSourceHashes.Count,
                visualAcceptance = "NOT_AUTOMATICALLY_PASSED: inspect every saved PNG for pose quality, body/collar deformation, clipping, shader appearance, stencil, transparency and outline.",
                notes = new[] {
                    "Full-body images use one camera fitted to the union of Renderer.bounds for the five poses; their framing is identical. These are conservative renderer bounds, not measured skinned vertex bounds.",
                    "Each image waits two frames plus 0.25 real seconds before capture; the file is then checked for a PNG header and nonzero dimensions.",
                    "Bone coordinates are actual Transform world values. Bounds use Unity Renderer.bounds directly; BakeMesh plus TransformPoint was rejected after scale100 imported clothing produced incorrect bounds in visual-02.",
                    "A02 is passed only when a physical duplicate shape is available through two distinct registered renderers and isolation is measured. Absence is reported as fixture-missing.",
                    "Saved sessions and metrics accompany images; no source FBX, prefab, material, manifest or bundle is modified. Original live adjustments are restored in stopped state." }
            };
            try { File.WriteAllText(Path.Combine(output, "visual-checks.json"), JsonConvert.SerializeObject(report, Formatting.Indented)); }
            catch (Exception e) { failure = e.ToString(); recorded = false; Debug.LogException(e); }
            Debug.Log("VIEWER_VISUAL_CHECKS_FINISHED " + (recorded ? "RECORDED" : "FAILED") + " " + visualPhase);
            if (Arg("--visual-exit") == "true") Application.Quit(recorded ? 0 : 1);
        }

        IEnumerator VisualChecksSuite(string output)
        {
            visualPhase = "initial-load";
            float deadline = UnityEngine.Time.realtimeSinceStartup + 180;
            while ((Active?.Avatar == null || IsBusy || reloadLoop) && UnityEngine.Time.realtimeSinceStartup < deadline) yield return null;
            VisualRequire(Active?.Avatar != null && !IsBusy && !reloadLoop, "Initial load failed: " + LastErrorCode + " " + Status);
            VisualRequire(Application.platform == RuntimePlatform.WindowsPlayer, "Visual evidence must be captured from the Windows Player");
            Pause();
            visualSourceHashes.Add(Active.Verified.Path, JsonFiles.Sha256(Active.Verified.Path));
            foreach (var bundle in Active.Verified.Manifest.bundles)
            {
                string file = JsonFiles.PackChild(Active.Verified.Directory, bundle.path);
                visualSourceHashes.Add(file, JsonFiles.Sha256(file));
            }
            var poses = new[] { "pose-rest", "pose-arms-up", "pose-elbows", "pose-crouch", "pose-leg-up" };
            foreach (string clip in poses)
                VisualRequire(Active.Verified.Manifest.clips.Any(c => c.clipId == clip && c.durationSeconds >= 1), "Required 1-second pose unavailable: " + clip);
            Edit(s =>
            {
                s.preview.mode = "original"; s.preview.lightPresetId = "studio";
                s.motion.loop = false; s.motion.speed = 1; s.motion.timeSeconds = 1;
                s.visibilityOverrides = Active.Verified.Manifest.renderers.Select(r => new VisibilityOverride { rendererId = r.rendererId, visible = Arg("--choker-check") == "true" ? r.category == "body" || VisualPrototype(r) : r.defaultVisible || VisualPrototype(r) }).ToArray();
                s.morphOverrides = Array.Empty<MorphOverride>(); s.unresolvedOverrides = Array.Empty<UnresolvedOverride>();
            });
            VisualRequire(LastErrorCode == "", "Visual setup rejected: " + Status);
            yield return null; yield return null;
            visualPhase = "measure-common-framing";
            bool anyBounds = false; Bounds union = default;
            foreach (string clip in poses)
            {
                Edit(s => { s.motion.clipId = clip; s.motion.timeSeconds = 1; });
                VisualRequire(Document.motion.clipId == clip && LastErrorCode == "", "Pose selection rejected: " + clip);
                // Unity refreshes skinned renderer bounds during the frame. Sampling
                // immediately after manual graph evaluation can return the prior pose.
                yield return null; yield return null;
                var rows = VisualMeasureMeshes();
                foreach (var row in rows.Where(r => r.visible && r.vertices > 0))
                {
                    var b = new Bounds(); b.SetMinMax(VisualVector(row.worldMin), VisualVector(row.worldMax));
                    if (anyBounds) union.Encapsulate(b); else { union = b; anyBounds = true; }
                }
            }
            VisualRequire(anyBounds && union.size.y > .1f, "No measurable renderer for full-body framing");
            VisualRequire(union.center.magnitude < 10 && union.size.magnitude < 10, "Pose bounds exceed the expected local RadDollV3 range; inspect imported scale and clip translations before accepting screenshots");
            var fullCamera = VisualFittedCamera(union, Quaternion.Euler(0, 180, 0), 35, 1.12f);
            Edit(s => s.camera = JsonFiles.Clone(fullCamera));
            for (int index = 0; index < poses.Length; index++)
            {
                string clip = poses[index];
                Edit(s => { s.motion.clipId = clip; s.motion.timeSeconds = 1; s.camera = JsonFiles.Clone(fullCamera); });
                VisualRequire(LastErrorCode == "" && Document.motion.clipId == clip, "Pose capture setup failed");
                yield return VisualCapture(output, (index + 1).ToString("D2") + "-" + clip, "identical-full-body");
            }
            visualChecks.Add("All five 1-second poses captured with identical serialized camera and finite Unity renderer world bounds; each non-rest pose moved at least one transform. Bounds are conservative, not baked vertex evidence.");
            VisualCheckDuplicateMorph();

            visualPhase = "neck-original";
            var prototype = Active.Verified.Manifest.renderers.Where(VisualPrototype).ToArray();
            VisualRequire(prototype.Length >= 2, "Custom collar and ribbon renderers are required for neck evidence");
            Edit(s =>
            {
                s.motion.clipId = "pose-arms-up"; s.motion.timeSeconds = 1;
                s.visibilityOverrides = Active.Verified.Manifest.renderers.Select(r => new VisibilityOverride
                { rendererId = r.rendererId, visible = VisualPrototype(r) || (Arg("--choker-check") == "true" ? r.category == "body" : r.category != "outfit" && r.defaultVisible) }).ToArray();
                s.preview.mode = "original";
            });
            yield return null; yield return null;
            var neckRows = VisualMeasureMeshes().Where(r => prototype.Any(p => p.rendererId == r.rendererId)).ToArray();
            var collarBounds = new Bounds(); bool collarMeasured = false;
            foreach (var row in neckRows)
            {
                var b = new Bounds(); b.SetMinMax(VisualVector(row.worldMin), VisualVector(row.worldMax));
                if (collarMeasured) collarBounds.Encapsulate(b); else { collarBounds = b; collarMeasured = true; }
            }
            VisualRequire(collarMeasured && collarBounds.size.magnitude > .02f, "Custom collar has no measurable world bounds");
            var neckBone = Active.Avatar.Root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.Equals("Neck", StringComparison.OrdinalIgnoreCase));
            if (neckBone) collarBounds.Encapsulate(neckBone.position);
            // Extra vertical room shows the neck and shoulders around the test garment.
            collarBounds.Expand(new Vector3(.04f, .10f, .02f));
            var neckCamera = VisualFittedCamera(collarBounds, Quaternion.Euler(0, 180, 0), 35, 1.15f);
            Edit(s => s.camera = neckCamera);
            var originalMaterials = Active.Avatar.Renderers.ToDictionary(p => p.Key, p => p.Value.sharedMaterials);
            yield return VisualCapture(output, "06-neck-original", "fixed-neck");
            Edit(s => s.preview.mode = "bodyDiagnostic");
            VisualRequire(Active.Verified.Manifest.renderers.Where(r => r.category == "body").All(r => Active.Avatar.Renderers[r.rendererId].sharedMaterials.All(m => m && m.shader.name == "Viewer/BodyDiagnostic")), "Diagnostic body shader was not applied");
            yield return VisualCapture(output, "07-neck-diagnostic", "fixed-neck");
            Edit(s => s.preview.mode = "original");
            VisualRequire(Active.Avatar.Renderers.All(p => p.Value.sharedMaterials.SequenceEqual(originalMaterials[p.Key])), "Original shared material references failed to restore");
            yield return VisualCapture(output, "08-neck-original-restored", "fixed-neck");
            visualChecks.Add("A19: bodyDiagnostic shader applied to body renderers, original shared material references restored exactly; original/diagnostic/restored PNGs saved with identical neck camera.");
            if (Arg("--choker-check") == "true")
            {
                foreach (var view in new[] { ("front", 180f), ("side", 90f), ("back", 0f) })
                {
                    Edit(s => { s.motion.clipId = "pose-rest"; s.motion.timeSeconds = 0; s.camera = VisualFittedCamera(collarBounds, Quaternion.Euler(0, view.Item2, 0), 35, 1.25f); });
                    yield return VisualCapture(output, "09-choker-" + view.Item1, "choker-detail");
                }
                foreach (string clip in new[] { "pose-neck-turn", "pose-neck-nod" })
                {
                    Edit(s => { s.motion.clipId = clip; s.motion.timeSeconds = 1; s.camera = JsonFiles.Clone(neckCamera); });
                    yield return VisualCapture(output, "10-" + clip, "choker-detail");
                }
                Edit(s => { s.motion.clipId = "pose-rest"; s.motion.timeSeconds = 0; s.camera = JsonFiles.Clone(neckCamera); });
                SaveNamedSet("チョーカー試着");
                VisualRequire(!Dirty && File.Exists(sessionPath), "Choker named set save failed");
                Edit(s => s.visibilityOverrides = Active.Verified.Manifest.renderers.Select(r => new VisibilityOverride
                { rendererId = r.rendererId, visible = r.category == "body" || VisualPrototype(r) || r.category == "accessory" && r.defaultVisible }).ToArray());
                SaveNamedSet("チョーカー試着（髪あり）");
                VisualRequire(!Dirty && File.Exists(sessionPath), "Hair presentation set save failed");
                yield return VisualCapture(output, "11-choker-hair", "choker-detail");
            }
            visualPhase = "finished";
        }

        IEnumerator VisualCapture(string output, string name, string framing)
        {
            visualPhase = "capture-" + name;
            VisualRequire(!IsPlaying && !IsBusy && Active?.Avatar != null, "Capture attempted without a stopped avatar");
            SetStatus("ポーズ確認画像を保存しています · " + name);
            yield return null; yield return null; yield return new WaitForSecondsRealtime(.25f);
            if (framing == "identical-full-body") VisualRequireFullFrame();
            if (framing == "choker-detail") VisualRequireFullFrame(true);
            var poseRows = Active.Avatar.Root.GetComponentsInChildren<Transform>(true).Select(t => new VisualPoseRow
            {
                path = VisualTransformPath(t), localPosition = VisualArray(t.localPosition), localRotation = VisualArray(t.localRotation),
                localScale = VisualArray(t.localScale), worldPosition = VisualArray(t.position), worldRotation = VisualArray(t.rotation)
            }).ToArray();
            if (visualRestRotations == null)
            {
                VisualRequire(Document.motion.clipId == "pose-rest", "First capture must establish rest baseline");
                visualRestRotations = poseRows.ToDictionary(p => p.path, p => VisualQuaternion(p.localRotation));
                visualRestPositions = poseRows.ToDictionary(p => p.path, p => VisualVector(p.localPosition));
            }
            int changed = 0; float maxAngle = 0;
            foreach (var row in poseRows)
            {
                float angle = Quaternion.Angle(visualRestRotations[row.path], VisualQuaternion(row.localRotation));
                maxAngle = Mathf.Max(maxAngle, angle);
                if (angle > .1f || Vector3.Distance(visualRestPositions[row.path], VisualVector(row.localPosition)) > .0001f) changed++;
            }
            if (Document.motion.clipId != "pose-rest") VisualRequire(changed > 0 && maxAngle > 1, "Pose did not move physical transforms: " + Document.motion.clipId);
            string sessionFile = Path.Combine(output, name + ".viewer.json");
            var session = Snapshot();
            session.pack.manifestPath = Path.GetRelativePath(Path.GetDirectoryName(sessionFile), Active.Verified.Path).Replace('\\', '/');
            JsonFiles.AtomicWrite(sessionFile, session);
            var meshes = VisualMeasureMeshes();
            string metricsFile = Path.Combine(output, name + ".metrics.json");
            File.WriteAllText(metricsFile, JsonConvert.SerializeObject(new
            {
                name, framing, session = Snapshot(), transforms = poseRows, renderers = meshes,
                changedTransformsFromRest = changed, maximumRotationFromRestDegrees = maxAngle,
                cameraWorldPosition = VisualArray(previewCamera.transform.position), cameraWorldRotation = VisualArray(previewCamera.transform.rotation),
                cameraPixelRect = new[] { previewCamera.pixelRect.x, previewCamera.pixelRect.y, previewCamera.pixelRect.width, previewCamera.pixelRect.height },
                cameraNearClipMeters = previewCamera.nearClipPlane, cameraFarClipMeters = previewCamera.farClipPlane,
                coordinates = "Renderer.bounds are Unity's conservative world-space culling bounds. Actual Transform local/world values are labelled. Baked vertex world-space conversion is intentionally not claimed."
            }, Formatting.Indented));
            string imageFile = Path.Combine(output, name + ".png");
            VisualRequire(!File.Exists(imageFile), "Use a new output directory; image already exists: " + imageFile);
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(imageFile);
            float deadline = UnityEngine.Time.realtimeSinceStartup + 20;
            while ((!File.Exists(imageFile) || new FileInfo(imageFile).Length < 32) && UnityEngine.Time.realtimeSinceStartup < deadline) yield return null;
            VisualRequire(File.Exists(imageFile) && new FileInfo(imageFile).Length >= 32, "Screenshot file did not arrive: " + imageFile);
            yield return null; // CaptureScreenshot's deferred write has returned to the main loop.
            byte[] header = new byte[24];
            using (var png = File.OpenRead(imageFile)) VisualRequire(png.Read(header, 0, header.Length) == header.Length, "PNG header truncated");
            VisualRequire(header[0] == 137 && header[1] == 80 && header[2] == 78 && header[3] == 71, "Screenshot is not PNG");
            int Dimension(int offset) => header[offset] << 24 | header[offset + 1] << 16 | header[offset + 2] << 8 | header[offset + 3];
            int width = Dimension(16), height = Dimension(20);
            VisualRequire(width == Screen.width && height == Screen.height && width > 0 && height > 0, "Screenshot dimensions differ from actual Player screen");
            visualImages.Add(new VisualImageRow { name = name, image = imageFile, session = sessionFile, metrics = metricsFile,
                clipId = Document.motion.clipId, timeSeconds = TimeSeconds, mode = Document.preview.mode, framing = framing,
                width = width, height = height, changedTransformsFromRest = changed, maximumRotationFromRestDegrees = maxAngle });
            Debug.Log("VIEWER_VISUAL_CAPTURED " + name + " " + width + "x" + height);
        }

        void VisualCheckDuplicateMorph()
        {
            visualPhase = "A02-duplicate-morph";
            var physical = new List<(string rendererId, string name, SkinnedMeshRenderer renderer, int index)>();
            foreach (var pair in Active.Avatar.Renderers)
                if (pair.Value is SkinnedMeshRenderer r)
                    for (int i = 0; i < r.sharedMesh.blendShapeCount; i++) physical.Add((pair.Key, r.sharedMesh.GetBlendShapeName(i), r, i));
            var groups = physical.GroupBy(p => p.name).Where(g => g.Select(p => p.rendererId).Distinct().Count() > 1).ToArray();
            var candidate = groups.FirstOrDefault(g => g.Count(p => Active.Verified.Manifest.morphBindings.Any(m => m.rendererId == p.rendererId && m.shapeName == p.name && m.maxWeight > m.minWeight)) >= 2);
            if (candidate == null)
            {
                visualDuplicateMorph = new { status = "fixture-missing", physicalDuplicateShapeNames = groups.Select(g => g.Key).ToArray(),
                    reason = "No same physical shapeName is exposed as an adjustable binding on two distinct renderers. A02 remains unverified; it is not counted as passed." };
                return;
            }
            var rows = candidate.ToArray();
            var target = rows.First(p => Active.Verified.Manifest.morphBindings.Any(m => m.rendererId == p.rendererId && m.shapeName == p.name && m.maxWeight > m.minWeight));
            var binding = Active.Verified.Manifest.morphBindings.First(m => m.rendererId == target.rendererId && m.shapeName == target.name);
            var before = rows.Select(p => p.renderer.GetBlendShapeWeight(p.index)).ToArray();
            var previous = Document.morphOverrides.FirstOrDefault(m => m.rendererId == target.rendererId && m.shapeName == target.name);
            float value = binding.minWeight + (binding.maxWeight - binding.minWeight) * .37f;
            if (Math.Abs(value - target.renderer.GetBlendShapeWeight(target.index)) < .01f) value = binding.minWeight + (binding.maxWeight - binding.minWeight) * .71f;
            SetMorph(target.rendererId, target.name, value);
            VisualRequire(LastErrorCode == "", "Duplicate morph test edit rejected");
            var after = rows.Select(p => p.renderer.GetBlendShapeWeight(p.index)).ToArray();
            for (int i = 0; i < rows.Length; i++)
                VisualRequire(Math.Abs(after[i] - (rows[i].rendererId == target.rendererId ? value : before[i])) < .01f, "Duplicate shape edit escaped target renderer: " + rows[i].rendererId);
            SetMorph(target.rendererId, target.name, previous == null ? (float?)null : previous.weight);
            for (int i = 0; i < rows.Length; i++) VisualRequire(Math.Abs(rows[i].renderer.GetBlendShapeWeight(rows[i].index) - before[i]) < .01f, "Duplicate morph test failed to restore physical values");
            visualDuplicateMorph = new { status = "passed", shapeName = target.name, targetRendererId = target.rendererId, requestedWeight = value,
                rendererIds = rows.Select(p => p.rendererId).ToArray(), beforeWeights = before, afterWeights = after,
                physicalDuplicateShapeNames = groups.Select(g => g.Key).ToArray(), evidence = "Public SetMorph changed only the selected renderer; all same-name physical mesh weights in other renderers remained unchanged and original weights were restored." };
            visualChecks.Add("A02: physically duplicated shapeName edited through renderer-specific SetMorph; other renderers unchanged, all original weights restored.");
        }

        List<VisualMeshRow> VisualMeasureMeshes()
        {
            var rows = new List<VisualMeshRow>();
            foreach (var binding in Active.Verified.Manifest.renderers)
            {
                var renderer = Active.Avatar.Renderers[binding.rendererId];
                Mesh mesh = renderer is SkinnedMeshRenderer skin ? skin.sharedMesh : renderer.GetComponent<MeshFilter>()?.sharedMesh;
                VisualRequire(mesh != null && mesh.vertexCount > 0, "Renderer has no measurable mesh: " + binding.path);
                // Imported clothing keeps a scale100 renderer with a transformed skeleton.
                // BakeMesh(false) followed by renderer.TransformPoint did not match actual
                // rendered coordinates. Use the same renderer bounds as the working Viewer
                // framing, and expose that these are conservative rather than vertex-tight.
                Bounds world = renderer.bounds;
                foreach (float value in VisualArray(world.min).Concat(VisualArray(world.max)))
                    VisualRequire(Validation.Finite(value), "Nonfinite renderer bounds: " + binding.path);
                rows.Add(new VisualMeshRow { rendererId = binding.rendererId, path = binding.path, category = binding.category,
                    visible = renderer.enabled && renderer.gameObject.activeInHierarchy, vertices = mesh.vertexCount,
                    worldMin = VisualArray(world.min), worldMax = VisualArray(world.max), rendererLossyScale = VisualArray(renderer.transform.lossyScale),
                    boundsSource = "Unity Renderer.bounds (conservative world-space culling bounds; not baked vertices)",
                    shaders = renderer.sharedMaterials.Select(m => m && m.shader ? m.shader.name : "MISSING").ToArray() });
            }
            return rows;
        }
        CameraState VisualFittedCamera(Bounds bounds, Quaternion rotation, float fov, float margin)
        {
            var inverse = Quaternion.Inverse(rotation); var extents = Vector3.zero;
            for (int i = 0; i < 8; i++)
            {
                var local = inverse * new Vector3((i & 1) == 0 ? -bounds.extents.x : bounds.extents.x, (i & 2) == 0 ? -bounds.extents.y : bounds.extents.y, (i & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                extents = Vector3.Max(extents, new Vector3(Mathf.Abs(local.x), Mathf.Abs(local.y), Mathf.Abs(local.z)));
            }
            float halfFov = Mathf.Tan(fov * Mathf.Deg2Rad * .5f);
            float distance = Mathf.Max(extents.y / halfFov, extents.x / (halfFov * Mathf.Max(.1f, previewCamera.aspect))) * margin + extents.z;
            VisualRequire(Validation.Finite(distance) && distance - extents.z > previewCamera.nearClipPlane && distance + extents.z < previewCamera.farClipPlane,
                "Fitted bounds exceed the actual Camera near/far planes; refusing empty or clipped screenshots");
            return new CameraState { targetMeters = VisualArray(bounds.center), orientationQuat = VisualArray(rotation), verticalFovDegrees = fov,
                distanceMeters = distance };
        }
        void VisualRequireFullFrame(bool chokerOnly = false)
        {
            foreach (var renderer in Active.Avatar.Renderers.Values.Where(r => r.enabled && r.gameObject.activeInHierarchy &&
                (!chokerOnly || r.name == "PrototypeCollar" || r.name == "PrototypeRibbon")))
            {
                Bounds bounds = renderer.bounds;
                for (int i = 0; i < 8; i++)
                {
                    var corner = bounds.center + new Vector3((i & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
                        (i & 2) == 0 ? -bounds.extents.y : bounds.extents.y, (i & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
                    var point = previewCamera.WorldToViewportPoint(corner);
                    VisualRequire(point.x >= 0 && point.x <= 1 && point.y >= 0 && point.y <= 1 &&
                        point.z > previewCamera.nearClipPlane && point.z < previewCamera.farClipPlane,
                        "Current renderer bounds are clipped in common full-body camera: " + renderer.name + " during " + Document.motion.clipId);
                }
            }
        }
        string VisualTransformPath(Transform t)
        {
            if (t == Active.Avatar.Root.transform) return ".";
            var names = new List<string>();
            while (t != Active.Avatar.Root.transform) { names.Add(t.name); t = t.parent; }
            names.Reverse(); return string.Join("/", names);
        }
        static bool VisualPrototype(RendererRecord r) => r.path.EndsWith("/PrototypeCollar", StringComparison.Ordinal) || r.path.EndsWith("/PrototypeRibbon", StringComparison.Ordinal);
        static float[] VisualArray(Vector3 value) => new[] { value.x, value.y, value.z };
        static float[] VisualArray(Quaternion value) => new[] { value.x, value.y, value.z, value.w };
        static Vector3 VisualVector(float[] value) => new Vector3(value[0], value[1], value[2]);
        static Quaternion VisualQuaternion(float[] value) => new Quaternion(value[0], value[1], value[2], value[3]);
        static void VisualRequire(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
