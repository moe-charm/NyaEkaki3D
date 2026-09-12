# Geometry

MeshRaycast owns an immutable triangle BVH for MeshData and RestTransform. It requires no UVs or paint state. Results contain triangle/submesh IDs, source vertex indices, barycentric U/V, world distance and facing. SurfacePaintMesh composes it with UV interpolation and paint continuity. RayGeometry holds internal double-precision math. Existing ray validation codes remain compatible with the paint API.
