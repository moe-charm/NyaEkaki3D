# Evidence Core

EvaluatedSnapshot.Acquire takes the workspace lock and captures committed final output with document identity. SnapshotId identifies an acquisition; OutputContentHash compares content. Empty and incomplete snapshots have no Value/Metrics. Faceless output is valid with zero render counts and optional loose-point bounds. Stale preview geometry is never relabeled as current.

EvidenceMetrics distinguishes render counts from nullable logical topology counts. Bounds use evaluated render positions, or loose polygon vertices when no faces exist, transformed into avatar space. Images are unique hashes; assigned materials and render submeshes are separate counts.

Not yet implemented: explicit node input/output, capture/manifest, poses, source epoch, validation results and MCP. WorkbenchCapture remains UI-test-only.

Node mesh input/output acquisition is now supported with EvidenceTarget.NodeMesh(objectId, graphId, nodeId, input). Target.Kind separates node input from node output. OutputNodeId always names the graph final node; Target.NodeId identifies an explicitly selected stage. A ready intermediate target does not imply a complete graph: FinalEvaluationComplete and Diagnostics describe final evaluation separately. Unsupported ports and mismatched identities are rejected, unresolved targets return Incomplete without substituting stale geometry. Image/Material ports are not yet supported.

EvidenceManifestCodec.Write emits explicit metadata schema 1. EvidenceStore.SaveMetadata atomically publishes snapshotId.evidence.json under a caller-selected directory, preserving previous files and accepting identical retries. Capture is not_requested, validations are not_run and artifacts are empty. Null source/workspace/pose values are unavailable, not zero. Reader and model image publication are still pending.

EvidenceManifestReader validates metadata schema 1 and returns an isolated EvidenceRecord, not a reconstructed geometry snapshot. It checks structural and state consistency; it does not authenticate files or prove external geometry/fit. Image-bearing capture results remain pending.

EvidenceCaptureSet/Codec/Store publish up to eight same-snapshot images, camera settings and hash references. Metadata and PNGs precede the capture manifest. Existing conflicting files are preserved. A capture record reports capture completion separately from the immutable acquisition metadata. Capture-package reader and Unity capture-to-store integration remain pending.

EvidenceCaptureReader now checks fixed local filenames, metadata identity/hash, camera/profile, PNG hash/framing/dimensions and returns isolated records. Hash consistency is not authentication. Runtime capture package roundtrip is verified with two repeated captures; distinct multiview and user-facing capture controls remain pending.

Secondary-motion evidence is a separate transient contract. `SecondaryMotionCaptureRecord` and `SecondaryMotionCaptureCodec` record one fixed 1/60-second preview run: input/config hashes, adapter/package/target/build identity, warmup, pose/root/collider conditions, frame step counts and PNG hashes. The record does not reuse `EvidenceCaptureSet`, because its frames intentionally come from different transient poses rather than one immutable evaluated snapshot. Unity reuses the lower-level `EvidenceModelCapture.CapturePng` renderer for each dynamic graph value; the native document and saved project remain unchanged.
