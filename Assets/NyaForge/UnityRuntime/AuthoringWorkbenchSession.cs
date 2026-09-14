using System;
using System.IO;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    /// <summary>
    /// Owns the live authoring document and the command service that edits it.
    /// UI panels read this state through the workbench; they do not construct
    /// a second command history or replace the workspace directly.
    /// </summary>
    public sealed class AuthoringWorkbenchSession
    {
        bool saveIncomplete;
        public AuthoringWorkspace Workspace { get; private set; }
        public AuthoringCommandService Commands { get; private set; }
        public string LoadedDirectory { get; private set; }
        public bool SaveIncomplete
        {
            get => saveIncomplete;
            set
            {
                if (saveIncomplete == value) return;
                saveIncomplete = value;
                StateChanged?.Invoke();
            }
        }

        public event Action StateChanged;

        public bool IsDirty => Workspace != null && Workspace.IsDirty;
        public bool CanUndo => Workspace != null && Workspace.CanUndo;
        public bool CanRedo => Workspace != null && Workspace.CanRedo;

        public void SetLoadedDirectory(string directory)
        {
            var normalized = directory == null ? null : Path.GetFullPath(directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (string.Equals(LoadedDirectory, normalized, StringComparison.OrdinalIgnoreCase)) return;
            LoadedDirectory = normalized;
            StateChanged?.Invoke();
        }

        public void NotifyChanged() => StateChanged?.Invoke();

        public void Replace(AuthoringWorkspace workspace, string loadedDirectory = null)
        {
            if (workspace == null) throw new ArgumentNullException(nameof(workspace));
            Workspace = workspace;
            Commands = new AuthoringCommandService(workspace);
            SetLoadedDirectory(loadedDirectory);
            SaveIncomplete = false;
            StateChanged?.Invoke();
        }
    }
}
