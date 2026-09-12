# Typed graph evaluation

This module contains the CPU-only foundation of C1-A. It does not reference Unity, an editor UI, or an MCP transport.

| File | Responsibility |
|---|---|
| GraphContracts | Immutable graph, edges, built-in node/port definitions and capacity limits |
| GraphNode | Typed immutable parameters, edit/rig payloads and opaque unknown-version payload |
| GraphValidator | Identity, single-input connection, port types and whole-graph cycle validation |
| PrimitiveGeometry | Bounded plane generation with UV, normals and tangents |
| GraphEvaluation | Deterministic evaluation order, mesh/domain snapshots, skeleton/binding/pose values and node-local diagnostics |
| GraphEditContext | Reference-space edits pinned to a graph/node/input snapshot/domain |
| StaticMeshGraph | Deterministic Source→EditMesh→Output representation of existing static objects |

MeshSource, Plane, EditMesh, Output, Scalar, `rig.skeleton`, `rig.skin-bind`, `rig.pose` and `rig.skin-deform` v1 are implemented. A scalar output may drive plane width/height in metres. Other types and future versions retain opaque payload in the in-memory graph and produce UNKNOWN_NODE; they are not executed. Wrong known port types, invalid edges and cycles are rejected when constructing a candidate graph, including disconnected components. Missing inputs are an incomplete evaluation, with independent node previews still available. This initial evaluator conservatively requires no diagnostics anywhere for IsComplete.

An EditMesh offset is in reference metres and pinned to both an input snapshot hash and source element domain. Upstream geometry changes do not silently rebase old offsets. Contexts must be reacquired after an upstream change, and a pre-existing stale payload still needs an explicit future rebase operation. Disabling an EditMesh bypasses its payload; it does not delete it.

The current element domain consists of a source node identity plus triangle topology hash. Element IDs inside the static payload are baseline indices. This does **not** claim the polygon/corner identity or seam-welding behavior planned for C1-B. No graph layout lives here.

## Integration boundary

Existing AuthoringObject data is owned by a deterministic Source→EditMesh→Output graph. Its old BaselineMesh/Transform/Offsets/LayerEnabled APIs are views onto those nodes, and the existing command/Undo/Bake path evaluates the graph. Legacy state and geometry hashes remain unchanged.

Static objects continue to use native schema 2. Graph objects use native schema 3 and `GraphBlobStore` references, including incomplete graphs, unknown payloads, skeleton blobs and skin-binding blobs. The graph node canvas and command layer are separate from this Unity-independent evaluator; remote creation of rig payload nodes and skin deformation output remain later work.

Unknown nodes own an immutable byte payload. `UnknownPayload` is a UTF-8 convenience view when `UnknownPayloadIsText` is true, otherwise it is a base64 view. Encoding always uses `UnknownPayloadBytes`, so an unsupported future version's binary parameters survive without text interpretation.

Core regression coverage is in Tests/Authoring.Core/GraphTests.cs. Do not infer runtime GUI, general native graph round-trip, or target-profile compatibility from those tests alone.
