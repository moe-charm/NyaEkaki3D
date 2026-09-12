using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Validates one simulation call and resolves each joint's own chain collider references.</summary>
    internal sealed class SpringSimulationInputs
    {
        internal readonly IReadOnlyList<SpringBoneJointSettings> Joints;
        internal readonly IReadOnlyDictionary<string, IReadOnlyList<SpringBoneCollider>> CollidersByBone;
        internal readonly string ChainHash;

        SpringSimulationInputs(List<SpringBoneJointSettings> joints, Dictionary<string, IReadOnlyList<SpringBoneCollider>> scopes, string hash)
        {
            Joints = joints.AsReadOnly();
            CollidersByBone = new System.Collections.ObjectModel.ReadOnlyDictionary<string, IReadOnlyList<SpringBoneCollider>>(scopes);
            ChainHash = hash;
        }

        internal static SpringSimulationInputs Validate(SkeletonDefinition skeleton, PoseSet pose, IReadOnlyList<SpringBoneChain> chains, IReadOnlyList<SpringBoneColliderGroup> colliders, SpringBoneState previous)
        {
            Checks.Require(skeleton != null && pose != null && chains != null, "INVALID_SPRING", "Spring skeleton, pose, and chains are required.");
            var validPose = pose.ValidateFor(skeleton); Checks.Require(chains.Count <= SpringBoneSimulator.MaxChains, "BUDGET_EXCEEDED", "Spring chain budget exceeded.");
            int total = 0; var joints = new List<SpringBoneJointSettings>(); var seen = new HashSet<string>(StringComparer.Ordinal); var scopes = new Dictionary<string, IReadOnlyList<SpringBoneCollider>>(StringComparer.Ordinal);
            if (colliders != null)
            {
                Checks.Require(colliders.Count <= 256, "BUDGET_EXCEEDED", "Spring collider group budget exceeded.");
                foreach (var group in colliders) Checks.Require(group != null, "INVALID_SPRING", "Spring collider group cannot be null.");
            }
            foreach (var chain in chains)
            {
                Checks.Require(chain != null, "INVALID_SPRING", "Spring chain cannot be null."); total += chain.Joints.Count;
                Checks.Require(total <= SpringBoneSimulator.MaxTotalJoints, "BUDGET_EXCEEDED", "Spring joint budget exceeded.");
                var resolved = new List<SpringBoneCollider>();
                foreach (var index in chain.ColliderGroupIndices.Distinct().OrderBy(value => value)) { Checks.Require(colliders != null && index < colliders.Count, "SPRING_COLLIDER_MISSING", "Spring collider group does not exist."); resolved.AddRange(colliders[index].Colliders); }
                var scope = resolved.AsReadOnly();
                foreach (var joint in chain.Joints)
                {
                    Checks.Require(skeleton.ById.ContainsKey(joint.BoneId), "SPRING_BONE_UNKNOWN", "Spring joint bone does not exist in the skeleton.");
                    Checks.Require(seen.Add(joint.BoneId), "DUPLICATE_SPRING_JOINT", "A spring joint bone may only appear once."); joints.Add(joint); scopes.Add(joint.BoneId, scope);
                }
            }
            string chainHash = HashChains(chains);
            if (previous != null)
            {
                Checks.Require(previous.SkeletonHash == skeleton.ContentHash, "SPRING_SKELETON_CHANGED", "Spring state belongs to another skeleton.");
                Checks.Require(previous.ChainHash == chainHash, "SPRING_CHAIN_CHANGED", "Spring state belongs to another chain definition.");
                foreach (var joint in joints) Checks.Require(previous.CurrentTails.ContainsKey(joint.BoneId) && previous.PreviousTails.ContainsKey(joint.BoneId), "SPRING_STATE_MISSING", "Spring state is missing a joint tail.");
            }
            return new SpringSimulationInputs(joints, scopes, chainHash);
        }

        private static string HashChains(IReadOnlyList<SpringBoneChain> chains)
        {
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream, Encoding.UTF8)) { writer.Write(1); writer.Write(chains.Count); foreach (var chain in chains) writer.Write(chain.ContentHash); return Checks.Hash(stream.ToArray()); }
        }

    }
}
