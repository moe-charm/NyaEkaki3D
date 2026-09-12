using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Applies a pinned rest-space skin binding to positions. Attributes remain owned by the source mesh.</summary>
    public static class SkinDeformer
    {
        public static MeshData Apply(MeshData mesh, SkeletonDefinition skeleton, SkinBinding binding, IEnumerable<BonePose> poses)
        {
            Checks.Require(mesh != null && skeleton != null && binding != null && poses != null, "INVALID_SKIN", "Mesh, skeleton, binding and poses are required.");
            Checks.Require(mesh.TopologyHash == binding.MeshTopologyHash, "SKIN_TOPOLOGY_CHANGED", "Skin binding belongs to another mesh topology.");
            Checks.Require(binding.SkeletonHash == skeleton.ContentHash, "SKIN_SKELETON_CHANGED", "Skin binding belongs to another skeleton.");
            var byId = new Dictionary<string, BonePose>(StringComparer.Ordinal);
            foreach (var pose in poses) Checks.Require(pose != null && byId.TryAdd(pose.BoneId, pose), "DUPLICATE_POSE", "Pose must contain each bone identity once.");
            foreach (var bone in skeleton.Bones) Checks.Require(byId.ContainsKey(bone.BoneId), "POSE_BONE_MISSING", "Pose is missing a skeleton bone.");
            Checks.Require(byId.Count == skeleton.Bones.Count, "POSE_BONE_UNKNOWN", "Pose contains a bone outside the skeleton.");

            var positions = mesh.Positions.ToArray();
            for (int vertex = 0; vertex < positions.Length; vertex++)
            {
                var influences = binding.Weights[vertex]; var result = new Vec3();
                foreach (var influence in influences)
                {
                    var bone = skeleton.ById[influence.BoneId]; var pose = byId[influence.BoneId].Transform;
                    var boneLocal = positions[vertex] - bone.Head;
                    result += pose.TransformPoint(boneLocal) * influence.Weight;
                }
                Checks.Finite(result); positions[vertex] = result;
            }
            return mesh.WithPositions(positions);
        }
    }
}
