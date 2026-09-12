# Graph asset storage

`GraphBlobStore` stores native graph schema 3 assets referenced by `project.nyaforge.json`. It owns immutable content-addressed graph and dependency blobs; `GraphProjectCodec` owns the manifest and optimistic save version. Static objects remain on native schema 2. Rig skeleton and skin-binding node payloads use explicit NYRS/NYRB v1 blobs inside this graph store.

- `Write(directory, graph)` takes the project writer lock, encodes and validates the asset budget, publishes immutable dependencies, then publishes the graph blob last. Rewriting the same graph is idempotent. Existing corrupt blobs are rejected rather than replaced.
- `WriteLocked` is internal, for the future ProjectStore transaction that already owns that same lock.
- `Read(directory, hash)` checks each graph/mesh/delta blob's size and content hash before decode. Graph structure goes through the normal type/port/cycle validator. Incomplete outputs and unknown nodes are retained; loading is not evidence that the graph evaluates successfully.

The graph wire format is **NYFG v1**, independent of native project and Bake versions. It has a bounded graph/node/edge envelope and explicit per-node versioned payload. Node and edge order are canonicalized for hashing. Mesh and delta data are referenced by hash and deduplicated. Known node payloads reject trailing bytes, invalid flags, invalid text, malformed counts and unsupported wire versions. Unknown type/version payloads remain opaque bytes.

Limits: 128 nodes, 512 edges, 32 KiB per node payload, existing 16 MiB per blob and 64 MiB total unique referenced bytes per graph. Text byte lengths are bounded before allocation. Reaching an unknown node is not arbitrary code execution or runtime type-name deserialization.

Coverage: `Tests/Authoring.Core/GraphStorageTests.cs`, plus Windows Player scale 1/100 asset round trips in `AuthoringWorkbench.Verification.cs`. General project migration/commit and GUI canvas acceptance remain separate checks.
