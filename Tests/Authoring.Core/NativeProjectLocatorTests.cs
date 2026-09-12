using System;
using System.IO;
using NyaForge.Authoring;

internal static partial class Program
{
    static void RunNativeProjectLocatorTests()
    {
        Test("native project locator returns the manifest parent", () =>
        {
            string directory = Dir("native-locator-valid");
            string manifest = Path.Combine(directory, ProjectStore.ManifestName);
            File.WriteAllText(manifest, "{}");
            Equal(Path.GetFullPath(directory), NativeProjectLocator.RequireManifestDirectory(manifest));
        });

        Test("native project locator rejects another JSON file", () =>
        {
            string directory = Dir("native-locator-wrong-name");
            string selected = Path.Combine(directory, "other.json");
            File.WriteAllText(selected, "{}");
            Expect("PROJECT_MANIFEST_REQUIRED", () => NativeProjectLocator.RequireManifestDirectory(selected));
        });

        Test("native project locator rejects a missing manifest", () =>
        {
            string selected = Path.Combine(Dir("native-locator-missing"), ProjectStore.ManifestName);
            Expect("PROJECT_MANIFEST_MISSING", () => NativeProjectLocator.RequireManifestDirectory(selected));
        });

        Test("native project locator rejects an empty path", () =>
        {
            Expect("PROJECT_PATH_REQUIRED", () => NativeProjectLocator.RequireManifestDirectory("  "));
        });
    }
}
