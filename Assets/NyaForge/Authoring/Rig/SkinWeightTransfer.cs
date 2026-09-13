using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Deterministic initial clothing weights from distance to rest bone segments.</summary>
    public static class SkinWeightTransfer
    {
        public static SkinBinding ByBoneProximity(MeshData mesh, SkeletonDefinition skeleton, float falloff = 0.05f, int maxInfluences = 4)
        {
            Checks.Require(mesh != null && skeleton != null, "INVALID_SKIN", "Mesh and skeleton are required.");
            Checks.Finite(falloff); Checks.Require(falloff > 0f && falloff <= 10f, "INVALID_WEIGHT", "Weight transfer falloff must be positive.");
            Checks.Require(maxInfluences >= 1 && maxInfluences <= SkinBinding.MaxInfluencesPerVertex, "INFLUENCE_LIMIT", "Weight transfer influence count is invalid.");
            var bones = skeleton.Bones.ToArray();
            var raw = new List<SkinBinding.VertexWeightInput>(mesh.VertexCount * maxInfluences);
            for (int vertex = 0; vertex < mesh.VertexCount; vertex++)
            {
                var nearest = bones.Select(bone => new DistanceWeight(bone.BoneId, DistanceSquared(mesh.Positions[vertex], bone), bone))
                    .OrderBy(item => item.Distance).ThenBy(item => item.Bone.BoneId, StringComparer.Ordinal).Take(maxInfluences).ToArray();
                float total = nearest.Sum(item => 1f / (item.Distance + falloff * falloff));
                foreach (var item in nearest)
                {
                    float weight = (1f / (item.Distance + falloff * falloff)) / total;
                    raw.Add(new SkinBinding.VertexWeightInput(vertex, item.BoneId, weight));
                }
            }
            return SkinBinding.Create(mesh, skeleton, raw);
        }

        sealed class DistanceWeight
        {
            public readonly string BoneId; public readonly float Distance; public readonly BoneDefinition Bone;
            public DistanceWeight(string boneId, float distance, BoneDefinition bone) { BoneId = boneId; Distance = distance; Bone = bone; }
        }

        static float DistanceSquared(Vec3 point, BoneDefinition bone)
        {
            var segment = bone.Tail - bone.Head;
            float length = segment.X * segment.X + segment.Y * segment.Y + segment.Z * segment.Z;
            float t = length <= 1e-12f ? 0f : ((point.X - bone.Head.X) * segment.X + (point.Y - bone.Head.Y) * segment.Y + (point.Z - bone.Head.Z) * segment.Z) / length;
            t = Math.Max(0f, Math.Min(1f, t));
            var closest = bone.Head + segment * t;
            var delta = point - closest;
            return delta.X * delta.X + delta.Y * delta.Y + delta.Z * delta.Z;
        }
    }
}
