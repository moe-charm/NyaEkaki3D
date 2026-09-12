using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifySaveFailureGuard(string output, List<string> checks)
        {
            foreach (string sidecar in new[] { VrmExpressionSessionStore.FileName, VrmSpringSessionStore.FileName })
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateFixture(), null);
                string directory = Path.Combine(output, "save-failure-" + Path.GetFileNameWithoutExtension(sidecar));
                Directory.CreateDirectory(directory);
                projectPath.SetValueWithoutNotify(directory);
                // An exclusive stale-sidecar handle prevents its deletion. This
                // exercises a real filesystem failure after the main commit.
                using (var locked = new FileStream(Path.Combine(directory, sidecar), FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    Check(!TrySaveForExit(), "Failed sidecar save allowed exit");
                    Check(!workspace.IsDirty && workspace.SaveVersion == 1, "Failure did not occur after the main commit");
                    Check(HasUnsaved && saveIncomplete, "Failed save lost its pending state");
                    Check(!WantsToQuit(), "Failed save bypassed quit confirmation");
                    bool replaced = false;
                    ConfirmReplace(() => replaced = true);
                    Check(!replaced, "Failed save allowed unconfirmed replacement");
                    CancelReplace();
                }
                Check(TrySaveProject(), "Save As retry failed after sidecar unlock");
                Check(workspace.SaveVersion == 2 && !HasUnsaved, "Retry did not finish the composite save");
                OpenProject();
                Check(!HasUnsaved && workspace.SaveVersion == 2, "Retried project did not reopen");
            }
            checks.Add("Save failure guard: expression/Spring sidecar IO failures keep pending state, deny exit/replacement, and permit Save As retry/reopen");
        }
    }
}
