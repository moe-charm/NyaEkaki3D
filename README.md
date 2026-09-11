# NyaForge

Windows-first avatar and clothing viewer foundation for Blender-based workflows.

This public repository contains only the generic Unity Viewer source, data contracts, runtime shaders, UI, sample scene, and project settings. Avatar files, textures, licenses, private packs, generated builds, and local verification outputs stay in their source projects.

The current viewer opens a prepared asset pack and supports visibility, morph values, pose playback, camera framing, named confirmation sets, reload, and visual checks. Vertex editing is the next major feature: edits will be stored as a non-destructive layer and exported back to a modeling workflow rather than silently replacing the source mesh.

## Build

Open this folder with Unity 6000.4.3f1 on Windows and open `Assets/Viewer/Scenes/Viewer.unity`. A pack is supplied separately through the viewer's `--library` option; no avatar is bundled in this repository.

## Scope

The Viewer runtime is generic. Avatar-specific pack builders, binding profiles, source models, and licensed dependencies are intentionally maintained outside this repository.

## License

The repository code is released under the MIT License. Third-party Unity, Newtonsoft.Json, and other dependencies retain their own licenses.
