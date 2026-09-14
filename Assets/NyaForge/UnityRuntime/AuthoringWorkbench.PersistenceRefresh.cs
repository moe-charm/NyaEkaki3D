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
        }
    }
}
