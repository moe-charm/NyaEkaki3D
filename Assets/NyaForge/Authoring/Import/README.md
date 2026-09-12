# Import boundary

`GlbImporter` is the first runtime import adapter. It accepts a bounded GLB v2 containing one mesh with up to the authoring submesh budget of triangle primitives, backed by one BIN buffer. POSITION is required; NORMAL, TANGENT and TEXCOORD_0 are retained when present and must use the same presence layout on every primitive. Primitive-local vertices are concatenated in order and their indices remain separate submeshes. POSITION morph targets become a mesh-pinned `MorphSet`, and `extras.targetNames` supplies optional names.

The adapter rejects skin bindings, mixed attribute or morph layouts, sparse accessors, unsupported component/type combinations and unknown GLB chunks before creating a graph. It returns a source hash and explicit warnings so a successful import is not mistaken for a complete avatar import. It does not infer a Blender vertex correspondence, reconstruct quads, or claim humanoid/VRM compatibility.

The Windows workbench uses this adapter only from an empty project and creates a native Source → optional MorphDeform → Output graph. The original file is read locally and is not copied into the public repository. VRM metadata/humanoid, skin, normal/tangent morph deltas and export are separate adapters.
