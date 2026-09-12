# Paint foundation

This folder is a Unity-independent image and brush foundation. Typed Paint graph nodes now own image payloads and UV bindings, stroke commands share document Undo, native projects reference image blobs, and final preview displays base color. The UnityRuntime brush GUI has passed pointer stroke, Undo, cancellation/capture loss, Escape and native reopen verification. Its temporary preview stays outside the document; a completed stroke commits once. Manual OS input acceptance remains pending. General material graphs remain pending. SurfaceBakeStore exports one opaque base-color image with a static mesh; Unity Bridge imports its PNG, material and prefab. Current progress is in root current_task.md.

Images are immutable 64×64 RGBA8 tiles. A stroke clones only touched tiles and returns a new image; older images remain usable by future Undo entries. Dimensions are initially limited to 1024×1024. RGB is sRGB encoded; alpha is straight linear coverage. Rows start at the bottom-left so the Unity texture adapter needs no vertical flip.

Strokes accept UV points in 0..1 and a radius in pixels. Coverage is the union of antialiased round line segments, so extra input samples along the same path do not increase opacity. Color compositing uses linear-light source-over and re-encodes RGB to sRGB. Separate strokes accumulate normally. The per-stroke work estimate is bounded before coverage allocation. This version has no masks, pressure, eraser, layers or GPU brush path yet.

NYFI v1 image blobs include dimensions and an explicit base-color/sRGB/straight-alpha/bottom-left profile. The hash store validates bytes and exact payload length. Native graph schema3 now references these blobs through image.paint v1; saving an isolated image blob still does not constitute saving a painted project.

PaintUvBinding hashes polygon domain, face/material/corner/vertex references and UV coordinates. Geometry position changes retain the binding, while UV, topology or domain changes require deliberate reconciliation. Paint node evaluation enforces this check; failures retain the old image as unresolved, and native save retains that state. Output's optional Image baseColor port checks the target mesh domain and UV hash again. A blank Paint node starts as opaque white and binds when its first stroke is committed. Once painted, it never silently rebases to a new UV map. Explicit rebind is a separate common command: it keeps every image pixel and updates the UV/domain interpretation after an explicit GUI action. Its context pins old binding, image hash, input node and target mesh snapshot; stale contexts are rejected. Reprojection/rebake that preserves surface appearance remains pending.

PaintEditing contexts pin image content, UV mapping and graph mesh domain. A stroke creates a new immutable node payload and one shared Undo entry. Stale image contexts are rejected, and retries with the same command ID do not composite twice. Mesh-only Bake still refuses bound base-color images. Use the separate surface profile for textured output.




## Layer foundation

`PaintLayer` owns an immutable image, stable ID/name, visibility, opacity 0..1 and optional `PaintMask`. A mask is bottom-left, linear byte coverage (0 hides, 255 reveals), independent of image RGB and alpha. Constructor input and returned mask bytes are copied.

`PaintLayers` is ordered bottom to top. Empty stacks composite to transparent black. Each layer uses linear-light source-over with `image alpha × opacity × mask coverage`; intermediate composites are RGBA8. `PaintBlend` shares the same calculation with existing brush strokes. Hidden layers remain in the stack and retain their image/mask.

Initial limits: 16 layers, matching canvas dimensions up to 1024×1024, and 32 MiB of conservatively counted padded image tiles plus mask bytes. Repeated shared images still count toward this limit. This bounds both working memory and later asset ownership. The compositor currently visits each visible layer pixel; tile caching and incremental evaluation remain future work.

Layer codecs, graph/native ownership, dedicated commands and layer selection/brush GUI are integrated. Mask painting and layer rename GUI are implemented in the separate PaintMasks workbench module; current verification evidence is in current_task.md. Existing image.paint v1 native data remains readable without automatic migration.

## Layer asset persistence

`PaintLayersStore` stores a standalone layer asset by content hash. NYFL v1 metadata retains canvas dimensions and bottom-to-top layer IDs, names, opacity, visibility, image hashes and optional mask hashes. NYFM v1 stores linear coverage8 masks with dimensions and bottom-left rows. Existing NYFI v1 image blobs are reused.

The reader validates the complete bounded metadata, IDs, text, flags, hashes and the shared 32 MiB estimated payload budget before fetching any dependency. Image and mask dimensions must match the owning canvas before pixel allocation. Image/mask blobs are published before the metadata blob. Corrupted dependency hashes are rejected. The budget describes retained payload; temporary decode/composite allocations add overhead.

This asset API alone is not a native project; the graph integration below supplies project ownership. The codec accepts blob callbacks so graph storage can collect dependencies without a second file ownership model.

## Layered graph integration

`image.paint-layers` v1 is a separate typed Mesh→Image node. `GraphNode.LayeredPaint` owns the immutable stack and its UV/domain binding. Its `PaintImage` is a derived composite cached on the immutable node; the codec stores the stack and referenced images/masks, not a second editable flattened image. The layer codec and composition semantics are versioned with this node.

Native graph schema3 now collects every layer/mask dependency through the existing blob callback. Old image.paint v1 encoding remains intact; legacy stroke commands reject the layered node so they cannot silently flatten it. `PaintLayerChange` holds immutable typed intent, `LayerEditing` validates stack/UV/domain context and produces a replacement graph node, and `LayerOperations` connects editing and explicit migration to shared retry/Undo semantics.

