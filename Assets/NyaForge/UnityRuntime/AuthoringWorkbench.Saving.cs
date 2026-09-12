using System;
using System.IO;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        // ProjectStore tracks the main document only. Until settings join its
        // transaction, a failed composite save must keep the workbench dirty.
        bool saveIncomplete;

        void SaveProject() { TrySaveProject(); }

        bool TrySaveProject()
        {
            saveIncomplete = true;
            try
            {
                var directory = Path.GetFullPath(projectPath.value).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                long expectedVersion = string.Equals(directory, savedDirectory, StringComparison.OrdinalIgnoreCase) ? workspace.SaveVersion : 0;
                ProjectStore.Save(directory, workspace, expectedVersion);
                // Remember the committed main-document version even if a later
                // step fails, so Save As can retry without a false conflict.
                savedDirectory = directory;
                VrmExpressionSessionStore.Save(directory, importedVrmSession);
                VrmSpringSessionStore.Save(directory, importedVrmSpringSession);
                graphCanvas.SaveLayout();
                saveIncomplete = false;
                Refresh();
                SetStatus("保存しました: " + directory);
                return true;
            }
            catch (Exception error)
            {
                saveIncomplete = true;
                Refresh();
                SetStatus("保存が完了していません。再試行してください: " + error.Message);
                Debug.LogWarning("[NyaForge authoring save] " + error);
                return false;
            }
        }

        bool TrySaveForExit() { return TrySaveProject() && !HasUnsaved; }
    }
}
