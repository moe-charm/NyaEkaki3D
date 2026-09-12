# Secondary motion core

`SecondaryMotionAsset` is the simulator-neutral setup owned by the authoring core. It keeps source-pinned skeleton and mesh topology identities, ordered stable-bone chains, fixed vertex indices, collider groups and a versioned target profile. `SecondaryMotionOutputKind` makes bone-pose output and mesh-deformation output explicit; fixed vertices cannot be attached to a bone-pose asset.

`SecondaryMotionCodec` writes the bounded `NYSM` v1 binary payload. `ReadDocument` returns unknown wire versions as an opaque, hash-pinned document so opening a project does not reset settings when the adapter package is unavailable. `SecondaryMotionAsset.ValidateFor` rejects changed skeletons, mesh topology and missing stable IDs before an adapter runs.

`ISecondaryMotionAdapter` is the calculation boundary. It receives a validated `SecondaryMotionEvaluationRequest` and returns a temporary `SecondaryMotionEvaluationResult` containing exactly the output kind declared by the asset. Unity, VRM, PhysBones and MagicaCloth2 types do not enter this module. `Import/VrmSecondaryMotionMigration` converts already-resolved VRM1 spring chains into this common topology; VRM0 source-node preview remains a separate adapter until its source-space output contract is defined.

The codec and contract are currently standalone Core assets. Native project attachment wiring, PhysBones DTO/Unity Bridge, GUI/MCP ownership and vendor package adapters remain SIM-02 onward work.
