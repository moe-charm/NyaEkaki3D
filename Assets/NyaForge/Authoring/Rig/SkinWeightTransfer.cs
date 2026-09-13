using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Geometry;

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

        /// <summary>Transfers weights by interpolating the nearest triangle of an avatar rest mesh.</summary>
        public static SkinBinding BySurfaceProjection(
            MeshData clothingMesh, RestTransform clothingTransform,
            MeshData avatarMesh, RestTransform avatarTransform,
            SkinBinding avatarBinding, SkeletonDefinition skeleton,
            int maxInfluences = 4)
            => BySurfaceProjection(clothingMesh, clothingTransform, avatarMesh,
                avatarTransform, avatarBinding, skeleton, maxInfluences, null, null);

        /// <summary>
        /// Transfers weights from an explicitly selected avatar surface region.
        /// Triangle indices are flattened in MeshData.Submeshes order. A
        /// positive maxDistance rejects clothing vertices that are too far from
        /// that region instead of silently assigning a distant bone influence.
        /// </summary>
        public static SkinBinding BySurfaceProjection(
            MeshData clothingMesh, RestTransform clothingTransform,
            MeshData avatarMesh, RestTransform avatarTransform,
            SkinBinding avatarBinding, SkeletonDefinition skeleton,
            int maxInfluences, float? maxDistance, IEnumerable<int> avatarTriangleIndices)
        {
            Checks.Require(clothingMesh != null && avatarMesh != null && avatarBinding != null && skeleton != null,
                "INVALID_SKIN", "Clothing mesh, avatar mesh, binding and skeleton are required.");
            clothingTransform.Validate(); avatarTransform.Validate();
            Checks.Require(maxInfluences >= 1 && maxInfluences <= SkinBinding.MaxInfluencesPerVertex,
                "INFLUENCE_LIMIT", "Weight transfer influence count is invalid.");
            if (maxDistance.HasValue)
            {
                Checks.Finite(maxDistance.Value);
                Checks.Require(maxDistance.Value > 0f && maxDistance.Value <= 10f,
                    "INVALID_WEIGHT", "Weight transfer distance must be between zero and 10 metres.");
            }
            avatarBinding.ValidateFor(avatarMesh, skeleton);
            var projection = new MeshSurfaceProjection(avatarMesh, avatarTransform, avatarTriangleIndices);
            var raw = new List<SkinBinding.VertexWeightInput>(clothingMesh.VertexCount * maxInfluences);
            for (int vertex = 0; vertex < clothingMesh.VertexCount; vertex++)
            {
                Vec3 avatarPoint = clothingTransform.ToAvatarPoint(clothingMesh.Positions[vertex]);
                MeshSurfaceHit hit = projection.FindClosest(avatarPoint);
                if (maxDistance.HasValue)
                    Checks.Require(hit.DistanceSquared <= (double)maxDistance.Value * maxDistance.Value,
                        "WEIGHT_TRANSFER_DISTANCE", "A clothing vertex is farther from the selected avatar surface than the configured limit.");
                var accumulated = new Dictionary<string, float>(StringComparer.Ordinal);
                Accumulate(accumulated, avatarBinding.Weights[hit.A], (float)hit.U);
                Accumulate(accumulated, avatarBinding.Weights[hit.B], (float)hit.V);
                Accumulate(accumulated, avatarBinding.Weights[hit.C], (float)hit.W);
                var selected = accumulated.Where(pair => pair.Value > 0f)
                    .OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key, StringComparer.Ordinal)
                    .Take(maxInfluences).ToArray();
                Checks.Require(selected.Length > 0, "UNWEIGHTED_VERTEX", "Surface projection produced no positive weight.");
                foreach (var pair in selected) raw.Add(new SkinBinding.VertexWeightInput(vertex, pair.Key, pair.Value));
            }
            return SkinBinding.Create(clothingMesh, skeleton, raw);
        }

        static void Accumulate(Dictionary<string, float> destination, IReadOnlyList<VertexWeight> weights, float factor)
        {
            Checks.Finite(factor);
            if (factor <= 0f) return;
            foreach (var weight in weights)
            {
                float contribution = weight.Weight * factor;
                Checks.Finite(contribution);
                if (contribution <= 0f) continue;
                float existing;
                destination.TryGetValue(weight.BoneId, out existing);
                destination[weight.BoneId] = existing + contribution;
            }
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
