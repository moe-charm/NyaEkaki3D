using System;
using System.IO;

namespace NyaForge.Authoring
{
    /// <summary>Validates a user-selected native project manifest without opening or mutating it.</summary>
    public static class NativeProjectLocator
    {
        /// <summary>
        /// Returns the canonical parent directory for an existing
        /// <c>project.nyaforge.json</c>. The caller still owns the subsequent
        /// ProjectStore.Open and unsaved-change decision.
        /// </summary>
        public static string RequireManifestDirectory(string selectedPath)
        {
            Checks.Require(!string.IsNullOrWhiteSpace(selectedPath), "PROJECT_PATH_REQUIRED", "A native project manifest path is required.");
            string full;
            try
            {
                full = Path.GetFullPath(selectedPath);
            }
            catch (ArgumentException)
            {
                throw new AuthoringException("PROJECT_PATH_INVALID", "Native project path is invalid.");
            }
            catch (NotSupportedException)
            {
                throw new AuthoringException("PROJECT_PATH_INVALID", "Native project path is unsupported.");
            }

            Checks.Require(string.Equals(Path.GetFileName(full), ProjectStore.ManifestName, StringComparison.OrdinalIgnoreCase),
                "PROJECT_MANIFEST_REQUIRED", "Select project.nyaforge.json.");
            Checks.Require(File.Exists(full), "PROJECT_MANIFEST_MISSING", "The native project manifest does not exist.");
            string directory = Path.GetDirectoryName(full);
            Checks.Require(!string.IsNullOrEmpty(directory), "PROJECT_PATH_INVALID", "Native project manifest has no parent directory.");
            return directory;
        }
    }
}
