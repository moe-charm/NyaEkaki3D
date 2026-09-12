using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        sealed class ReloadRequest
        {
            public string path, hash, sessionPath, sessionHash;
            public SessionDocument session;
            public bool usePackDefaults;
            public string openedPath;
        }
        ReloadRequest pending;
        bool reloadLoop;
        public int CompletedReloads { get; private set; }
        public void RequestReload(string path, SessionDocument session = null, string expectedHash = null, string loadedSessionPath = null, string loadedSessionHash = null, bool usePackDefaults = false, string openedPath = null)
        {
            CancelPendingUpdateCheck(); // A later direct open supersedes older pointer verification too.
            pending = new ReloadRequest { path = path, hash = expectedHash, session = session, sessionPath = loadedSessionPath, sessionHash = loadedSessionHash, usePackDefaults = usePackDefaults, openedPath = openedPath };
            if (!reloadLoop) StartCoroutine(ReloadLoop());
        }
        IEnumerator ReloadLoop()
        {
            reloadLoop = true;
            while (pending != null)
            {
                var request = pending; pending = null;
                SetStatus("パックを確認しています…");
                var task = Task.Run(() => PackStore.Verify(request.path, CompatibilityToken, request.hash));
                while (!task.IsCompleted) yield return null;
                if (pending != null) continue; // Before switching, a newer request may supersede validation.
                if (task.IsFaulted)
                {
                    if (!RetainInitialSessionForRecovery(request, task.Exception.GetBaseException())) Error(task.Exception);
                    continue;
                }
                var candidate = task.Result;
                SessionDocument state = null;
                Exception failure = null;
                var reconciliationMessages = new System.Collections.Generic.List<string>();
                try
                {
                    state = request.session != null ? JsonFiles.Clone(request.session) : request.usePackDefaults || Document == null ? PackStore.Defaults(candidate) : Snapshot();
                    Validation.Session(state);
                    if (request.session != null && state.pack.revision != candidate.Manifest.revision)
                        throw new ContractException("PACK_REVISION_MISMATCH", "保存した版とパックの版が一致しません。元のパックを選んでください。");
                    reconciliationMessages = Validation.Reconcile(state, candidate.Manifest, request.session == null && !request.usePackDefaults ? Active?.Verified.Manifest : null);
                    state.pack = PackStore.Reference(candidate);
                }
                catch (Exception e) { failure = e; }
                if (failure != null) { Error(failure); continue; }
                // A switch is a non-interruptible transaction; new requests remain pending until Ready.
                IsBusy = true; Pause();
                var old = Active?.Verified;
                var oldState = Document == null ? null : Snapshot();
                bool oldDirty = Dirty;
                SetStatus("更新を読み込んでいます…");
                Active?.DestroyInstances();
                yield return null; // Ensure deferred Destroy has finished before Unload(true).
                Active?.Dispose(); Active = null;
                var loaded = new ActivePack { Verified = candidate };
                yield return LoadPack(loaded, e => failure = e);
                if (failure == null)
                {
                    try { loaded.Avatar.Apply(state, state.motion.timeSeconds); if (request.usePackDefaults || (oldState == null && request.session == null) || request.sessionPath == "") FrameAvatar(state, loaded.Avatar); }
                    catch (Exception e) { failure = e; }
                }
                if (failure == null)
                {
                    Active = loaded; Document = state; TimeSeconds = state.motion.timeSeconds; IsPlaying = false;
                    RememberPackPath(request.openedPath ?? request.path);
                    if (request.openedPath != null && Path.GetFileName(request.openedPath).StartsWith("current.", StringComparison.Ordinal)) LibraryPath = Path.GetDirectoryName(request.openedPath);
                    Dirty = !request.usePackDefaults && request.session == null && oldState != null;
                    if (request.usePackDefaults) { sessionPath = ""; sessionHash = null; }
                    if (request.sessionPath != null) { sessionPath = request.sessionPath; sessionHash = request.sessionHash; }
                    CompletedReloads++;
                    ApplyCameraAndLight(); RebuildControls(); SetStatus("表示中 · " + candidate.Manifest.revision + (reconciliationMessages.Count > 0 ? " · " + string.Join(" / ", reconciliationMessages) : ""));
                }
                else
                {
                    loaded.DestroyInstances(); yield return null; loaded.Dispose();
                    if (old != null)
                    {
                        var restored = new ActivePack { Verified = old }; Exception recoveryError = null;
                        yield return LoadPack(restored, e => recoveryError = e);
                        if (recoveryError == null)
                        {
                            try { restored.Avatar.Apply(oldState, oldState.motion.timeSeconds); }
                            catch (Exception e) { recoveryError = e; }
                        }
                        if (recoveryError == null)
                        {
                            Active = restored; Document = oldState; TimeSeconds = oldState.motion.timeSeconds; Dirty = oldDirty;
                            ApplyCameraAndLight(); RebuildControls();
                            SetStatus("更新に失敗したため前の版へ戻しました: " + failure.Message, "RELOAD_FAILED_RESTORED");
                        }
                        else
                        {
                            restored.DestroyInstances(); yield return null; restored.Dispose();
                            Document = oldState; Dirty = oldDirty;
                            SetStatus("パックを選び直してください。調整値は保持しています: " + recoveryError.Message, "RELOAD_RECOVERY_REQUIRED");
                        }
                    }
                    else if (!RetainInitialSessionForRecovery(request, failure)) Error(failure);
                }
                IsBusy = false; StateChanged?.Invoke();
            }
            reloadLoop = false;
        }
        bool RetainInitialSessionForRecovery(ReloadRequest request, Exception failure)
        {
            // A failed open must never replace an already displayed session or
            // a recovery snapshot. At startup, however, keep a valid saved
            // session even when its referenced pack is no longer available.
            if (Active != null || Document != null || request.session == null) return false;
            SessionDocument retained;
            try
            {
                retained = JsonFiles.Clone(request.session);
                Validation.Session(retained);
                // request.path has already been resolved against the session's
                // directory by OpenPath, not against the process directory.
                retained.pack.manifestPath = Path.GetFullPath(request.path);
            }
            catch { return false; }
            Document = retained;
            TimeSeconds = retained.motion.timeSeconds;
            IsPlaying = false;
            Dirty = false;
            if (request.sessionPath != null)
            {
                sessionPath = request.sessionPath;
                sessionHash = request.sessionHash;
            }
            savePathField?.SetValueWithoutNotify(sessionPath);
            ApplyCameraAndLight();
            SetStatus("パックを選び直してください。保存した調整値は保持しています: " + failure.Message, "RELOAD_RECOVERY_REQUIRED");
            StateChanged?.Invoke();
            return true;
        }
        IEnumerator LoadPack(ActivePack target, Action<Exception> fail)
        {
            foreach (var b in PackStore.DependencyOrder(target.Verified.Manifest))
            {
                AssetBundleCreateRequest operation = null;
                try { operation = AssetBundle.LoadFromFileAsync(JsonFiles.PackChild(target.Verified.Directory, b.path)); }
                catch (Exception e) { fail(e); yield break; }
                yield return operation;
                if (!operation.assetBundle) { fail(new ContractException("PACK_LOAD_FAILED", "パックを読み込めません: " + b.path)); yield break; }
                target.Bundles.Add(b.id, operation.assetBundle);
            }
            try { target.Instantiate(); }
            catch (Exception e) { fail(e); }
        }
    }
}
