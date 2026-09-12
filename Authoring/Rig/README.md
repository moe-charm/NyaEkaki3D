# Rig core

`SkeletonDefinition` owns a stable-ID rest-pose hierarchy. Bone coordinates use the owning mesh's avatar-rest space; pose transforms are deliberately not inferred from the Unity scene.

`SkinBinding` binds every render mesh vertex to one to four explicitly named bones and normalizes each vertex's positive weights. It pins the mesh topology hash and skeleton content hash so a later topology or rig change cannot silently reuse the binding. This slice does not yet deform meshes, create a humanoid preset, paint weights, or export skin data; those are the next C2 modules.
