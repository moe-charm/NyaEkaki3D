# NyaForge Authoring Core 0.1

This Unity-independent assembly implements empty projects and the initial static editing profile. It can be used directly from this project's Assets directory or installed as the local Unity package `com.nyaforge.authoring` in another project. The receiving `UnityBridge` package uses the same Bake reader.

Modules: `Domain/` owns immutable project/object data, `Commands/` owns typed operations, candidate evaluation and transactions, `Projection/` defines the resource publication contract, `Persistence/` owns schema codecs and atomic storage primitives, and `Rig/` owns Unity-independent rest skeleton and normalized skin-binding data. `AuthoringWorkspace` holds the current document/history; `ProjectStore` coordinates persistence. Unity rendering stays in `UnityRuntime`.

`Graph/` now owns typed node definitions, DAG validation, plane/source/edit/output evaluation, pinned edit contexts, and native graph persistence. `rig.skeleton` is a typed rest-skeleton node whose `NYRS` v1 blob survives save/reload, and `rig.skin-bind` validates mesh/skeleton identities while persisting an `NYRB` v1 binding blob; both are exposed by graph inspection. `rig.pose` and `rig.skin-deform` also persist and evaluate rest-relative deformation. `MorphTarget`/`MorphSet` provide a mesh-pinned sparse delta core and `NYRM` codec. `rig.morph-set` and `rig.morph-deform` are typed graph nodes with native persistence and inspection; the runtime workbench exposes target/weight editing and mapped VRM expression application for these nodes. `Import/GlbImport` provides a strict first GLB v2 boundary for one static mesh with bounded triangle primitives, shared attribute layout and POSITION morph retention; `Import/GlbSkinImport` adds a separate translation-only one-skin profile that maps JOINTS_0/WEIGHTS_0 into the rig core and the existing graph chain; `Import/VrmMetadata` reads VRM 0.x/1.0 identity, humanoid mapping, expression and SpringBone inventory without binding the core to UniVRM, while `VrmExpressionMapper` resolves supported one-owner morph binds to native target IDs. `VrmExpressionSession` and `VrmSpringSession` keep supported imported metadata in bounded project sidecars. Existing static object evaluation passes through Source→EditMesh→Output; its historical property APIs are views over those nodes. Runtime Rig UI includes bone selection, weight mixing, brush weight paint, XYZ pose, rest movement and explicit rebind; SpringBone simulation, VRM/FBX mesh/material conversion, material application and export remain pending. See [Graph integration boundaries](Graph/README.md).

`AuthoringWorkspace.CreateEmpty()` creates zero objects without a dummy mesh. Its evaluation returns no mesh. `AuthoringOperation.AddMesh(mesh, transform)` adds a sample through the same transaction/history path, so Undo can return to empty. The current static profile supports zero or one object; multiple objects and typed graphs are subsequent work. Empty exports fail with `NO_EXPORTABLE_OBJECT` before creating an output directory.

Native writes now use schema **2**, with an explicit objects array. Schema **1** is read through `LegacyProjectCodec`; saving over a schema 1 manifest returns `MIGRATION_REQUIRED`. Save to a new directory to migrate while preserving the original. State/geometry hashes for legacy single-object data remain comparable. Bake stays at schema 1, independently of the native document schema.

```csharp
using NyaForge.Authoring;

var workspace = AuthoringWorkspace.CreateFixture(100f);
var commands = new AuthoringCommandService(workspace);
var request = workspace.NewCommand(
    AuthoringOperation.TranslateVertices(new[] { 0 }, new Vec3(0.01f, 0, 0)));
CommandResult result = commands.Execute(request, optionalProjection);
if (!result.Success) throw new Exception(result.Code + ": " + result.Message);
ProjectStore.Save(projectDirectory, workspace, expectedSaveVersion: 0);
string manifest = BakeStore.Export(exportDirectory, workspace);
BakeDocument received = BakeStore.Read(manifest);
Vec3 metres = received.Transform.ToAvatarPoint(received.Mesh.Positions[0]);
```

