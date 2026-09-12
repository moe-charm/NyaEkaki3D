using System;
using System.Collections;
using System.IO;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        bool projectPickerOpen;

        void BrowseProject()
        {
            if (!projectPickerOpen) StartCoroutine(PickProject());
        }

        IEnumerator PickProject()
        {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            var previous = workspace;
            string state = workspace?.Document?.StateHash;
            projectPickerOpen = true;
            try
            {
                var picker = Platform.WindowsFilePicker.Open(
                    Platform.WindowsFilePicker.GetActiveWindow(),
                    ProjectDirectory(projectPath?.value),
                    "NyaForge project (project.nyaforge.json)\0project.nyaforge.json\0すべてのJSON (*.json)\0*.json\0\0",
                    "NyaForge — 制作projectを開く",
                    "json");
                while (!picker.IsCompleted) yield return null;
                if (picker.IsFaulted)
                {
                    SetStatus("制作projectの選択を開けませんでした: " + picker.Exception.GetBaseException().Message);
                    yield break;
                }
                if (string.IsNullOrEmpty(picker.Result)) yield break;
                string directory;
                try { directory = NativeProjectLocator.RequireManifestDirectory(picker.Result); }
                catch (AuthoringException error)
                {
                    SetStatus(error.Code == "PROJECT_MANIFEST_REQUIRED" ? "project.nyaforge.jsonを選択してください。" : "選択した制作projectが見つかりません。" );
                    yield break;
                }
                // The project can change while the Windows dialog is open. Do not
                // replace a newer workspace with the stale selection.
                if (!ReferenceEquals(previous, workspace) || state != workspace?.Document?.StateHash)
                {
                    SetStatus("選択中に作品が変わったため、projectを開く操作を取り消しました。");
                    yield break;
                }
                ConfirmReplace(() =>
                {
                    projectPath.SetValueWithoutNotify(directory);
                    OpenProject();
                });
            }
            finally { projectPickerOpen = false; }
#else
            SetStatus("制作projectのExplorer選択はWindows版に対応しています。フォルダのパスを指定して開けます。");
            yield break;
#endif
        }

        static string ProjectDirectory(string path)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(path)) return null;
                string full = Path.GetFullPath(path);
                if (File.Exists(full)) return Path.GetDirectoryName(full);
                return Directory.Exists(full) ? full : Path.GetDirectoryName(full);
            }
            catch (ArgumentException) { return null; }
            catch (IOException) { return null; }
        }
    }
}
