using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Simulation
{
    /// <summary>Creates a new secondary-motion asset only from an explicit old-to-new identity map.</summary>
    public static class SecondaryMotionRebind
    {
        public static SecondaryMotionAsset Apply(SecondaryMotionAsset source, SkeletonDefinition targetSkeleton, MeshData targetMesh,
            IReadOnlyDictionary<string, string> boneIds, IReadOnlyDictionary<int, int> fixedVertexIndices = null)
        {
            Checks.Require(source != null, "INVALID_SIMULATION", "Secondary motion asset is required for rebind.");
            if (source.SkeletonHash != "") Checks.Require(targetSkeleton != null, "INVALID_SIMULATION", "A target skeleton is required for rebind.");
            if (source.MeshTopologyHash != "") Checks.Require(targetMesh != null, "INVALID_SIMULATION", "A target mesh is required for rebind.");

            var referencedBones = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chain in source.Chains) foreach (var bone in chain.BoneIds) referencedBones.Add(bone);
            foreach (var group in source.ColliderGroups) foreach (var collider in group.Colliders) if (collider.BoneId != "") referencedBones.Add(collider.BoneId);
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var id in referencedBones)
            {
                string rebound = null;
                Checks.Require(boneIds != null && boneIds.TryGetValue(id, out rebound), "SIMULATION_REBIND_REQUIRED", "Every referenced bone must have an explicit rebind mapping.");
                Checks.Id(rebound); Checks.Require(targetSkeleton.ById.ContainsKey(rebound), "SIMULATION_BONE_MISSING", "Rebind target bone is missing from the target skeleton.");
                map.Add(id, rebound);
            }
            if (boneIds != null) foreach (var pair in boneIds)
            {
                Checks.Id(pair.Key); Checks.Id(pair.Value);
                Checks.Require(referencedBones.Contains(pair.Key), "SIMULATION_REBIND_EXTRA", "Rebind mapping contains an unreferenced source bone.");
            }
            Checks.Require(map.Values.Distinct(StringComparer.Ordinal).Count() == map.Count, "SIMULATION_REBIND_AMBIGUOUS", "Two source bones cannot map to the same target bone.");

            var chains = source.Chains.Select(chain => new SecondaryMotionChain(chain.Name, chain.BoneIds.Select(id => map[id]), chain.ColliderGroupIndices)).ToArray();
            var colliders = source.ColliderGroups.Select(group => new SecondaryMotionColliderGroup(group.Name, group.Colliders.Select(collider =>
                new SecondaryMotionCollider(collider.BoneId == "" ? "" : map[collider.BoneId], collider.Center, collider.Radius, collider.Tail)))).ToArray();

            var vertices = source.FixedVertexIndices.ToArray();
            bool topologyChanged = source.MeshTopologyHash != "" && targetMesh != null && source.MeshTopologyHash != targetMesh.TopologyHash;
            if (vertices.Length > 0 && topologyChanged)
            {
                Checks.Require(fixedVertexIndices != null, "SIMULATION_REBIND_REQUIRED", "Changed mesh topology requires an explicit fixed-vertex mapping.");
                var mapped = new List<int>();
                foreach (int index in vertices)
                {
                    Checks.Require(fixedVertexIndices.TryGetValue(index, out var rebound), "SIMULATION_REBIND_REQUIRED", "Every fixed vertex must have an explicit rebind mapping.");
                    Checks.Require(rebound >= 0 && rebound < targetMesh.VertexCount, "SIMULATION_VERTEX_MISSING", "Rebind target vertex is outside the target mesh."); mapped.Add(rebound);
                }
                Checks.Require(mapped.Distinct().Count() == mapped.Count, "SIMULATION_REBIND_AMBIGUOUS", "Two fixed vertices cannot map to the same target vertex."); vertices = mapped.ToArray();
            }
            else if (fixedVertexIndices != null)
            {
                foreach (var pair in fixedVertexIndices) Checks.Require(vertices.Contains(pair.Key), "SIMULATION_REBIND_EXTRA", "Rebind mapping contains an unreferenced fixed vertex.");
            }
            var reboundAsset = new SecondaryMotionAsset(source.Profile,
                source.SkeletonHash == "" ? "" : targetSkeleton.ContentHash,
                source.MeshTopologyHash == "" ? "" : targetMesh.TopologyHash,
                chains, colliders, vertices);
            reboundAsset.ValidateFor(targetSkeleton, targetMesh);
            return reboundAsset;
        }
    }
}