`MeshData` owns all input arrays. Attribute lists are read-only; submesh index arrays returned to callers are copies. Empty attribute arrays mean absent attributes. `AuthoringDocument.BaselineMesh` never changes. Its sparse `Offsets` use avatar rest metres; evaluation converts each vector to mesh local coordinates exactly once. The first profile supports positive uniform scale and translation only. Mesh positions in the Bake binary remain mesh-local; consumers apply `BakeDocument.Transform` once. There is no hidden scale-100 correction.

`Execute` serializes writers, validates identities and revisions, and resolves repeated command IDs before checking the old revision. Reusing an ID with another payload fails. Successful and structurally valid failed requests retain their original result for this instance. Reopening changes instance ID, resets Undo/Redo and request history, and preserves geometry and document revision. Undo/Redo also advance revision. `IsDirty` compares content to the last saved content, so returning to that content through Undo/Redo clears it.

Projection adapters implement `IAuthoringProjection.Prepare` and return independently owned candidate resources. `IPreparedProjection.Commit` swaps the visible projection; `Rollback` must restore the previous projection even when Commit failed midway; `Dispose` releases candidate resources after failure or old resources after success. A rollback failure is reported as `PROJECTION_RECOVERY_REQUIRED`. Adapters must not invoke nested edits or save/export from inside callbacks. Cleanup exceptions after a commit do not turn it into an apparent failed edit.

Native storage consists of `project.nyaforge.json` and immutable `blobs/<sha256>.bin`. Optional adapter sidecars, such as `vrm-expression-session.nyaforge.json` and `vrm-spring-session.nyaforge.json`, contain only explicitly supported metadata and are owned by their adapter rather than the graph manifest. Saving uses a process lock, on-disk optimistic save version, flushed temporary blobs, then atomic manifest replacement. Keep the expected version observed when opening/saving; do not fetch a newer version just to bypass a conflict. A new directory expects version 0. Old blobs remain available; automatic garbage collection is intentionally absent. Local filesystems must support same-directory atomic replacement and exclusive file sharing for these guarantees; power-loss durability depends on the filesystem.

Binary schemas use little-endian numeric fields, version 1, and the explicit byte-order marker `0x01020304`. Mesh blobs carry magic `NYFM`, vertex/attribute/submesh counts, each submesh's index count, positions, normals, tangents, UV0, then triangle indices. Delta blobs carry `NYFD`, count, then ordered vertex-index/Vec3 records. Readers bound file sizes and validate counts and exact lengths before allocating arrays. Hashes use SHA-256 over canonical bytes (negative zero is normalized). Native and Bake manifests reject unknown/missing/duplicate fields and type coercion.

Limits: 100,000 vertices, 600,000 indices, 32 submeshes, 16 MiB per blob, 64 KiB per manifest, 64 operations per command, 128 Undo/Redo entries, and 10,000 remembered command IDs per instance. The latter limit refuses new edits until reopening instead of silently evicting deduplication records. Normals and tangents are preserved after position edits; the Bake result explicitly reports that condition. The current `Rig/` slice validates rest bones and normalized weights, evaluates rest-relative pose deformation, and persists skeleton/binding/pose codecs; humanoid placement, multi-axis pose, morphs, animation, shader/material conversion, Blender source correspondence, body fitting, pack extraction, skin export and MCP transport remain outside the current milestone.

The original public fixture has eight vertices, four triangles, two submeshes, a front/back normal and UV seam, and no private avatar dependencies. At scale 1 and 100 it occupies the same rest-space coordinates. The reference edit moves vertex 0 from `(-0.1, -0.05, -0.02)` to `(-0.09, -0.05, -0.02)` metres; the other vertices are unchanged.

Run the independent console regression suite from the repository root:

```powershell
dotnet run --project Tests/Authoring.Core/Authoring.Core.Tests.csproj
```

The suite tests data ownership, coordinate conversion, transactions, rollback, retries, stale writes, Undo, persistence, corruption and unsupported features. Passing it does not establish Unity rendering or receiving-project compatibility; those are separate Unity integration checks.
