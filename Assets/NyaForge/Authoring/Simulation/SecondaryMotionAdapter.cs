using System;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Simulation
{
    /// <summary>Validated input shared by every secondary-motion backend.</summary>
    public sealed class SecondaryMotionEvaluationRequest
    {
        public SecondaryMotionAsset Asset { get; }
        public SkeletonDefinition Skeleton { get; }
        public MeshData Mesh { get; }
        public PoseSet BasePose { get; }
        public float DeltaTime { get; }

        public SecondaryMotionEvaluationRequest(SecondaryMotionAsset asset, SkeletonDefinition skeleton, MeshData mesh, PoseSet basePose, float deltaTime)
        {
            Checks.Require(asset != null, "INVALID_SIMULATION", "Secondary motion asset is required.");
            Checks.Finite(deltaTime); Checks.Require(deltaTime >= 0f && deltaTime <= 0.25f, "INVALID_DELTA_TIME", "Secondary motion delta time must be between 0 and 0.25 seconds.");
            if (basePose != null) basePose.ValidateFor(skeleton);
            asset.ValidateFor(skeleton, mesh);
            Checks.Require(asset.Profile.OutputKind != SecondaryMotionOutputKind.BonePose || basePose != null,
                "INVALID_SIMULATION", "Bone-pose evaluation requires a base pose.");
            Asset = asset; Skeleton = skeleton; Mesh = mesh; BasePose = basePose; DeltaTime = deltaTime;
        }
    }

    /// <summary>One temporary simulation frame. Only the profile's declared output is populated.</summary>
    public sealed class SecondaryMotionEvaluationResult
    {
        public string AssetHash { get; }
        public SecondaryMotionOutputKind OutputKind { get; }
        public PoseSet BonePose { get; }
        public MeshData Mesh { get; }

        private SecondaryMotionEvaluationResult(SecondaryMotionAsset asset, SecondaryMotionOutputKind kind, PoseSet pose, MeshData mesh)
        {
            AssetHash = asset.ContentHash; OutputKind = kind; BonePose = pose; Mesh = mesh;
        }

        public static SecondaryMotionEvaluationResult Bone(SecondaryMotionAsset asset, PoseSet pose)
        {
            Checks.Require(asset != null && asset.Profile.OutputKind == SecondaryMotionOutputKind.BonePose, "INVALID_SIMULATION", "Asset does not declare bone-pose output.");
            Checks.Require(pose != null && pose.SkeletonHash == asset.SkeletonHash, "SIMULATION_SKELETON_CHANGED", "Simulation pose belongs to another skeleton.");
            return new SecondaryMotionEvaluationResult(asset, SecondaryMotionOutputKind.BonePose, pose, null);
        }

        public static SecondaryMotionEvaluationResult MeshDeformation(SecondaryMotionAsset asset, MeshData mesh)
        {
            Checks.Require(asset != null && asset.Profile.OutputKind == SecondaryMotionOutputKind.MeshDeformation, "INVALID_SIMULATION", "Asset does not declare mesh-deformation output.");
            Checks.Require(mesh != null && mesh.TopologyHash == asset.MeshTopologyHash, "SIMULATION_TOPOLOGY_CHANGED", "Simulation mesh belongs to another topology.");
            return new SecondaryMotionEvaluationResult(asset, SecondaryMotionOutputKind.MeshDeformation, null, mesh);
        }
    }

    /// <summary>Backend boundary. Implementations may depend on Unity or a vendor package outside this assembly.</summary>
    public interface ISecondaryMotionAdapter
    {
        SecondaryMotionCapabilities Capabilities { get; }
        SecondaryMotionEvaluationResult Evaluate(SecondaryMotionEvaluationRequest request);
    }
}
