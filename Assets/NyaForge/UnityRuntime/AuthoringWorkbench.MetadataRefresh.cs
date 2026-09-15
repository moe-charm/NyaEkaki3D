namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Rebuilds every in-memory cache whose source of truth is a native
        /// project attachment. History restores document and attachments as a
        /// pair, so GUI and MCP must pass through the same boundary before
        /// refreshing panels or exporting VRM data.
        /// </summary>
        void RefreshMetadataFromWorkspace()
        {
            RefreshImportedVrmSessionsFromWorkspace();
            RefreshSecondaryMotionAttachmentFromWorkspace();
            RefreshReferenceProtectionFromWorkspace();
            RefreshDeliveryAllowlistFromWorkspace();
        }
    }
}
