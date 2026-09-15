using System;
using System.IO;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Refreshes only persistence-related presentation. Session changes
        /// can update this panel without rebuilding geometry or editor panels.
        /// The path field is intentionally left untouched while the user is
        /// entering a destination; Save/Open owns its value explicitly.
        /// </summary>
        void RefreshPersistencePanel()
        {
            if (projectLabel != null)
                projectLabel.text = HasUnsaved ? "● 未保存の変更があります" : "保存済み";
            if (projectManifestLabel != null)
            {
                string directory = projectPath == null ? "" : projectPath.value;
                try
                {
                    directory = string.IsNullOrWhiteSpace(directory) ? "" : Path.GetFullPath(directory);
                    projectManifestLabel.text = string.IsNullOrWhiteSpace(directory)
                        ? "正本: 保存先を指定してください"
                        : "正本: " + Path.Combine(directory, ProjectStore.ManifestName);
                    projectManifestLabel.tooltip = string.IsNullOrWhiteSpace(directory)
                        ? "この制作状態の正本ファイル。先に保存先を指定してください。"
                        : Path.Combine(directory, ProjectStore.ManifestName);
                }
                catch (Exception)
                {
                    projectManifestLabel.text = "正本: 保存先のパスを確認してください";
                }
            }
            RefreshProjectOutputScope();
            RefreshRecentProjectControls();
        }
    }
}
