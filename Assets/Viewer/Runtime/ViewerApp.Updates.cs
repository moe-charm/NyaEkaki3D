using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        VisualElement updatePanel;
        Label updateTitle, updateSummary;
        Button updateApply;
        VerifiedPack offeredUpdate;
        int updateGeneration, supersededUpdateChecks;
        bool updateChecking;
        // Opt-in acceptance barrier, never enabled by normal application flows.
        bool holdPointerResultsForCheck;
        int heldPointerResultsForCheck;

        void CancelPendingUpdateCheck()
        {
            ++updateGeneration;
            updateChecking = false;
            HideUpdateOffer();
        }

        void BuildUpdateUi(VisualElement parent)
        {
            updatePanel = new VisualElement { name = "update-notice" };
            updatePanel.style.display = DisplayStyle.None;
            updatePanel.style.flexDirection = FlexDirection.Row;
            updatePanel.style.flexShrink = 0;
            updatePanel.style.backgroundColor = new Color(.10f, .23f, .25f);
            updatePanel.style.paddingLeft = 14; updatePanel.style.paddingRight = 12;
            updatePanel.style.paddingTop = 7; updatePanel.style.paddingBottom = 7;
            var text = new VisualElement(); text.style.flexGrow = 1; text.style.flexBasis = 0;
            updateTitle = new Label("新しい版があります");
            updateTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            updateSummary = new Label(); updateSummary.style.fontSize = 12;
            text.Add(updateTitle); text.Add(updateSummary); updatePanel.Add(text);
            var actions = new VisualElement(); actions.style.justifyContent = Justify.Center; actions.style.flexShrink = 0;
            updateApply = MakeButton("更新を適用", ApplyOfferedUpdate, "update-apply");
            actions.Add(updateApply);
            actions.Add(MakeButton("閉じる", DismissUpdateNotice, "update-dismiss"));
            updatePanel.Add(actions); parent.Add(updatePanel);
            StateChanged += RefreshOfferedUpdate;
            updatePanel.schedule.Execute(() => updateApply.SetEnabled(offeredUpdate != null && !updateChecking && !IsBusy && !reloadLoop)).Every(100);
        }

        public void BeginUpdateCheck() => BeginPointerCheck(Path.Combine(LibraryPath, "current.StandaloneWindows64.json"), false);

        void OpenCurrentPointer(string pointerPath, bool usePackDefaults = false) => BeginPointerCheck(pointerPath, true, usePackDefaults);

        void BeginPointerCheck(string pointerPath, bool applyImmediately, bool usePackDefaults = false)
        {
            int generation = ++updateGeneration;
            HideUpdateOffer();
            updateChecking = true;
            SetStatus(applyImmediately ? "選択したパックを確認しています…" : "新しい版を確認しています…");
            StartCoroutine(CheckUpdateCandidate(pointerPath, generation, applyImmediately, usePackDefaults));
        }

        IEnumerator CheckUpdateCandidate(string pointerPath, int generation, bool applyImmediately, bool usePackDefaults)
        {
            string token = CompatibilityToken;
            var operation = Task.Run(() => VerifyUpdatePointer(pointerPath, token));
            // Always yield once: a later check can supersede even a cached result.
            yield return null;
            while (!operation.IsCompleted) yield return null;
            if (holdPointerResultsForCheck)
            {
                heldPointerResultsForCheck++;
                while (holdPointerResultsForCheck) yield return null;
            }
            if (generation != updateGeneration)
            {
                if (operation.IsFaulted) _ = operation.Exception;
                supersededUpdateChecks++;
                yield break;
            }
            updateChecking = false;
            if (operation.IsFaulted) { Error(operation.Exception); yield break; }
            if (operation.IsCanceled) { SetStatus("更新確認を中止しました。"); yield break; }
            var candidate = operation.Result;
            try
            {
                if (applyImmediately)
                {
                    // Opening a pointer is an explicit load action. It shares
                    // identity/file verification with update checks, then uses
                    // the normal reload queue instead of showing an offer.
                    if (!usePackDefaults) CheckUpdateCompatibility(candidate);
                    RequestReload(candidate.Path, null, candidate.Hash, usePackDefaults: usePackDefaults, openedPath: pointerPath);
                    yield break;
                }
                // The active revision may have changed while the worker verified
                // the files. Use the current state, or cancel during a switch.
                if (reloadLoop || IsBusy)
                {
                    SetStatus("読込が完了してから、更新を確認してください。");
                    yield break;
                }
                if (Active == null && Document == null)
                {
                    RequestReload(candidate.Path, null, candidate.Hash, openedPath: pointerPath);
                    yield break;
                }
                if (SameActiveUpdate(candidate)) { SetStatus("現在の表示は最新の版です。"); yield break; }
                var messages = CheckUpdateCompatibility(candidate);
                offeredUpdate = candidate;
                DisplayUpdateOffer(messages);
                SetStatus("新しい版があります。変更内容を確認して適用できます。");
            }
            catch (Exception e) { HideUpdateOffer(); Error(e); }
        }

        static VerifiedPack VerifyUpdatePointer(string path, string token)
        {
            var pointer = JsonFiles.Read<CurrentReference>(path);
            if (pointer.schemaVersion != 1) throw new ContractException("PACK_SCHEMA_UNSUPPORTED", "更新案内の保存形式に対応していません。");
            if (pointer.buildTarget != "StandaloneWindows64") throw new ContractException("PACK_TARGET_MISMATCH", "Windows用の更新ではありません。");
            bool IsId(string value) => value != null && Regex.IsMatch(value, @"^[A-Za-z0-9][A-Za-z0-9_.:-]{0,159}$");
            if (!IsId(pointer.packId) || !IsId(pointer.revision)) throw new ContractException("ID_INVALID", "更新案内の識別情報が不正です。");
            if (pointer.manifestSha256 == null || !Regex.IsMatch(pointer.manifestSha256, "^[a-fA-F0-9]{64}$"))
                throw new ContractException("HASH_INVALID", "更新案内の確認情報が不正です。");
            string manifestPath = JsonFiles.PackChild(Path.GetDirectoryName(Path.GetFullPath(path)), pointer.manifestPath);
            var verified = PackStore.Verify(manifestPath, token, pointer.manifestSha256);
            if (verified.Manifest.packId != pointer.packId) throw new ContractException("PACK_ID_MISMATCH", "更新案内とパックの対象が一致しません。");
            if (verified.Manifest.revision != pointer.revision) throw new ContractException("PACK_REVISION_MISMATCH", "更新案内とパックの版が一致しません。");
            return verified;
        }

        bool SameActiveUpdate(VerifiedPack candidate) => Active != null &&
            Active.Verified.Manifest.packId == candidate.Manifest.packId &&
            Active.Verified.Manifest.revision == candidate.Manifest.revision && Active.Verified.Hash == candidate.Hash;

        List<string> CheckUpdateCompatibility(VerifiedPack candidate)
        {
            if (Document == null) return new List<string>();
            var snapshot = Snapshot(); Validation.Session(snapshot);
            return Validation.Reconcile(snapshot, candidate.Manifest, Active?.Verified.Manifest);
        }

        void DisplayUpdateOffer(List<string> messages)
        {
            if (updatePanel == null) return;
            updateTitle.text = "新しい版 · " + offeredUpdate.Manifest.displayName;
            updateTitle.tooltip = offeredUpdate.Manifest.revision;
            updateSummary.text = DescribeUpdate(Active?.Verified.Manifest, offeredUpdate.Manifest) +
                (messages.Count > 0 ? "\n適用時: " + string.Join(" / ", messages) : "");
            updatePanel.style.display = DisplayStyle.Flex;
            updateApply.SetEnabled(!IsBusy && !reloadLoop && !updateChecking);
            WakeRendering();
        }

        void RefreshOfferedUpdate()
        {
            if (offeredUpdate == null || IsBusy || reloadLoop) return;
            try
            {
                if (SameActiveUpdate(offeredUpdate)) { HideUpdateOffer(); return; }
                // Reconcile again after edits, because a newly selected pose or
                // morph override may alter compatibility or the warning text.
                var messages = CheckUpdateCompatibility(offeredUpdate);
                DisplayUpdateOffer(messages);
            }
            catch (Exception e) { HideUpdateOffer(); Error(e); }
        }

        void ApplyOfferedUpdate()
        {
            if (offeredUpdate == null) return;
            if (IsBusy || reloadLoop || updateChecking) { SetStatus("読込・更新確認が終わるまでお待ちください。"); return; }
            try
            {
                var candidate = offeredUpdate;
                CheckUpdateCompatibility(candidate);
                ++updateGeneration; updateChecking = false; HideUpdateOffer();
                // RequestReload verifies the immutable files again before it
                // destroys the current avatar, then takes the latest snapshot.
                RequestReload(candidate.Path, null, candidate.Hash);
            }
            catch (Exception e) { HideUpdateOffer(); Error(e); }
        }

        void DismissUpdateNotice()
        {
            ++updateGeneration; updateChecking = false; HideUpdateOffer();
            SetStatus("更新の案内を閉じました。現在の表示を続けます。");
        }

        void HideUpdateOffer()
        {
            offeredUpdate = null;
            if (updatePanel != null) updatePanel.style.display = DisplayStyle.None;
            WakeRendering();
        }

        static string DescribeUpdate(PackManifest previous, PackManifest next)
        {
            if (previous == null) return "この版に保存した調整値を適用できます。\nパーツ " + next.renderers.Length + " / シェイプキー " + next.morphBindings.Length + " / ポーズ " + next.clips.Length;
            string Changes<T>(string label, IEnumerable<T> oldValues, IEnumerable<T> newValues, Func<T, string> key, Func<T, string> name)
            {
                var oldRows = oldValues.ToDictionary(key, StringComparer.Ordinal);
                var newRows = newValues.ToDictionary(key, StringComparer.Ordinal);
                var added = newRows.Where(row => !oldRows.ContainsKey(row.Key)).Select(row => name(row.Value)).ToArray();
                var removed = oldRows.Where(row => !newRows.ContainsKey(row.Key)).Select(row => name(row.Value)).ToArray();
                int changed = newRows.Count(row => oldRows.TryGetValue(row.Key, out var old) && !JToken.DeepEquals(JToken.FromObject(old), JToken.FromObject(row.Value)));
                string Names(string[] names) => string.Join("、", names.Take(3)) + (names.Length > 3 ? " ほか" + (names.Length - 3) + "件" : "");
                return label + ": 追加 " + added.Length + " / 削除 " + removed.Length + " / 設定変更 " + changed +
                    (added.Length > 0 ? "（追加: " + Names(added) + "）" : "") + (removed.Length > 0 ? "（削除: " + Names(removed) + "）" : "");
            }
            var oldBundles = previous.bundles.ToDictionary(bundle => bundle.id, StringComparer.Ordinal);
            var changedBundles = next.bundles.Where(bundle => !oldBundles.TryGetValue(bundle.id, out var old) || old.sha256 != bundle.sha256).Select(bundle => bundle.id).ToArray();
            var details = new List<string>();
            if (changedBundles.Contains("content")) details.Add("衣装・身体・ポーズの表示データを更新");
            if (changedBundles.Contains("shaders")) details.Add("材質の表示データを更新");
            int other = changedBundles.Count(id => id != "content" && id != "shaders");
            if (other > 0) details.Add("その他の表示データ " + other + "件を更新");
            int removedBundles = previous.bundles.Count(bundle => !next.bundles.Any(row => row.id == bundle.id));
            if (removedBundles > 0) details.Add("表示データ " + removedBundles + "件を削除");
            if (details.Count == 0) details.Add("表示データは同じ内容です。構成・設定の変更を確認できます。");
            return string.Join(" / ", details) + "\n" +
                Changes("パーツ", previous.renderers, next.renderers, row => row.rendererId, row => row.displayName) + "\n" +
                Changes("シェイプキー", previous.morphBindings, next.morphBindings, row => row.rendererId + "\u001f" + row.shapeName, row => row.shapeName) + "\n" +
                Changes("ポーズ", previous.clips, next.clips, row => row.clipId, row => row.displayName);
        }

        IEnumerator RunUpdateNoticeCheck(string output)
        {
            var checks = new List<string>();
            string failure = null, originalLibrary = LibraryPath;
            var steps = UpdateNoticeCheckSteps(output, checks);
            while (true)
            {
                bool moved = false; object yielded = null;
                try { moved = steps.MoveNext(); if (moved) yielded = steps.Current; }
                catch (Exception e) { failure = e.ToString(); }
                if (failure != null || !moved) break;
                yield return yielded;
            }
            try { (steps as IDisposable)?.Dispose(); }
            catch (Exception e) { failure = (failure ?? "") + "\n" + e; }
            LibraryPath = originalLibrary;
            holdPointerResultsForCheck = false;
            ++updateGeneration; updateChecking = false; HideUpdateOffer();
            try
            {
                Directory.CreateDirectory(output);
                File.WriteAllText(Path.Combine(output, "update-notice.json"), JsonConvert.SerializeObject(new
                {
                    success = failure == null, checks, failure, completedReloads = CompletedReloads,
                    activeRevision = Active?.Verified.Manifest.revision,
                    unityVersion = Application.unityVersion, isEditor = Application.isEditor,
                    developmentBuild = Debug.isDebugBuild, supersededUpdateChecks,
                    scope = "Actual Player update notice/apply, copied immutable fixtures, pointer/hash/required-binding rejection, and newest-check wins. Real source revisions are never edited."
                }, Formatting.Indented));
            }
            catch (Exception e) { failure = (failure ?? "") + "\nReport write failed: " + e; Debug.LogException(e); }
            Debug.Log("VIEWER_UPDATE_NOTICE_CHECK_FINISHED " + (failure == null ? "PASS" : "FAIL"));
            if (failure != null) Debug.LogError(failure);
            if (Arg("--update-exit") == "true") Application.Quit(failure == null ? 0 : 1);
        }

        IEnumerator UpdateNoticeCheckSteps(string output, List<string> checks)
        {
            Directory.CreateDirectory(output);
            if (Application.isEditor || Debug.isDebugBuild || Application.platform != RuntimePlatform.WindowsPlayer)
                throw new InvalidOperationException("Update notice acceptance requires a normal Windows Player.");
            double deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 180;
            while ((Active == null || reloadLoop || IsBusy) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (Active?.Avatar == null || reloadLoop || IsBusy) throw new InvalidOperationException("Initial external pack failed to become ready.");
            var series = JObject.Parse(File.ReadAllText(Arg("--revision-series") ?? throw new ArgumentException("--revision-series is required for update acceptance")));
            var initialPack = Active.Verified;
            var next = ((JArray)series["revisions"]).Last(row => (string)row["revision"] != Active.Verified.Manifest.revision);
            var verified = PackStore.Verify((string)next["manifestPath"], CompatibilityToken, (string)next["manifestSha256"]);
            string library = Path.GetFullPath(Path.Combine(output, "test-library")); Directory.CreateDirectory(library);
            string pointerPath = Path.Combine(library, "current.StandaloneWindows64.json");
            string CopyFixture(VerifiedPack source, PackManifest replacement, string name)
            {
                string directory = Path.Combine(library, "revisions", name); Directory.CreateDirectory(directory);
                foreach (var bundle in source.Manifest.bundles)
                {
                    string destination = JsonFiles.PackChild(directory, bundle.path);
                    Directory.CreateDirectory(Path.GetDirectoryName(destination));
                    File.Copy(JsonFiles.PackChild(source.Directory, bundle.path), destination, false);
                }
                string manifestPath = Path.Combine(directory, "manifest.json");
                File.WriteAllText(manifestPath, JsonFiles.Encode(replacement)); return manifestPath;
            }
            string validPath = CopyFixture(verified, verified.Manifest, "valid");
            var pointer = new CurrentReference { schemaVersion = 1, packId = verified.Manifest.packId, revision = verified.Manifest.revision,
                buildTarget = "StandaloneWindows64", manifestPath = Path.GetRelativePath(library, validPath).Replace('\\', '/'), manifestSha256 = JsonFiles.Sha256(validPath) };
            void Publish(CurrentReference value) => JsonFiles.AtomicWrite(pointerPath, value, allowReplace: true);
            LibraryPath = library;
            Pause();
            Edit(state => { state.motion.clipId = "pose-arms-up"; state.motion.timeSeconds = .5; });
            CameraPreset("front"); CameraPreset("neck");
            var morph = Active.Verified.Manifest.morphBindings.First(row => row.shapeName == "Shrink_Neck");
            SetMorph(morph.rendererId, morph.shapeName, 15);
            var stateBefore = JObject.FromObject(Snapshot()); stateBefore.Remove("pack");
            var cameraBefore = previewCamera.transform.position; var rotationBefore = previewCamera.transform.rotation;
            var originalRoot = Active.Avatar.Root; int countBefore = CompletedReloads;
            Publish(pointer); BeginUpdateCheck();
            deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 180;
            while (updateChecking && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (updateChecking || offeredUpdate == null || Active.Avatar.Root != originalRoot || CompletedReloads != countBefore ||
                updatePanel == null || updatePanel.style.display.value != DisplayStyle.Flex || string.IsNullOrWhiteSpace(updateSummary.text))
                throw new InvalidOperationException("Valid check did not show a nonmutating update notice: " + LastErrorCode + " " + Status);
            checks.Add("valid-next-shows-summary-without-replacing-active-avatar");
            yield return new WaitForEndOfFrame();
            var shot = ScreenCapture.CaptureScreenshotAsTexture();
            try { File.WriteAllBytes(Path.Combine(output, "update-notice.png"), shot.EncodeToPNG()); }
            finally { Destroy(shot); }
            ApplyOfferedUpdate();
            deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 180;
            while ((reloadLoop || IsBusy) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            var stateAfter = JObject.FromObject(Snapshot()); stateAfter.Remove("pack");
            if (reloadLoop || IsBusy || Active?.Avatar == null || Active.Verified.Manifest.revision != pointer.revision || CompletedReloads != countBefore + 1 ||
                !JToken.DeepEquals(stateBefore, stateAfter) || IsPlaying || Vector3.Distance(cameraBefore, previewCamera.transform.position) > .00001f ||
                Quaternion.Angle(rotationBefore, previewCamera.transform.rotation) > .05f || Math.Abs(Active.Avatar.MorphValue(morph.rendererId, morph.shapeName) - 15) > .01)
                throw new InvalidOperationException("Explicit apply failed to preserve paused adjustment state: " + LastErrorCode + " " + Status);
            checks.Add("explicit-apply-switches-once-and-preserves-camera-pose-time-morph-visibility");
            var stableRoot = Active.Avatar.Root; int stableCount = CompletedReloads;
            var missingManifest = JsonFiles.Clone(verified.Manifest);
            var missing = missingManifest.renderers.First(row => row.required && row.path.EndsWith("/PrototypeCollar", StringComparison.Ordinal));
            missingManifest.renderers = missingManifest.renderers.Where(row => row.rendererId != missing.rendererId).ToArray();
            missingManifest.morphBindings = missingManifest.morphBindings.Where(row => row.rendererId != missing.rendererId).ToArray();
            missingManifest.revision += "-missing-required";
            string invalidPath = CopyFixture(verified, missingManifest, "missing-required");
            var badCases = new List<(CurrentReference pointer, string code)>();
            var badId = JsonFiles.Clone(pointer); badId.packId = "wrong-pack"; badCases.Add((badId, "PACK_ID_MISMATCH"));
            var badRevision = JsonFiles.Clone(pointer); badRevision.revision = "wrong-revision"; badCases.Add((badRevision, "PACK_REVISION_MISMATCH"));
            var badHash = JsonFiles.Clone(pointer); badHash.manifestSha256 = new string('0', 64); badCases.Add((badHash, "PACK_HASH_MISMATCH"));
            var badBinding = JsonFiles.Clone(pointer); badBinding.revision = missingManifest.revision;
            badBinding.manifestPath = Path.GetRelativePath(library, invalidPath).Replace('\\', '/'); badBinding.manifestSha256 = JsonFiles.Sha256(invalidPath);
            badCases.Add((badBinding, "REQUIRED_BINDING_MISSING"));
            foreach (var bad in badCases)
            {
                Publish(bad.pointer); BeginUpdateCheck();
                deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 180;
                while (updateChecking && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                if (updateChecking || offeredUpdate != null || Active.Avatar.Root != stableRoot || CompletedReloads != stableCount || LastErrorCode != bad.code)
                    throw new InvalidOperationException("Invalid update was not rejected before apply: expected " + bad.code + ", got " + LastErrorCode);
                checks.Add("rejected-before-apply-" + bad.code);
                OpenPath(pointerPath);
                deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 180;
                while ((updateChecking || reloadLoop || IsBusy) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                if (updateChecking || reloadLoop || IsBusy || offeredUpdate != null || Active.Avatar.Root != stableRoot || CompletedReloads != stableCount || LastErrorCode != bad.code)
                    throw new InvalidOperationException("Explicit pointer open did not reject the invalid candidate: expected " + bad.code + ", got " + LastErrorCode);
                checks.Add("explicit-open-rejected-" + bad.code);
            }
            Publish(pointer);
            int supersededBefore = supersededUpdateChecks;
            BeginUpdateCheck(); BeginUpdateCheck();
            deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 180;
            while ((updateChecking || supersededUpdateChecks == supersededBefore) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (updateChecking || supersededUpdateChecks <= supersededBefore || offeredUpdate != null || LastErrorCode != "" || Active.Avatar.Root != stableRoot || CompletedReloads != stableCount)
                throw new InvalidOperationException("Concurrent update checks did not keep the newest result.");
            checks.Add("concurrent-check-newest-wins-and-current-revision-is-latest");

            // Hold an actually verified pointer outside ReloadLoop, then issue a
            // newer manifest/session open. Releasing the older worker must not
            // replace that selection. This exercises both independent entry paths.
            foreach (bool useSession in new[] { false, true })
            {
                int heldBefore = heldPointerResultsForCheck;
                int discardedBefore = supersededUpdateChecks;
                int completedBefore = CompletedReloads;
                holdPointerResultsForCheck = true;
                OpenPath(pointerPath);
                deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 30;
                while (heldPointerResultsForCheck == heldBefore && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                if (heldPointerResultsForCheck == heldBefore) throw new InvalidOperationException("Pointer barrier was not reached.");
                if (useSession)
                {
                    var newerSession = Snapshot(); newerSession.pack = PackStore.Reference(initialPack);
                    string newerPath = Path.Combine(output, "newer-open.viewer.json");
                    File.WriteAllText(newerPath, JsonFiles.Encode(newerSession)); OpenPath(newerPath);
                }
                else OpenPath(initialPack.Path);
                deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 30;
                while ((reloadLoop || IsBusy) && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                if (Active?.Verified.Hash != initialPack.Hash || CompletedReloads != completedBefore + 1 || reloadLoop || IsBusy)
                    throw new InvalidOperationException("Newer selection failed while older pointer was held.");
                var selectedRoot = Active.Avatar.Root;
                holdPointerResultsForCheck = false;
                deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 30;
                while (supersededUpdateChecks == discardedBefore && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
                if (supersededUpdateChecks == discardedBefore || updateChecking || reloadLoop || IsBusy || Active.Avatar.Root != selectedRoot || CompletedReloads != completedBefore + 1)
                    throw new InvalidOperationException("Older pointer overrode the newer direct selection.");
                checks.Add("held-pointer-cannot-override-newer-" + (useSession ? "session" : "manifest"));
            }

            var clip = Active.Verified.Manifest.clips.First(c => c.clipId == Document.motion.clipId);
            Edit(s => { s.motion.loop = false; s.motion.timeSeconds = clip.durationSeconds - .02; });
            string savedNearEnd = Path.Combine(output, "near-end.viewer.json"); Save(savedNearEnd);
            if (Dirty || LastErrorCode != "") throw new InvalidOperationException("Cannot establish clean transport test state.");
            IsPlaying = true;
            deadline = UnityEngine.Time.realtimeSinceStartupAsDouble + 5;
            while (IsPlaying && UnityEngine.Time.realtimeSinceStartupAsDouble < deadline) yield return null;
            if (IsPlaying || !Dirty || Math.Abs(TimeSeconds - clip.durationSeconds) > .000001 || Document.motion.timeSeconds != TimeSeconds || !statusLabel.text.StartsWith("● ", StringComparison.Ordinal))
                throw new InvalidOperationException("Natural playback completion did not synchronize time/dirty/UI.");
            checks.Add("nonloop-natural-end-synchronizes-time-dirty-and-status");
            Save(savedNearEnd.ToUpperInvariant());
            if (Dirty || LastErrorCode != "" || JsonFiles.Read<SessionDocument>(savedNearEnd).motion.timeSeconds != clip.durationSeconds)
                throw new InvalidOperationException("Case-only Windows save path failed.");
            checks.Add("same-windows-path-case-change-keeps-save-fingerprint");
            string otherVolume = Path.Combine(Application.temporaryCachePath, "ReviewAcceptance", Guid.NewGuid().ToString("N"), "別名.viewer.json");
            if (string.Equals(Path.GetPathRoot(otherVolume), Path.GetPathRoot(Active.Verified.Path), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Cross-volume save fixture is unavailable on this machine.");
            Save(otherVolume);
            var savedOther = JsonFiles.Read<SessionDocument>(otherVolume);
            if (Dirty || LastErrorCode != "" || !Path.IsPathRooted(savedOther.pack.manifestPath) || !Status.Contains("絶対パス"))
                throw new InvalidOperationException("Cross-volume save did not show the absolute-reference notice.");
            checks.Add("cross-volume-save-announces-absolute-reference: " + otherVolume);
        }
    }
}
