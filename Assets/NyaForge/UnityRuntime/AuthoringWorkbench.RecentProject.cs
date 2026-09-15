using System;
using System.IO;
using System.Text;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        const int RecentProjectPointerLimit = 4096;
        const string RecentProjectPointerName = "last-project.pointer";

        string RecentProjectPointerPath
        {
            get { return Path.Combine(Application.persistentDataPath, "Authoring", RecentProjectPointerName); }
        }

        /// <summary>
        /// Reads the last successful native-save directory. The pointer is
        /// disposable convenience metadata; the native manifest remains the
        /// source of truth and is validated before it is returned.
        /// </summary>
        string ReadRecentProjectDirectory()
        {
            try
            {
                string path = RecentProjectPointerPath;
                if (!File.Exists(path)) return null;
                byte[] bytes = File.ReadAllBytes(path);
                if (bytes.Length == 0 || bytes.Length > RecentProjectPointerLimit) return null;
                string selected = new UTF8Encoding(false, true).GetString(bytes).Trim();
                if (selected.Length == 0 || selected.IndexOf('\0') >= 0 || selected.IndexOf('\r') >= 0 || selected.IndexOf('\n') >= 0) return null;
                return NativeProjectLocator.RequireManifestDirectory(Path.Combine(selected, ProjectStore.ManifestName));
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is DecoderFallbackException || error is ArgumentException || error is NotSupportedException || error is AuthoringException)
            {
                return null;
            }
        }

        void RememberRecentProject(string directory)
        {
            try
            {
                string canonical = NativeProjectLocator.RequireManifestDirectory(Path.Combine(Path.GetFullPath(directory), ProjectStore.ManifestName));
                string pointer = RecentProjectPointerPath;
                string parent = Path.GetDirectoryName(pointer);
                Directory.CreateDirectory(parent);
                string temporary = pointer + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    File.WriteAllText(temporary, canonical + Environment.NewLine, new UTF8Encoding(false));
                    if (File.Exists(pointer)) File.Replace(temporary, pointer, null);
                    else File.Move(temporary, pointer);
                }
                finally
                {
                    if (File.Exists(temporary)) File.Delete(temporary);
                }
            }
            catch (Exception error) when (error is IOException || error is UnauthorizedAccessException || error is ArgumentException || error is NotSupportedException || error is AuthoringException)
            {
                Debug.LogWarning("[NyaForge recent project] " + error.Message);
            }
        }

        void ReopenRecentProject()
        {
            string directory = ReadRecentProjectDirectory();
            if (string.IsNullOrEmpty(directory))
            {
                SetStatus("最後に保存した制作projectが見つかりません。Explorerでproject.nyaforge.jsonを選んでください。");
                RefreshRecentProjectControls();
                return;
            }
            ConfirmReplace(() =>
            {
                projectPath.SetValueWithoutNotify(directory);
                OpenProject();
            });
        }

        void RefreshRecentProjectControls()
        {
            if (recentProjectButton == null) return;
            string directory = ReadRecentProjectDirectory();
            recentProjectButton.SetEnabled(!string.IsNullOrEmpty(directory));
            recentProjectButton.tooltip = string.IsNullOrEmpty(directory)
                ? "正常保存した制作projectがまだありません。Explorerでproject.nyaforge.jsonを選んでください。"
                : "最後に正常保存した制作: " + directory + "\\" + ProjectStore.ManifestName;
        }
    }
}
