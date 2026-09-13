# NyaForge

Windows-first authoring app for VR characters, clothing, and items. The product goal is to complete modeling through export without requiring Blender.

This public repository contains the generic Unity Viewer, authoring core and UI, Unity Bridge, public synthetic fixtures, tests, and documentation. Private avatar files, textures, licenses, and packs stay in their source projects; generated builds and local verification outputs are excluded from Git.

The viewer opens a prepared asset pack and supports visibility, morph values, pose playback, camera framing, named confirmation sets, reload, and visual checks. Authoring includes empty projects, typed graphs, a runtime node canvas, non-destructive vertex editing, polygon source/edit nodes, face selection and region extrusion, Undo/Redo, native saving, GLB/VRM one-mesh import with rest-pose skin editing, standard GLB static/skinned export profiles, and MCP inspection/export commands. Native graph projects with imported rig, morph, spring, or attachment metadata use schema 4 (older schemas remain readable); FBX/BLEND and arbitrary pack editing remain outside the current import boundary. See the current task for evidence and remaining limits.

For a public import smoke without private avatar assets, run `Tools/New-NyaForgeGlbFixture.ps1` to create a tiny GLB with a static mesh and a two-bone skinned mesh, then pass it to `Tools/Test-NyaForgeAuthoring.ps1 -ImportModel` as described in the [authoring quick start](docs/Authoring-Quickstart.md).

## Start here

- [Current development task and verification results](current_task.md)
- [Documentation index and authority](docs/README.md)
- [Authoring design v2](docs/NyaForge-Authoring-Design2.md)
- [Implementation gaps and next development slices](docs/Development-Plan.md)
- [Authoring quick start](docs/Authoring-Quickstart.md)
- [Unity Bridge installation and import](UnityBridge/README.md)

## Build

Open this folder with Unity 6000.4.3f1 on Windows and open `Assets/Viewer/Scenes/Viewer.unity`. A pack is supplied separately through the viewer's `--library` option; no avatar is bundled in this repository.

With that Editor installed, build the public synthetic pack and Windows executable:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\Build-NyaForge.ps1 -Target All
```

Output: `Builds/Windows/NyaForge.exe`. Select **制作へ**, or start directly with `NyaForge.exe --authoring true`. This mode needs no avatar or external pack. The product uses its own `%USERPROFILE%/AppData/LocalLow/NyaForge/NyaForge` settings directory.

To build while a Player is running, use another output directory, for example `Tools/Build-NyaForge.ps1 -Target Player -BuildName Windows-C1B-Extrude`. The latest recorded verified binary and reports are listed in [current_task.md](current_task.md). Test scripts accept the same `-BuildName`. A build refuses to overwrite its running target.

To view an existing pack, select **パックを開く…** and choose `current.StandaloneWindows64.json` in the Windows file picker. Pack manifests and `.viewer.json` sessions are also supported; raw FBX/BLEND imports are not implemented. **最近**, **確認セット**, and **設定** show their controls only when needed. See the [quickstart](docs/Authoring-Quickstart.md).

To view the synthetic body/neck/collar pack, launch with `--library "<absolute repository path>/GeneratedPacks/NyaForgeFixture"`. This technical mannequin is not a Humanoid avatar.

In the authoring screen, **Explorerで選ぶ…** opens a Windows file picker for an existing `project.nyaforge.json`; NyaForge uses its parent directory as the native project folder and rejects other JSON files before replacing the current workspace.

Core regression tests require .NET 10:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
```

## Scope

The Viewer runtime is generic. Avatar-specific pack builders, binding profiles, source models, and licensed dependencies are intentionally maintained outside this repository.

## License

The repository code is released under the MIT License. Third-party Unity, Newtonsoft.Json, and other dependencies retain their own licenses.
