using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        IEnumerator RunSmoke(string output)
        {
            Directory.CreateDirectory(output);
            float deadline = UnityEngine.Time.realtimeSinceStartup + 180;
            while (Active == null && UnityEngine.Time.realtimeSinceStartup < deadline) yield return null;
            var checks = new List<string>(); string failure = null;
            if (Active == null) failure = "Initial load failed: " + LastErrorCode + " " + Status;
            else
            {
                try
                {
                    checks.Add("external-pack-loaded");
                    var garment = Active.Verified.Manifest.renderers.First(x => x.path.EndsWith("/PrototypeCollar", StringComparison.Ordinal));
                    SetVisible(garment.rendererId, false);
                    if (Active.Avatar.Renderers[garment.rendererId].enabled) throw new Exception("Visibility OFF not applied");
                    SetVisible(garment.rendererId, true);
                    if (!Active.Avatar.Renderers[garment.rendererId].enabled) throw new Exception("Visibility ON not applied");
                    checks.Add("renderer-toggle");
                    var bones = Active.Avatar.Root.GetComponentsInChildren<Transform>(true);
                    foreach (var row in Active.Verified.Manifest.renderers.Where(r => r.category == "body" || r.rendererId == garment.rendererId))
                    {
                        var flags = Active.Avatar.Renderers.ToDictionary(p => p.Key, p => p.Value.enabled);
                        var world = bones.Select(t => t.localToWorldMatrix).ToArray();
                        SetVisible(row.rendererId, !flags[row.rendererId]);
                        if (Active.Avatar.Renderers.Any(p => p.Value.enabled != (p.Key == row.rendererId ? !flags[p.Key] : flags[p.Key]))) throw new Exception("Toggle affected another renderer");
                        if (bones.Where((t,i) => t.localToWorldMatrix != world[i]).Any()) throw new Exception("Toggle changed a shared bone");
                        SetVisible(row.rendererId, flags[row.rendererId]);
                    }
                    checks.Add("body-clothing-toggle-isolation-and-shared-bones");
                    Edit(s => { s.motion.clipId = "pose-rest"; s.motion.timeSeconds = 0; });
                    var rest = bones.Select(t => t.localRotation).ToArray();
                    Edit(s => { s.motion.clipId = "pose-arms-up"; s.motion.timeSeconds = 1; });
                    var raised = bones.Select(t => t.localRotation).ToArray();
                    if (!raised.Where((q, i) => Quaternion.Angle(q, rest[i]) > 10).Any()) throw new Exception("Arms-up clip did not move any bone");
                    Edit(s => { s.motion.clipId = "pose-rest"; s.motion.timeSeconds = 0; });
                    if (bones.Where((t,i) => Quaternion.Angle(t.localRotation, rest[i]) > .05f).Any()) throw new Exception("Pose residual after returning to rest");
                    Edit(s => { s.motion.clipId = "pose-arms-up"; s.motion.timeSeconds = 1; });
                    if (bones.Where((t,i) => Quaternion.Angle(t.localRotation, raised[i]) > .05f).Any()) throw new Exception("Pose A-B-A is not deterministic");
                    Seek(.5); var seek = bones.Select(t => t.localRotation).ToArray();
                    Seek(.1); Seek(.5);
                    if (bones.Where((t,i) => Quaternion.Angle(t.localRotation, seek[i]) > .05f).Any()) throw new Exception("Stopped seek is not deterministic");
                    checks.Add("pose-motion-and-repeatability");
                    Edit(s => { s.motion.clipId = "pose-rest"; s.motion.timeSeconds = 1; });
                    var baselinePositions = bones.Select(t => t.localPosition).ToArray();
                    var hips = bones.Single(t => t.name == "Hips"); var hipsRest = hips.position;
                    Edit(s => { s.motion.clipId = "pose-crouch"; s.motion.timeSeconds = 1; });
                    if (Vector3.Distance(hips.position, hipsRest + Vector3.down * .22f) > .0001f) throw new Exception("Crouch displacement is not 0.22 world metres down");
                    Edit(s => { s.motion.clipId = "pose-rest"; s.motion.timeSeconds = 1; });
                    if (bones.Where((t,i) => Vector3.Distance(t.localPosition, baselinePositions[i]) > .000001f).Any()) throw new Exception("Position residual after crouch to rest");
                    checks.Add("crouch-world-displacement-and-position-reset");
                    Edit(s => { s.motion.clipId = "pose-arms-up"; s.motion.timeSeconds = .5; });
                    var morph = Active.Verified.Manifest.morphBindings.First(x => x.shapeName == "Shrink_Neck");
                    SetMorph(morph.rendererId, morph.shapeName, 15);
                    Seek(.5);
                    if (Math.Abs(Active.Avatar.MorphValue(morph.rendererId, morph.shapeName) - 15) > .01) throw new Exception("Manual morph lost after seek");
                    checks.Add("manual-morph-after-seek");
                    SetMorph(morph.rendererId, morph.shapeName, null);
                    if (Math.Abs(Active.Avatar.MorphValue(morph.rendererId, morph.shapeName) - morph.defaultWeight) > .01) throw new Exception("Morph reset failed for transform-only test clip");
                    SetMorph(morph.rendererId, morph.shapeName, 15);
                    checks.Add("morph-override-reset");
                    var originalMaterials = Active.Avatar.Renderers.ToDictionary(p => p.Key, p => p.Value.sharedMaterials);
                    Edit(s => s.preview.mode = "bodyDiagnostic");
                    Edit(s => s.preview.mode = "original");
                    if (Active.Avatar.Renderers.Any(p => !p.Value.sharedMaterials.SequenceEqual(originalMaterials[p.Key]))) throw new Exception("Original material references were not restored");
                    checks.Add("diagnostic-material-restore");
                    CameraPreset("all");
                    Save(Path.Combine(output, "smoke.viewer.json"));
                    var saved = JsonFiles.Read<SessionDocument>(Path.Combine(output, "smoke.viewer.json"));
                    if (saved.motion.timeSeconds != .5) throw new Exception("Saved time incorrect");
                    checks.Add("session-save");
                }
                catch (Exception e) { failure = e.ToString(); }
                yield return new WaitForEndOfFrame();
                ScreenCapture.CaptureScreenshot(Path.Combine(output, "viewer.png"));
                yield return new WaitForSecondsRealtime(1);
            }
            var report = new
            {
                success = failure == null, checks, failure,
                unity = Application.unityVersion, graphics = SystemInfo.graphicsDeviceType.ToString(), gpu = SystemInfo.graphicsDeviceName,
                allocatedBytes = Profiler.GetTotalAllocatedMemoryLong(), residentBytes = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64,
                frameTime = UnityEngine.Time.unscaledDeltaTime,
                rendererCount = Active?.Verified.Manifest.renderers.Length ?? 0,
                shaderNames = Active?.Avatar.Renderers.Values.SelectMany(r => r.sharedMaterials).Select(m => m.shader.name).Distinct().ToArray(),
                scope = "Initial runtime smoke only; full acceptance not yet verified"
            };
            File.WriteAllText(Path.Combine(output, "smoke.json"), Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented));
            Debug.Log("VIEWER_SMOKE_FINISHED " + (failure == null ? "PASS" : "FAIL"));
            if (Arg("--smoke-exit") == "true") Application.Quit(failure == null ? 0 : 1);
        }
    }
}