UV changes retain the stack as unresolved. Explicit rebind preserves every layer and checks the stack hash as well as image/UV/input hashes, including edits to invisible layers that do not change the composite. Surface/PNG output uses the same evaluated composite as preview. The GUI edits the selected layer image in 2D and shows the composite in 3D. Empty stacks allow adding a layer but disable brush, deletion and appearance controls.

Explicit migration retains legacy RGBA bytes, UV/domain and node ID; Undo restores the legacy node. The first effective full-opacity unmasked layer shares its immutable image directly, preserving RGB even where alpha is zero. This matters for the currently opaque preview/export material. Subsequent layers use source-over composition.

## Surface input preparation

`SurfacePaintMesh` prepares an immutable mesh/transform BVH and resolves avatar-space rays to nearest triangle UVs and distances. `SurfaceRayGeometry` owns double-precision intersection math. Out-of-range UVs remain occluding hits, rather than exposing a farther surface. Double-sided queries match the current preview; explicit back-face culling is also supported. `SurfaceUvContinuity` uses logical vertex IDs and exact endpoint UVs for shared-edge continuity, without welding coincident unrelated geometry.

`SurfaceStrokeSampler` resamples screen-space input (2px by default), breaks on misses/unpaintable UV/discontinuous edges, and produces immutable path snapshots. Ray samples and UV points have independent shared budgets. Failed gestures cannot produce a committable snapshot. `AuthoringWorkbench.SurfacePaint` connects this input to final-output layered Paint, owns capture/cancel behavior, and commits one color/mask path command. `BaseColorSurface` owns temporary preview textures separately from the committed texture. See docs/Surface-Paint-Development.md for the remaining precision and manual acceptance work.

`SurfaceBoundaryRefinement` bisects observed triangle/UV-validity/background transitions to 1/64 screen pixel (maximum depth 12), emitting intermediate hits in stroke order. All refinement queries and UV points consume the existing shared gesture budgets.

`PaintPathSimplifier` separates up to 4096 raw surface UV samples from the existing 1024-point immutable command path. It reduces each section independently with bounded Douglas-Peucker work and UV error at most `1/(8*PaintImage.MaxDimension)`. Section endpoints and gaps survive; irreducible output over budget invalidates the gesture. `SurfaceStrokeSampler.PointCount` counts raw UV samples; use `Snapshot().PointCount` for retained command points. Snapshot results are cached until raw input changes. Existing 2D path APIs and stored images are unchanged.

`SurfaceScreenProjection` clips world triangles against camera near/far depth before projection. `SurfaceScreenCoverage` indexes the projected triangles in a 2D BVH and supplies segment entry/exit parameters and interval midpoints, finding narrow surfaces hidden between equal coarse hits. These hints join the regular sampling schedule; raycast still determines nearest visibility. The Unity host reuses a coverage snapshot while mesh, placement, exact camera matrices, viewport and clipping planes agree; camera/viewport changes cancel the gesture. Sub-texel occluders split paths, but brush footprints may overlap across the split.

`SurfaceCameraSnapshot` stores copied clip X/Y/W and depth rows, viewport and clipping distances. Its projection and coverage methods are engine-independent and can run on a worker. `SurfaceCameraCapture` is the Unity-thread adapter; no live camera delegate is retained. Current GUI coverage construction remains synchronous until preparation-job ownership and cancellation are connected.

`SurfacePreparationQueue` now owns one worker and one replaceable pending request. Inputs copy logical IDs, ready results include both BVHs, and only the latest generation can publish. Clear/dispose cancel and discard stale completions without waiting on the UI thread. A successful ray snapshot can be reused for camera-only changes. Core concurrency tests cover coalescing, late results, cancellation and recovery; GUI adoption remains pending. See docs/Surface-Preparation.md.

`PaintStrokePath` owns disconnected UV polylines for one gesture. All sections share the 1024-point and 16M-pixel-visit budgets. `BrushCoverage.RasterizePaths` unions every section before applying image alpha or mask strength once. Existing single-path APIs use this same rasterizer. `PaintLayerChange.PathStroke` and `MaskPathStroke` preserve section boundaries in the command fingerprint and commit all sections as one Undo entry. This does not yet determine where a 3D input crosses a UV seam; the input assembler must supply those breaks.

## Mask stroke behavior

`BrushCoverage` owns bounded UV path validation and union coverage rasterization, shared by color and mask brushes. `PaintMaskStroke` interpolates each coverage byte toward a target (0 hides, 255 reveals) using union coverage times strength 0..1, then rounds once. It never applies an sRGB transfer to mask data. Repeated samples over the same path do not accumulate within one stroke; separate strokes do.

`PaintLayerChange.MaskStroke` freezes path, radius, target and strength. Layer commands require an existing mask, preserve color pixels, validate stack/UV/domain context, and provide shared retry, Undo and native persistence. Target and strength participate in the command fingerprint. Adding or removing a mask remains a separate explicit operation.

`AuthoringWorkbench.PaintMasks` owns target selection, white-mask creation/removal, hide/reveal controls and layer naming. Mask strokes use the same `PaintMaskStroke` for temporary preview and document commit. `PaintCanvas` snapshots the supplied preview operation at pointer down; switching target cancels the gesture. `PaintMaskDisplay` creates an opaque grayscale visualization (coverage byte repeated in RGB), never source data for mask editing. Immutable mask display caching avoids rebuilding the display on unrelated refreshes. Outputs always use the composited color image, including while the 2D editor shows a mask.


