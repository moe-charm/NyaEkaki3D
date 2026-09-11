using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        // Opt-in Player acceptance. Only files created inside the supplied output directory
        // may be corrupted. Source revisions remain immutable throughout these checks.
        sealed class AcceptanceCheck
        {
            public string id, evidence;
        }
        sealed class AcceptancePose
        {
            public Dictionary<string, Vector3> positions, scales;
            public Dictionary<string, Quaternion> rotations;
        }
        readonly List<AcceptanceCheck> acceptanceChecks = new List<AcceptanceCheck>();
        readonly Dictionary<string, string> acceptanceOriginalHashes = new Dictionary<string, string>();
        string acceptancePhase = "startup";
        string acceptanceFixtureRoot;
        VerifiedPack acceptanceSource;

        IEnumerator RunAcceptance(string output)
        {
            output = Path.GetFullPath(output);
            string failure = null;
            bool resume = Arg("--acceptance-resume") == "true";
            bool missingSession = Arg("--acceptance-missing-session") == "true";
            var stack = new Stack<IEnumerator>();
            try
            {
                Directory.CreateDirectory(output);
                AcceptanceRequire(!(resume && missingSession), "Choose either resume or missing-session acceptance mode, not both");
                stack.Push(missingSession ? AcceptanceMissingSession(output) : resume ? AcceptanceResume(output) : AcceptanceSuite(output));
            }
            catch (Exception e) { failure = e.ToString(); }
            // Drive nested test enumerators here so an assertion/timeout always writes a
            // terminal report. Unity's separate reload coroutine is checked by state + deadline.
            while (stack.Count > 0 && failure == null)
            {
                bool more = false; object current = null;
                try { more = stack.Peek().MoveNext(); if (more) current = stack.Peek().Current; }
                catch (Exception e) { failure = e.ToString(); }
                if (failure != null) break;
                if (!more) { (stack.Pop() as IDisposable)?.Dispose(); continue; }
                if (current is IEnumerator nested) stack.Push(nested);
                else yield return current;
            }
            while (stack.Count > 0) (stack.Pop() as IDisposable)?.Dispose();
            try
            {
                foreach (var source in acceptanceOriginalHashes)
                    AcceptanceRequire(File.Exists(source.Key) && JsonFiles.Sha256(source.Key) == source.Value, "Immutable source changed: " + source.Key);
            }
            catch (Exception e) { failure = (failure == null ? "" : failure + "\n") + e; }
            var report = new
            {
                success = failure == null, phase = acceptancePhase, failure, checks = acceptanceChecks,
                unity = Application.unityVersion, graphics = SystemInfo.graphicsDeviceType.ToString(),
                gpu = SystemInfo.graphicsDeviceName, developmentBuild = Debug.isDebugBuild,
                sourceManifest = acceptanceSource?.Path, sourceManifestSha256 = acceptanceSource?.Hash,
                fixtureRoot = acceptanceFixtureRoot, completedReloads = CompletedReloads,
                lastErrorCode = LastErrorCode, status = Status,
                originalFilesVerifiedUnchanged = acceptanceOriginalHashes.Count,
                scope = missingSession ? "Fresh Player process: missing saved revision retains session in Recovery, Save As and explicit compatible relink" : resume ? "Second Player process: saved session restored and stopped" : "Real Player fault/reload/session acceptance; fixture manifests reuse source bundle content",
                exclusions = missingSession ? new[] { "This verifies startup with a missing manifest path; corrupted existing bundles and old-active double failure are covered by the separate primary acceptance run", "This does not prove visual material quality or performance budgets" } : resume ? new[] { "Separate primary acceptance.json must also pass" } : new[]
                {
                    "A07 real Blender geometry change and A22 twenty distinct-content revisions are not covered by metadata fixtures",
                    "A16 actual process restart requires a second launch with --session <output>/sessions/second/roundtrip.viewer.json --acceptance-resume true",
                    "A10/A24 remove and restore the advertised morph binding; the underlying mesh retains the shape",
                    "A21 native focus/minimize/power, performance budgets, and visual material/penetration acceptance are separate"
                }
            };
            try { File.WriteAllText(Path.Combine(output, missingSession ? "acceptance-missing-session.json" : resume ? "acceptance-resume.json" : "acceptance.json"), Newtonsoft.Json.JsonConvert.SerializeObject(report, Newtonsoft.Json.Formatting.Indented)); }
            catch (Exception e) { failure = e.ToString(); Debug.LogException(e); }
            Debug.Log("VIEWER_ACCEPTANCE_FINISHED " + (failure == null ? "PASS" : "FAIL") + " " + acceptancePhase);
            if (Arg("--acceptance-exit") == "true") Application.Quit(failure == null ? 0 : 1);
        }

        IEnumerator AcceptanceSuite(string output)
        {
            yield return AcceptanceWait(() => Active?.Avatar != null && !reloadLoop && !IsBusy, "initial pack load");
            acceptanceSource = Active.Verified;
            acceptanceOriginalHashes.Add(acceptanceSource.Path, JsonFiles.Sha256(acceptanceSource.Path));
            foreach (var bundle in acceptanceSource.Manifest.bundles)
            {
                string path = JsonFiles.PackChild(acceptanceSource.Directory, bundle.path);
                acceptanceOriginalHashes.Add(path, JsonFiles.Sha256(path));
            }
            acceptanceFixtureRoot = Path.Combine(output, "fixtures-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(acceptanceFixtureRoot);
            foreach (var bundle in acceptanceSource.Manifest.bundles)
            {
                string destination = JsonFiles.PackChild(acceptanceFixtureRoot, bundle.path);
                Directory.CreateDirectory(Path.GetDirectoryName(destination));
                File.Copy(JsonFiles.PackChild(acceptanceSource.Directory, bundle.path), destination, false);
            }
            var fixtureBase = JsonFiles.Clone(acceptanceSource.Manifest);
            string good = AcceptanceManifest(fixtureBase, "good");
            var garment = fixtureBase.renderers.First(r => r.path.EndsWith("/PrototypeCollar", StringComparison.Ordinal));
            var morph = fixtureBase.morphBindings.First(m => m.shapeName == "Shrink_Neck" && !m.required);
            Pause();
            Edit(s => { s.motion.clipId = "pose-arms-up"; s.motion.timeSeconds = .7; s.motion.speed = .6; s.motion.loop = false; s.preview.lightPresetId = "outdoor"; });
            SetMorph(morph.rendererId, morph.shapeName, 15);
            SetVisible(garment.rendererId, false);
            CameraPreset("neck");
            AcceptanceRequire(Document.motion.clipId == "pose-arms-up" && Math.Abs(TimeSeconds - .7) < .00001, "Acceptance state setup failed: " + LastErrorCode);
            var baseline = Snapshot(); var pose = AcceptanceCapturePose();
            int completed = CompletedReloads;
            RequestReload(acceptanceSource.Path);
            yield return AcceptanceIdle("same revision reload");
            AcceptanceRequire(CompletedReloads == completed + 1 && Active.Verified.Hash == acceptanceSource.Hash, "Same revision was not reloaded");
            AcceptanceEquivalent(baseline, Snapshot(), false);
            AcceptancePoseMatches(pose); AcceptanceApplied(morph, garment, 15, false);
            AcceptancePass("A07-prerequisite", "Same source revision reloaded through real AssetBundles; camera, stopped .7 s arms-up pose, morph 15 and collar visibility retained. Real geometry-change acceptance is separate.");

            var target = JsonFiles.Clone(fixtureBase); target.toolchain.buildTarget = "StandaloneOSX";
            yield return AcceptanceReject(AcceptanceManifest(target, "wrong-target"), "PACK_TARGET_MISMATCH", "A11-target");
            var token = JsonFiles.Clone(fixtureBase); token.toolchain.compatibilityToken = "unrelated-toolchain";
            yield return AcceptanceReject(AcceptanceManifest(token, "wrong-token"), "PACK_TOOLCHAIN_MISMATCH", "A11-token");
            var hash = JsonFiles.Clone(fixtureBase); hash.bundles[0].sha256 = new string('0', 64);
            yield return AcceptanceReject(AcceptanceManifest(hash, "wrong-hash"), "PACK_HASH_MISMATCH", "A12-hash");
            var size = JsonFiles.Clone(fixtureBase); size.bundles[0].sizeBytes++;
            yield return AcceptanceReject(AcceptanceManifest(size, "wrong-size"), "PACK_INCOMPLETE", "A12-size");
            var missing = JsonFiles.Clone(fixtureBase); missing.bundles[0].path = "unfinished-transfer.bundle";
            yield return AcceptanceReject(AcceptanceManifest(missing, "missing-bundle"), "PACK_INCOMPLETE", "A12-missing");
            var requiredBodies = fixtureBase.renderers.Where(r => r.required && r.category == "body")
                .OrderByDescending(r => r.path == "Body_2").ToArray();
            AcceptanceRequire(requiredBodies.Any(r => r.path == "Body") && requiredBodies.Any(r => r.path == "Body_2"), "A09 requires actual Body and Body_2 renderer bindings");
            for (int bodyIndex = 0; bodyIndex < requiredBodies.Length; bodyIndex++)
            {
                var required = JsonFiles.Clone(fixtureBase);
                string removed = requiredBodies[bodyIndex].rendererId;
                required.renderers = required.renderers.Where(r => r.rendererId != removed).ToArray();
                required.morphBindings = required.morphBindings.Where(m => m.rendererId != removed).ToArray();
                yield return AcceptanceReject(AcceptanceManifest(required, "missing-required-body-" + bodyIndex), "REQUIRED_BINDING_MISSING", bodyIndex == 0 ? "A09-renderer" : "A09-renderer-" + (bodyIndex + 1));
            }
            AcceptancePass("A09-body-targets", "Required body-category bindings removed independently: " + string.Join(", ", requiredBodies.Select(r => r.path + " (" + r.rendererId + ")")) + "; every request rejected while preserving old root and snapshot.");
            var requiredClip = JsonFiles.Clone(fixtureBase);
            requiredClip.clips = requiredClip.clips.Where(c => c.clipId != "pose-arms-up").ToArray();
            yield return AcceptanceReject(AcceptanceManifest(requiredClip, "missing-required-clip"), "REQUIRED_BINDING_MISSING", "A09-clip");

            // Bundle hash and size are correct for intentionally invalid bytes. This must
            // pass prevalidation and fail only after old instances were destroyed/unloaded.
            var corrupt = JsonFiles.Clone(fixtureBase);
            var corruptContent = corrupt.bundles.First(b => b.id == corrupt.avatarPrefab.bundleId);
            corruptContent.path = "corrupt-content.bundle";
            File.WriteAllBytes(JsonFiles.PackChild(acceptanceFixtureRoot, corruptContent.path), new byte[] { 0x42, 0x41, 0x44, 0x21 });
            corruptContent.sizeBytes = 4;
            corruptContent.sha256 = JsonFiles.Sha256(JsonFiles.PackChild(acceptanceFixtureRoot, corruptContent.path));
            string corruptPath = AcceptanceManifest(corrupt, "corrupt-but-hash-valid");
            PackStore.Verify(corruptPath, CompatibilityToken);
            var oldRoot = Active.Avatar.Root; baseline = Snapshot(); pose = AcceptanceCapturePose(); completed = CompletedReloads;
            RequestReload(corruptPath);
            yield return AcceptanceIdle("failed load rollback");
            AcceptanceRequire(LastErrorCode == "RELOAD_FAILED_RESTORED" && Active?.Avatar != null && CompletedReloads == completed, "Failed bundle did not restore old pack: " + LastErrorCode);
            AcceptanceRequire(!oldRoot && Active.Verified.Hash == acceptanceSource.Hash, "Rollback did not reconstruct old instance");
            AcceptanceEquivalent(baseline, Snapshot(), false); AcceptancePoseMatches(pose); AcceptanceApplied(morph, garment, 15, false);
            AcceptancePass("A13", "Verified hash/size of invalid bundle bytes before request; Unity load failed after switching, destroyed old root, reloaded original bundle and restored stopped snapshot.");

            var noMorph = JsonFiles.Clone(fixtureBase);
            noMorph.morphBindings = noMorph.morphBindings.Where(m => m.rendererId != morph.rendererId || m.shapeName != morph.shapeName).ToArray();
            string noMorphPath = AcceptanceManifest(noMorph, "optional-morph-absent");
            RequestReload(noMorphPath);
            yield return AcceptanceIdle("optional morph disappearance");
            AcceptanceRequire(Active?.Verified.Path == noMorphPath && LastErrorCode == "", "Optional morph fixture not loaded");
            AcceptanceRequire(!Document.morphOverrides.Any(m => m.rendererId == morph.rendererId && m.shapeName == morph.shapeName), "Missing morph remained active");
            AcceptanceRequire(Document.unresolvedOverrides.Any(m => m.rendererId == morph.rendererId && m.shapeName == morph.shapeName && m.weight == 15 && m.reasonCode == "BINDING_MISSING"), "Missing morph override was lost");
            var actualMorphRenderer = (SkinnedMeshRenderer)Active.Avatar.Renderers[morph.rendererId];
            int shapeIndex = actualMorphRenderer.sharedMesh.GetBlendShapeIndex(morph.shapeName);
            AcceptanceRequire(Math.Abs(actualMorphRenderer.GetBlendShapeWeight(shapeIndex) - morph.defaultWeight) < .01, "Absent advertised binding still applied manual weight");
            AcceptancePass("A10", "Removed optional advertised binding; retained weight 15 as BINDING_MISSING, did not apply weight to still-present physical mesh shape.");
            RequestReload(good);
            yield return AcceptanceIdle("optional morph returns");
            AcceptanceRequire(Active?.Verified.Path == good && Document.unresolvedOverrides.Length == 0, "Returned binding was not resolved");
            AcceptanceApplied(morph, garment, 15, false);
            var withBoth = Snapshot();
            withBoth.morphOverrides = new[] { new MorphOverride { rendererId = morph.rendererId, shapeName = morph.shapeName, weight = 22 } };
            withBoth.unresolvedOverrides = new[] { new UnresolvedOverride { kind = "morph", rendererId = morph.rendererId, shapeName = morph.shapeName, weight = 15, reasonCode = "BINDING_MISSING" } };
            RequestReload(good, withBoth);
            yield return AcceptanceIdle("active override wins over unresolved");
            AcceptanceApplied(morph, garment, 22, false);
            AcceptanceRequire(Document.unresolvedOverrides.Length == 0, "Superseded unresolved override was retained");
            AcceptancePass("A24", "Returned optional binding reapplied weight 15; a current explicit weight 22 prevailed over older unresolved weight 15 through real reload.");
            SetMorph(morph.rendererId, morph.shapeName, 15);

            var shortened = JsonFiles.Clone(fixtureBase);
            shortened.clips.First(c => c.clipId == "pose-arms-up").durationSeconds = .25;
            string shortPath = AcceptanceManifest(shortened, "shortened-clip");
            RequestReload(shortPath);
            yield return AcceptanceIdle("duration clamp");
            AcceptanceRequire(Active?.Verified.Path == shortPath && Math.Abs(TimeSeconds - .25) < .00001 && Status.Contains("終端"), "Duration clamp or notice absent");
            AcceptancePass("A08", "Advertised clip duration shortened from source to .25 s; .7 s snapshot clamped to .25 s and UI status explained clamp.");
            RequestReload(good);
            yield return AcceptanceIdle("restore full duration");
            Seek(.7);

            string queuedFirst = AcceptanceManifest(fixtureBase, "queued-first");
            string queuedMiddle = AcceptanceManifest(fixtureBase, "queued-middle");
            string queuedLatest = AcceptanceManifest(fixtureBase, "queued-latest");
            baseline = Snapshot(); completed = CompletedReloads;
            RequestReload(queuedFirst);
            yield return AcceptanceWait(() => IsBusy || !reloadLoop, "enter switch before queued update");
            AcceptanceRequire(IsBusy, "Queued-update test never observed Switching");
            RequestReload(queuedMiddle); RequestReload(queuedLatest);
            yield return AcceptanceIdle("coalesced queued updates");
            AcceptanceRequire(Active?.Verified.Path == queuedLatest && CompletedReloads == completed + 2, "Active switch was interrupted, or latest pending update was not applied exactly once");
            AcceptanceEquivalent(baseline, Snapshot(), true); AcceptanceApplied(morph, garment, 15, false);
            AcceptancePass("A15", "Observed IsBusy before issuing two more requests in one frame; first switch completed and only latest pending candidate loaded (two successful transactions).");
            baseline = Snapshot(); completed = CompletedReloads;
            RequestReload(queuedFirst);
            yield return AcceptanceWait(() => IsBusy || !reloadLoop, "enter switch before invalid pending update");
            AcceptanceRequire(IsBusy, "Invalid-pending test never observed Switching");
            RequestReload(Path.Combine(acceptanceFixtureRoot, "no-manifest.json"));
            yield return AcceptanceIdle("invalid pending candidate");
            AcceptanceRequire(Active?.Verified.Path == queuedFirst && CompletedReloads == completed + 1 && LastErrorCode == "PACK_INCOMPLETE", "Invalid pending request lost successfully switched revision");
            AcceptanceEquivalent(baseline, Snapshot(), true); AcceptanceApplied(morph, garment, 15, false);
            AcceptancePass("A25", "Queued missing candidate during IsBusy; in-progress switch completed, pending verification rejected, committed display and adjustments preserved.");

            string firstSave = Path.Combine(output, "sessions", "first", "roundtrip.viewer.json");
            string secondSave = Path.Combine(output, "sessions", "second", "roundtrip.viewer.json");
            baseline = Snapshot();
            Save(firstSave); AcceptanceRequire(LastErrorCode == "" && File.Exists(firstSave) && !Dirty, "Initial save failed");
            Save(secondSave); AcceptanceRequire(LastErrorCode == "" && File.Exists(secondSave) && !Dirty, "Save As failed");
            var saved = JsonFiles.Read<SessionDocument>(secondSave);
            AcceptanceRequire(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(secondSave), saved.pack.manifestPath)) == Active.Verified.Path, "Save As did not rebase manifest reference");
            CameraPreset("all"); SetMorph(morph.rendererId, morph.shapeName, 40); SetVisible(garment.rendererId, true); Seek(.1);
            OpenPath(secondSave);
            yield return AcceptanceIdle("saved session reopen");
            AcceptanceEquivalent(baseline, Snapshot(), false); AcceptanceApplied(morph, garment, 15, false);
            AcceptanceRequire(!IsPlaying && !Dirty, "Loaded session not stopped and clean");
            JsonFiles.AtomicWrite(Path.Combine(output, "restart-expected.viewer.json"), Snapshot(), allowReplace: true);
            AcceptancePass("A23", "Saved to two separate directories, verified rebased path resolves to identical manifest, changed live controls and reopened Save As to restore camera/visibility/morph/time.");
            AcceptancePass("A16-in-process", "Save/Open restored stopped snapshot; separate --acceptance-resume true Player launch is required for process-restart proof.");

            var rejectedRoot = Active.Avatar.Root; baseline = Snapshot();
            string brokenSession = Path.Combine(output, "malformed.viewer.json");
            File.WriteAllText(brokenSession, "{ broken json"); string brokenHash = JsonFiles.Sha256(brokenSession);
            OpenPath(brokenSession);
            AcceptanceRequire(LastErrorCode == "JSON_INVALID" && Active.Avatar.Root == rejectedRoot && JsonFiles.Sha256(brokenSession) == brokenHash, "Malformed JSON changed display/file");
            AcceptanceEquivalent(baseline, Snapshot(), false);
            var unsupported = Snapshot(); unsupported.schemaVersion = 999;
            string unsupportedPath = Path.Combine(output, "future-schema.viewer.json"); JsonFiles.AtomicWrite(unsupportedPath, unsupported);
            OpenPath(unsupportedPath);
            AcceptanceRequire(LastErrorCode == "SESSION_SCHEMA_UNSUPPORTED" && Active.Avatar.Root == rejectedRoot, "Future schema changed active graph");
            AcceptanceEquivalent(baseline, Snapshot(), false);
            AcceptancePass("A17", "Malformed JSON and schema 999 were rejected before replacing active root or snapshot; malformed source hash unchanged.");
            string conflictPath = Path.Combine(output, "sessions", "conflict.viewer.json");
            Save(conflictPath); AcceptanceRequire(LastErrorCode == "" && !Dirty, "Conflict setup save failed");
            File.AppendAllText(conflictPath, "\n "); string externallyEditedHash = JsonFiles.Sha256(conflictPath);
            SetMorph(morph.rendererId, morph.shapeName, 17); Save(conflictPath);
            AcceptanceRequire(LastErrorCode == "SESSION_SAVE_CONFLICT" && Dirty && JsonFiles.Sha256(conflictPath) == externallyEditedHash, "External save conflict overwrote file or cleared dirty");
            AcceptancePass("A18-conflict", "Modified a test save externally, made live morph edit, attempted overwrite: SESSION_SAVE_CONFLICT retained external file bytes and dirty state.");
            SetMorph(morph.rendererId, morph.shapeName, 15);

            // A real Windows replacement failure, separate from optimistic hash conflict.
            // The lock permits reads (so pre-save hash validation succeeds) but denies
            // delete sharing, which File.Replace needs. Only this newly created test save
            // is held; no bundle, source or user save file is locked or modified.
            string lockedSave = Path.Combine(output, "sessions", "replace-locked.viewer.json");
            Save(lockedSave);
            AcceptanceRequire(LastErrorCode == "" && File.Exists(lockedSave) && !Dirty, "Replacement-failure setup save failed");
            byte[] lockedBytes = File.ReadAllBytes(lockedSave);
            SetMorph(morph.rendererId, morph.shapeName, 19);
            var lockedSnapshot = Snapshot();
            string ioExceptionLog = null, ioStatus = null, ioCode = null;
            Application.LogCallback observeIo = (message, trace, kind) =>
            {
                string exceptionTrace = message + "\n" + trace;
                if (kind == LogType.Exception && message.Contains("IOException") &&
                    (exceptionTrace.Contains("File.Replace") || exceptionTrace.Contains("FileSystem.ReplaceFile")) &&
                    exceptionTrace.Contains("JsonFiles.AtomicWrite")) ioExceptionLog = exceptionTrace;
            };
            Application.logMessageReceived += observeIo;
            try
            {
                using (var held = new FileStream(lockedSave, FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    Save(lockedSave);
                    ioCode = LastErrorCode; ioStatus = Status;
                    AcceptanceRequire(Dirty && File.ReadAllBytes(lockedSave).SequenceEqual(lockedBytes), "Failed replacement overwrote bytes or cleared dirty");
                }
                yield return null;
            }
            finally { Application.logMessageReceived -= observeIo; }
            AcceptanceRequire(!string.IsNullOrEmpty(ioCode) && ioCode != "SESSION_SAVE_CONFLICT" && !string.IsNullOrWhiteSpace(ioStatus) && !string.IsNullOrEmpty(ioExceptionLog), "No real I/O exception was reported for locked-file replacement: " + ioCode + " " + ioStatus);
            AcceptanceRequire(Dirty && File.ReadAllBytes(lockedSave).SequenceEqual(lockedBytes), "Replacement failure did not retain exact old save and dirty state");
            AcceptanceEquivalent(lockedSnapshot, Snapshot(), false);
            AcceptanceRequire(!Directory.EnumerateFiles(Path.GetDirectoryName(lockedSave), "." + Path.GetFileName(lockedSave) + ".*.tmp").Any(), "Replacement failure leaked a temporary save");
            AcceptancePass("A18-replace-io", "Actual Windows File.Replace failed while test save was held with FileShare.Read; exact previous bytes, complete edited snapshot and dirty retained, temporary file cleaned. UI error=" + ioCode + "; logged I/O exception=" + ioExceptionLog);
            SetMorph(morph.rendererId, morph.shapeName, 15);

            // The old revision's only corruptible file is a new private fixture copy.
            // Keep the genuine source and shared fixture bundle files untouched.
            var recoverable = JsonFiles.Clone(fixtureBase);
            var oldContent = recoverable.bundles.First(b => b.id == recoverable.avatarPrefab.bundleId);
            string originalContentPath = JsonFiles.PackChild(acceptanceSource.Directory, oldContent.path);
            oldContent.path = "recovery-old-content.bundle";
            string oldCopy = JsonFiles.PackChild(acceptanceFixtureRoot, oldContent.path);
            File.Copy(originalContentPath, oldCopy, false);
            string recoverablePath = AcceptanceManifest(recoverable, "recovery-old");
            RequestReload(recoverablePath);
            yield return AcceptanceIdle("prepare double-failure copy");
            AcceptanceRequire(Active?.Verified.Path == recoverablePath && LastErrorCode == "", "Recovery setup copy did not load");
            baseline = Snapshot(); pose = AcceptanceCapturePose();
            // Wait until the switch has unloaded old bundles, so Windows no longer holds
            // their file handles. Withhold only the independent old fixture while the
            // candidate is loading its dependencies, before recovery attempts to reopen it.
            string removedCopy = oldCopy + ".withheld";
            RequestReload(corruptPath);
            yield return AcceptanceWait(() => (IsBusy && Active == null) || !reloadLoop, "old bundles unloaded before Recovery fault");
            AcceptanceRequire(IsBusy && Active == null, "Recovery fault could not be injected after old bundle unload");
            File.Move(oldCopy, removedCopy);
            yield return AcceptanceIdle("both candidate and old unavailable");
            AcceptanceRequire(LastErrorCode == "RELOAD_RECOVERY_REQUIRED" && Active == null && Document != null && !IsPlaying, "Double failure did not reach retained-data Recovery");
            AcceptanceEquivalent(baseline, Snapshot(), false);
            string recoverySave = Path.Combine(output, "sessions", "recovery", "recovery.viewer.json");
            Save(recoverySave);
            AcceptanceRequire(LastErrorCode == "" && File.Exists(recoverySave), "Recovery Save As failed");
            var recoverySaved = JsonFiles.Read<SessionDocument>(recoverySave);
            AcceptanceRequire(recoverySaved.morphOverrides.Any(m => m.rendererId == morph.rendererId && m.weight == 15), "Recovery save lost adjustments");
            RequestReload(acceptanceSource.Path);
            yield return AcceptanceIdle("relink retained Recovery data");
            AcceptanceRequire(Active?.Verified.Path == acceptanceSource.Path && LastErrorCode == "", "Recovery relink failed");
            AcceptanceEquivalent(baseline, Snapshot(), true); AcceptancePoseMatches(pose); AcceptanceApplied(morph, garment, 15, false);
            File.Move(removedCopy, oldCopy);
            AcceptancePass("A14", "Loaded independent fixture copy, withheld only its content bundle, failed new bundle and old reload; Recovery retained snapshot, Save As serialized it, selecting original source reapplied it.");

            // Leave a useful, unmodified original revision on screen and preserve restart
            // fixtures. A subsequent launch will independently verify the earlier Save As.
            SetStatus("Windows実行時の更新・保存検証を完了しました");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "acceptance-final.png"));
            yield return new WaitForSecondsRealtime(.5f);
            AcceptanceRequire(acceptanceOriginalHashes.All(p => JsonFiles.Sha256(p.Key) == p.Value), "Immutable source files changed");
            AcceptancePass("source-preservation", "Original manifest and every source AssetBundle SHA-256 unchanged after all fixture failures and relinks.");
            acceptancePhase = "finished";
        }

        IEnumerator AcceptanceMissingSession(string output)
        {
            string inputPath = Arg("--session"), relinkPath = Arg("--relink-manifest");
            AcceptanceRequire(!string.IsNullOrWhiteSpace(inputPath) && !string.IsNullOrWhiteSpace(relinkPath), "Missing-session acceptance requires --session and --relink-manifest");
            inputPath = Path.GetFullPath(inputPath); relinkPath = Path.GetFullPath(relinkPath);
            var expected = JsonFiles.Read<SessionDocument>(inputPath, out var inputHash);
            Validation.Session(expected);
            expected.pack.manifestPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(inputPath), expected.pack.manifestPath));
            AcceptanceRequire(!File.Exists(expected.pack.manifestPath), "The supplied session must reference an actually missing manifest path");
            AcceptanceRequire(expected.morphOverrides.Length > 0 && expected.visibilityOverrides.Length > 0,
                "Provide a saved fixture with nonempty morph and visibility overrides to prove retained adjustments");
            acceptanceOriginalHashes.Add(inputPath, inputHash);
            acceptanceSource = PackStore.Verify(relinkPath, CompatibilityToken);
            acceptanceOriginalHashes.Add(acceptanceSource.Path, acceptanceSource.Hash);
            foreach (var bundle in acceptanceSource.Manifest.bundles)
            {
                string file = JsonFiles.PackChild(acceptanceSource.Directory, bundle.path);
                acceptanceOriginalHashes.Add(file, JsonFiles.Sha256(file));
            }
            yield return AcceptanceWait(() => !reloadLoop && !IsBusy && (Document != null || LastErrorCode != ""), "fresh startup missing saved revision");
            AcceptanceRequire(Active == null && Document != null && CompletedReloads == 0 && !IsPlaying && !Dirty && LastErrorCode == "RELOAD_RECOVERY_REQUIRED",
                "Fresh missing-session startup did not retain a stopped clean Recovery document without loading another pack: " + LastErrorCode);
            AcceptanceEquivalent(expected, Snapshot(), false);
            AcceptancePass("A14-startup-retained", "Fresh process opened valid saved session pointing to a missing manifest; zero successful reloads, no implicit current-pack substitution, Active null, all saved fields retained with absolute missing path, stopped and clean.");

            string recoverySave = Path.Combine(output, "missing-recovery.viewer.json");
            Save(recoverySave);
            AcceptanceRequire(File.Exists(recoverySave) && LastErrorCode == "" && !Dirty && Active == null, "Missing-session Recovery Save As failed");
            var saved = JsonFiles.Read<SessionDocument>(recoverySave);
            saved.pack.manifestPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(recoverySave), saved.pack.manifestPath));
            AcceptanceEquivalent(expected, saved, false);
            AcceptanceRequire(JsonFiles.Sha256(inputPath) == inputHash, "Recovery Save As modified the original session");
            AcceptancePass("A14-startup-save-as", "Recovery Save As wrote missing-recovery.viewer.json, rebased the original missing manifest reference and preserved every saved adjustment without modifying the source session.");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "missing-recovery.png"));
            yield return new WaitForSecondsRealtime(.25f);

            // The user/runner explicitly chooses this compatible manifest. Do not pass
            // request.session here: that would assert exact old-revision restoration,
            // while this operation deliberately migrates the retained saved adjustments.
            OpenPath(relinkPath);
            yield return AcceptanceIdle("explicit relink of initial Recovery snapshot");
            AcceptanceRequire(Active?.Avatar != null && Active.Verified.Path == relinkPath && CompletedReloads == 1 && !IsPlaying && Dirty && LastErrorCode == "", "Explicit compatible relink did not recover the retained session");
            AcceptanceEquivalent(expected, Snapshot(), true);
            foreach (var value in expected.visibilityOverrides)
                AcceptanceRequire(Active.Avatar.Renderers[value.rendererId].enabled == value.visible, "Relink lost applied renderer visibility: " + value.rendererId);
            foreach (var value in expected.morphOverrides)
                AcceptanceRequire(Math.Abs(Active.Avatar.MorphValue(value.rendererId, value.shapeName) - value.weight) < .01, "Relink lost applied morph value: " + value.shapeName);
            var firstMorph = expected.morphOverrides[0]; var firstVisibility = expected.visibilityOverrides[0];
            AcceptanceApplied(Active.Verified.Manifest.morphBindings.First(m => m.rendererId == firstMorph.rendererId && m.shapeName == firstMorph.shapeName),
                Active.Verified.Manifest.renderers.First(r => r.rendererId == firstVisibility.rendererId), firstMorph.weight, firstVisibility.visible);
            AcceptanceRequire(JsonFiles.Sha256(inputPath) == inputHash, "Explicit relink modified original saved input");
            AcceptancePass("A14-startup-relinked", "Explicit --relink-manifest loaded exactly once, preserved all non-pack fields, applied every visibility and morph override plus camera, stayed stopped, and became dirty without overwriting the old session.");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "missing-recovery-relinked.png"));
            yield return new WaitForSecondsRealtime(.5f);
            acceptancePhase = "missing-session-finished";
        }

        IEnumerator AcceptanceResume(string output)
        {
            yield return AcceptanceWait(() => Active?.Avatar != null && !reloadLoop && !IsBusy, "second process saved session load");
            AcceptanceRequire(!string.IsNullOrEmpty(Arg("--session")), "Resume verification requires an explicit --session argument");
            var expected = JsonFiles.Read<SessionDocument>(Path.Combine(output, "restart-expected.viewer.json"));
            AcceptanceEquivalent(expected, Snapshot(), false);
            AcceptanceRequire(!IsPlaying && !Dirty, "Second process restored playing or dirty state");
            foreach (var v in expected.visibilityOverrides) AcceptanceRequire(Active.Avatar.Renderers[v.rendererId].enabled == v.visible, "Restart renderer visibility mismatch");
            foreach (var m in expected.morphOverrides) AcceptanceRequire(Math.Abs(Active.Avatar.MorphValue(m.rendererId, m.shapeName) - m.weight) < .01, "Restart applied morph mismatch");
            AcceptancePass("A16-restart", "A second Player process opened --session Save As, matched all saved fields including manifest SHA, camera/clip time, applied renderer/morph values, and remained stopped and clean.");
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(output, "acceptance-restart.png"));
            yield return new WaitForSecondsRealtime(.5f);
            acceptancePhase = "restart-finished";
        }

        IEnumerator AcceptanceReject(string path, string error, string id)
        {
            var root = Active.Avatar.Root; var state = Snapshot(); int completed = CompletedReloads;
            RequestReload(path);
            yield return AcceptanceIdle(id);
            AcceptanceRequire(LastErrorCode == error, id + " expected " + error + " but got " + LastErrorCode);
            AcceptanceRequire(Active?.Avatar?.Root == root && CompletedReloads == completed, id + " replaced old instance before rejecting");
            AcceptanceEquivalent(state, Snapshot(), false);
            AcceptancePass(id, error + ": original live root identity, complete snapshot and successful-reload count unchanged.");
        }
        IEnumerator AcceptanceIdle(string phase) => AcceptanceWait(() => !reloadLoop && !IsBusy && pending == null, phase);
        IEnumerator AcceptanceWait(Func<bool> condition, string phase)
        {
            acceptancePhase = phase;
            float deadline = UnityEngine.Time.realtimeSinceStartup + 180;
            do
            {
                yield return null;
                if (condition()) yield break;
            } while (UnityEngine.Time.realtimeSinceStartup < deadline);
            throw new TimeoutException("Acceptance timed out during " + phase + ": " + LastErrorCode + " " + Status);
        }
        string AcceptanceManifest(PackManifest source, string name)
        {
            var fixture = JsonFiles.Clone(source);
            fixture.revision = "acceptance-" + name;
            string path = Path.Combine(acceptanceFixtureRoot, name + ".manifest.json");
            JsonFiles.AtomicWrite(path, fixture);
            return path;
        }
        static void AcceptanceRequire(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
        }
        void AcceptancePass(string id, string evidence)
        {
            acceptanceChecks.Add(new AcceptanceCheck { id = id, evidence = evidence });
            Debug.Log("VIEWER_ACCEPTANCE_PASS " + id + " " + evidence);
        }
        static void AcceptanceEquivalent(SessionDocument expected, SessionDocument actual, bool ignorePack)
        {
            var a = JsonFiles.Clone(expected); var b = JsonFiles.Clone(actual);
            if (ignorePack) b.pack = a.pack;
            AcceptanceRequire(JsonFiles.Encode(a) == JsonFiles.Encode(b), "Snapshot mismatch: expected " + JsonFiles.Encode(a) + " actual " + JsonFiles.Encode(b));
        }
        void AcceptanceApplied(MorphBinding morph, RendererRecord renderer, float weight, bool visible)
        {
            AcceptanceRequire(Active?.Avatar != null && !IsPlaying, "No stopped avatar");
            AcceptanceRequire(Math.Abs(Active.Avatar.MorphValue(morph.rendererId, morph.shapeName) - weight) < .01, "Live mesh morph value differs from retained snapshot");
            AcceptanceRequire(Active.Avatar.Renderers[renderer.rendererId].enabled == visible, "Live renderer visibility differs from snapshot");
            var c = Document.camera;
            var q = new Quaternion(c.orientationQuat[0], c.orientationQuat[1], c.orientationQuat[2], c.orientationQuat[3]);
            var p = new Vector3(c.targetMeters[0], c.targetMeters[1], c.targetMeters[2]) - q * Vector3.forward * c.distanceMeters;
            AcceptanceRequire(Vector3.Distance(previewCamera.transform.position, p) < .00001f && Quaternion.Angle(previewCamera.transform.rotation, q) < .03f, "Live camera differs from retained snapshot");
        }
        AcceptancePose AcceptanceCapturePose()
        {
            var transforms = Active.Avatar.Root.GetComponentsInChildren<Transform>(true);
            string Key(Transform t)
            {
                if (t == Active.Avatar.Root.transform) return ".";
                var segments = new List<string>();
                while (t != Active.Avatar.Root.transform) { segments.Add(t.name); t = t.parent; }
                segments.Reverse(); return string.Join("/", segments);
            }
            return new AcceptancePose
            {
                positions = transforms.ToDictionary(Key, t => t.localPosition),
                scales = transforms.ToDictionary(Key, t => t.localScale),
                rotations = transforms.ToDictionary(Key, t => t.localRotation)
            };
        }
        void AcceptancePoseMatches(AcceptancePose expected)
        {
            var actual = AcceptanceCapturePose();
            AcceptanceRequire(expected.positions.Count == actual.positions.Count, "Transform count changed unexpectedly");
            foreach (var p in expected.positions)
                AcceptanceRequire(actual.positions.ContainsKey(p.Key) && Vector3.Distance(p.Value, actual.positions[p.Key]) < .00001f && Vector3.Distance(expected.scales[p.Key], actual.scales[p.Key]) < .00001f && Quaternion.Angle(expected.rotations[p.Key], actual.rotations[p.Key]) < .03f, "Applied pose changed at " + p.Key);
        }
    }
}
