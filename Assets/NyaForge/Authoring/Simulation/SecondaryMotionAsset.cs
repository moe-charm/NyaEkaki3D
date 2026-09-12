using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Simulation
{
    /// <summary>Ordered stable-bone chain shared by all bone-output adapters.</summary>
    public sealed class SecondaryMotionChain
    {
        public const int MaxBones = 1024;
        public string Name { get; }
        public IReadOnlyList<string> BoneIds { get; }
        public IReadOnlyList<int> ColliderGroupIndices { get; }

        public SecondaryMotionChain(string name, IEnumerable<string> boneIds, IEnumerable<int> colliderGroupIndices)
        {
            Checks.Name(name); Checks.Require(boneIds != null && colliderGroupIndices != null, "INVALID_SIMULATION", "Secondary motion chain data is required.");
            var bones = boneIds.ToArray(); var groups = colliderGroupIndices.ToArray();
            Checks.Require(bones.Length > 0 && bones.Length <= MaxBones, "BUDGET_EXCEEDED", "Secondary motion chain exceeds bone capacity.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in bones) { Checks.Id(id); Checks.Require(seen.Add(id), "INVALID_SIMULATION", "Secondary motion chain repeats a bone."); }
            foreach (var group in groups) Checks.Require(group >= 0, "INVALID_SIMULATION", "Secondary motion collider group index cannot be negative.");
            Name = name; BoneIds = Array.AsReadOnly(bones); ColliderGroupIndices = Array.AsReadOnly(groups);
        }
    }

    /// <summary>Collider in avatar rest space. An empty BoneId means a root/world-owned collider.</summary>
    public sealed class SecondaryMotionCollider
    {
        public string BoneId { get; }
        public Vec3 Center { get; }
        public float Radius { get; }
        public Vec3? Tail { get; }

        public SecondaryMotionCollider(string boneId, Vec3 center, float radius, Vec3? tail = null)
        {
            Checks.Require(boneId != null && (boneId == "" || Guid.TryParseExact(boneId, "D", out _)), "INVALID_SIMULATION", "Collider bone identity is invalid.");
            Checks.Finite(center); Checks.Finite(radius); Checks.Require(radius >= 0f && radius <= 10f, "INVALID_SIMULATION", "Secondary motion collider radius is outside 0 to 10.");
            if (tail.HasValue) Checks.Finite(tail.Value);
            BoneId = boneId; Center = center; Radius = radius; Tail = tail;
        }
    }

    /// <summary>Named collider collection referenced by stable chain-local indices.</summary>
    public sealed class SecondaryMotionColliderGroup
    {
        public const int MaxColliders = 64;
        public string Name { get; }
        public IReadOnlyList<SecondaryMotionCollider> Colliders { get; }

        public SecondaryMotionColliderGroup(string name, IEnumerable<SecondaryMotionCollider> colliders)
        {
            Checks.Name(name); Checks.Require(colliders != null, "INVALID_SIMULATION", "Secondary motion colliders are required.");
            var values = colliders.ToArray(); Checks.Require(values.Length <= MaxColliders, "BUDGET_EXCEEDED", "Secondary motion collider group exceeds capacity.");
            foreach (var collider in values) Checks.Require(collider != null, "INVALID_SIMULATION", "Secondary motion collider cannot be null.");
            Name = name; Colliders = Array.AsReadOnly(values);
        }
    }

    /// <summary>Native, simulator-neutral topology and target profile for one secondary-motion setup.</summary>
    public sealed class SecondaryMotionAsset
    {
        public const int MaxChains = 256;
        public const int MaxColliderGroups = 256;
        public const int MaxFixedVertices = AuthoringLimits.MaxVertices;
        public SecondaryMotionProfile Profile { get; }
        public string SkeletonHash { get; }
        public string MeshTopologyHash { get; }
        public IReadOnlyList<SecondaryMotionChain> Chains { get; }
        public IReadOnlyList<SecondaryMotionColliderGroup> ColliderGroups { get; }
        public IReadOnlyList<int> FixedVertexIndices { get; }
        public string ContentHash { get; }

        public SecondaryMotionAsset(SecondaryMotionProfile profile, string skeletonHash, string meshTopologyHash,
            IEnumerable<SecondaryMotionChain> chains, IEnumerable<SecondaryMotionColliderGroup> colliderGroups, IEnumerable<int> fixedVertexIndices)
        {
            Checks.Require(profile != null, "INVALID_SIMULATION", "Secondary motion profile is required.");
            HashOptional(skeletonHash, "Skeleton hash"); HashOptional(meshTopologyHash, "Mesh topology hash");
            Checks.Require(profile.OutputKind == SecondaryMotionOutputKind.BonePose ? !string.IsNullOrEmpty(skeletonHash) : !string.IsNullOrEmpty(meshTopologyHash),
                "INVALID_SIMULATION", "The selected output kind requires its source identity.");
            var chainValues = (chains ?? Array.Empty<SecondaryMotionChain>()).ToArray();
            var colliderValues = (colliderGroups ?? Array.Empty<SecondaryMotionColliderGroup>()).ToArray();
            var fixedValues = (fixedVertexIndices ?? Array.Empty<int>()).ToArray();
            Checks.Require(chainValues.Length <= MaxChains && colliderValues.Length <= MaxColliderGroups, "BUDGET_EXCEEDED", "Secondary motion asset exceeds capacity.");
            Checks.Require(fixedValues.Length <= MaxFixedVertices, "BUDGET_EXCEEDED", "Secondary motion fixed region exceeds capacity.");
            foreach (var chain in chainValues) Checks.Require(chain != null, "INVALID_SIMULATION", "Secondary motion chain cannot be null.");
            foreach (var group in colliderValues) Checks.Require(group != null, "INVALID_SIMULATION", "Secondary motion collider group cannot be null.");
            Checks.Require(chainValues.Length == 0 || !string.IsNullOrEmpty(skeletonHash), "INVALID_SIMULATION", "Bone chains require a skeleton identity.");
            Checks.Require(fixedValues.Length == 0 || !string.IsNullOrEmpty(meshTopologyHash), "INVALID_SIMULATION", "Fixed vertices require a mesh topology identity.");
            foreach (var group in colliderValues) foreach (var collider in group.Colliders)
                Checks.Require(collider.BoneId == "" || !string.IsNullOrEmpty(skeletonHash), "INVALID_SIMULATION", "Bone-attached colliders require a skeleton identity.");
            foreach (var chain in chainValues) foreach (int index in chain.ColliderGroupIndices)
                Checks.Require(index < colliderValues.Length, "INVALID_SIMULATION", "Secondary motion chain references a missing collider group.");
            var seen = new HashSet<int>(); foreach (int index in fixedValues)
                Checks.Require(index >= 0 && seen.Add(index), "INVALID_SIMULATION", "Secondary motion fixed vertex index is invalid or repeated.");
            Checks.Require(profile.OutputKind != SecondaryMotionOutputKind.BonePose || chainValues.Length > 0,
                "INVALID_SIMULATION", "Bone-pose output requires at least one chain.");
            Checks.Require(profile.OutputKind != SecondaryMotionOutputKind.BonePose || fixedValues.Length == 0,
                "INVALID_SIMULATION", "Fixed vertices belong to mesh-deformation output, not bone-pose output.");
            Profile = profile; SkeletonHash = skeletonHash ?? ""; MeshTopologyHash = meshTopologyHash ?? "";
            Chains = Array.AsReadOnly(chainValues); ColliderGroups = Array.AsReadOnly(colliderValues);
            FixedVertexIndices = Array.AsReadOnly(fixedValues.OrderBy(index => index).ToArray());
            ContentHash = Checks.Hash(SecondaryMotionCodec.Write(this));
        }

        /// <summary>Checks pinned identities and fixed-region indices against the current authoring inputs.</summary>
        public void ValidateFor(SkeletonDefinition skeleton, MeshData mesh)
        {
            if (SkeletonHash != "") Checks.Require(skeleton != null && skeleton.ContentHash == SkeletonHash, "SIMULATION_SKELETON_CHANGED", "Secondary motion skeleton identity changed; rebind the setup.");
            if (MeshTopologyHash != "") Checks.Require(mesh != null && mesh.TopologyHash == MeshTopologyHash, "SIMULATION_TOPOLOGY_CHANGED", "Secondary motion mesh topology changed; rebind the setup.");
            if (skeleton != null && SkeletonHash != "")
            {
                foreach (var chain in Chains) foreach (var bone in chain.BoneIds) Checks.Require(skeleton.ById.ContainsKey(bone), "SIMULATION_BONE_MISSING", "Secondary motion references a missing bone.");
            }
            if (Profile.OutputKind == SecondaryMotionOutputKind.BonePose)
                Checks.Require(skeleton != null, "INVALID_SIMULATION", "Bone-pose output requires a skeleton.");
            if (FixedVertexIndices.Count > 0)
            {
                Checks.Require(mesh != null, "INVALID_SIMULATION", "Fixed vertices require a mesh.");
                foreach (int index in FixedVertexIndices) Checks.Require(index < mesh.VertexCount, "SIMULATION_VERTEX_MISSING", "Secondary motion fixed vertex is outside the mesh.");
            }
            foreach (var group in ColliderGroups) foreach (var collider in group.Colliders)
                if (collider.BoneId != "") Checks.Require(skeleton != null && skeleton.ById.ContainsKey(collider.BoneId), "SIMULATION_BONE_MISSING", "Secondary motion collider references a missing bone.");
        }

        static void HashOptional(string hash, string label)
        {
            Checks.Require(hash != null, "INVALID_SIMULATION", label + " is required as an empty or SHA-256 value.");
            if (hash != "") Checks.HashText(hash);
        }
    }
}
