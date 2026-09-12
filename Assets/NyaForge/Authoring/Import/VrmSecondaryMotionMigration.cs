using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;

namespace NyaForge.Authoring.Import
{
    /// <summary>Converts an existing resolved VRM spring preview into the simulator-neutral asset contract.</summary>
    public static class VrmSecondaryMotionMigration
    {
        public static SecondaryMotionAsset FromVrm1(VrmSpringSession source, ImportedRigSession rig, AuthoringGraph graph, PoseSet pose)
        {
            Checks.Require(source != null && rig != null && graph != null && pose != null, "INVALID_VRM", "VRM spring migration inputs are required.");
            Checks.Require(source.Format == "vrm1", "UNSUPPORTED_FORMAT", "Only VRM1 authored-bone spring migration is supported by this contract.");
            var chains = Vrm1SpringRuntimeAdapter.CreateChains(source, rig, graph, pose);
            var colliders = VrmSpringColliderAdapter.Convert(source, rig, graph, pose);
            var skeleton = graph.Nodes[rig.SkeletonNodeId].Skeleton;
            string meshHash = rig.SourceSkinBinding == null ? "" : rig.SourceSkinBinding.MeshTopologyHash;
            var profile = new SecondaryMotionProfile("nyaforge.vrm-spring", "vrm1", 1, "1", "", SecondaryMotionOutputKind.BonePose, Array.Empty<byte>());
            return FromSpringChains(profile, skeleton.ContentHash, meshHash, chains, colliders);
        }

        /// <summary>Builds common topology from any already-resolved bone spring adapter.</summary>
        public static SecondaryMotionAsset FromSpringChains(SecondaryMotionProfile profile, string skeletonHash, string meshTopologyHash,
            IEnumerable<SpringBoneChain> chains, IEnumerable<SpringBoneColliderGroup> colliderGroups)
        {
            Checks.Require(profile != null && profile.OutputKind == SecondaryMotionOutputKind.BonePose, "INVALID_SIMULATION", "A bone-pose profile is required.");
            Checks.Require(chains != null && colliderGroups != null, "INVALID_SIMULATION", "Resolved spring data is required.");
            var commonChains = chains.Select(chain =>
                new SecondaryMotionChain(chain.Name, chain.Joints.Select(joint => joint.BoneId), chain.ColliderGroupIndices));
            var commonColliders = colliderGroups.Select(group =>
                new SecondaryMotionColliderGroup(group.Name, group.Colliders.Select(collider =>
                    new SecondaryMotionCollider("", collider.Center, collider.Radius, collider.Tail))));
            return new SecondaryMotionAsset(profile, skeletonHash, meshTopologyHash, commonChains, commonColliders, Array.Empty<int>());
        }
    }
}
