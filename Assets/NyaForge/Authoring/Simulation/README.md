# Secondary motion core

`SecondaryMotionAsset` is the simulator-neutral setup owned by the authoring core. It keeps source-pinned skeleton and mesh topology identities, ordered stable-bone chains, fixed vertex indices, collider groups and a versioned target profile. `SecondaryMotionOutputKind` makes bone-pose output and mesh-deformation output explicit; fixed vertices cannot be attached to a bone-pose asset.

`SecondaryMotionCodec` writes the bounded `NYSM` v1 binary payload. `ReadDocument` returns unknown wire versions as an opaque, hash-pinned document so opening a project does not reset settings when the adapter package is unavailable. `SecondaryMotionAsset.ValidateFor` rejects changed skeletons, mesh topology and missing stable IDs before an adapter runs.

`ISecondaryMotionAdapter` is the calculation boundary. It receives a validated `SecondaryMotionEvaluationRequest` and returns a temporary `SecondaryMotionEvaluationResult` containing exactly the output kind declared by the asset. Unity, VRM, PhysBones and MagicaCloth2 types do not enter this module. `Import/VrmSecondaryMotionMigration` converts already-resolved VRM1 spring chains into this common topology; VRM0 source-node preview remains a separate adapter until its source-space output contract is defined.

`PhysBonesTargetProfile` is the target-specific DTO for a VRChat PhysBones Bridge. It stores SDK/package versions, a skeleton hash, ordered stable BoneIds, root/endpoint/exclusion/branch mapping, collider-group references, bounded limits, keyframe curves and interaction flags. `PhysBonesTargetCodec` persists it as strict `NYPP` v1 and retains unknown versions as opaque bytes. `PhysBonesCapabilities` plus `PhysBonesLossReport.Compare` produces an explicit supported/unsupported feature list before any Unity component is written; SDK types and vendor assets remain outside Core.

SIM-02 currently covers this DTO, codec, validation and loss-report boundary. Native project attachment wiring, the Unity SDK component writer, GUI/MCP ownership and vendor package adapters remain follow-up work. A passing Core report does not establish VRChat SDK compilation or in-avatar motion.
