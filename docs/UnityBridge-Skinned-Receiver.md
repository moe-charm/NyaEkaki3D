# Unity Bridge: skinned clothing receiver

`NyaForge.UnityBridge.Editor.SkinnedClothingReceiver` is the first Unity-side
receiver for the Windows v1 clothing path. It creates a scene child with a
`SkinnedMeshRenderer` from a Core `MeshData` + `SkeletonDefinition` +
`SkinBinding` result. `SkinnedClothingPackage` adds the delivery path: a
clothing-only GLB, stable skeleton sidecar, and stable binding sidecar are
published together under one manifest and checked before the receiver creates
anything.

The receiver takes an explicit `IReadOnlyDictionary<string, Transform>` keyed
by the Core `BoneId`. It never guesses a bone from a display name or from an
array position. Every mapped transform must be inside the supplied avatar
root, and all binding identities are checked before a scene object is created.
The authored rest transform is applied once, then vertices are converted to
avatar-root local space. Bindposes use
`bone.worldToLocalMatrix * avatarRoot.localToWorldMatrix`.

Unity's v1 `BoneWeight` path supports four influences per vertex. A Core
binding with five or more influences is rejected with
`SKIN_INFLUENCES_UNSUPPORTED`; it is never silently truncated. The object and
generated mesh/materials are registered or cleaned up as one operation, so an
exception does not leave a half-created receiver in the scene.

## Intended call

```csharp
var result = SkinnedClothingReceiver.Apply(
    clothingMesh,
    clothingTransform,
    clothingSkeleton,
    clothingBinding,
    avatarRoot,
    boneIdToAvatarTransform,
    "Cuff");
```

`ApplyGlb` is a convenience for the single-mesh/single-skin GLB profile and
uses the Core importer before applying the same explicit receiver checks.
`ApplyPackage` reads `skinned-clothing.nyaforge.json`, verifies all payload
hashes and geometry identities, and then applies the sidecar skeleton/binding
instead of trusting generated GLB bone IDs.
Materials can be supplied per submesh; otherwise temporary Standard-shader
materials are created for the viewer scene.

The disposable Bridge regression can exercise this exact package path by
passing `-ClothingPackageManifest <path>` to
`Tools/Test-NyaForgeUnityBridge.ps1`. The option is intentionally optional so
the base Bridge fixture remains independent of any private clothing asset.

This is a receiver-side Unity scene operation. It does not claim VRChat SDK or
VRChat runtime acceptance, PhysBones conversion, automatic body fitting,
material/shader equivalence, or final VRM semantic export. Those remain later
acceptance cards in `docs/Windows-v1-Development-Plan.md`.
