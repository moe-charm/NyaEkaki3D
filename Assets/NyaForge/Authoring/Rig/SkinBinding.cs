using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring.Rig
{
    public sealed class VertexWeight
    {
        public int VertexIndex { get; }
        public string BoneId { get; }
        public float Weight { get; }
        internal VertexWeight(int vertexIndex, string boneId, float weight) { VertexIndex = vertexIndex; BoneId = boneId; Weight = weight; }
    }

    /// <summary>Rest-mesh binding with deterministic, normalized influences. Pose evaluation is a later module.</summary>
    public sealed class SkinBinding
    {
        public const int MaxInfluencesPerVertex = 32;
        public string MeshTopologyHash { get; }
        public string SkeletonHash { get; }
        public string ContentHash { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<VertexWeight>> Weights { get; }

        private SkinBinding(string meshTopologyHash, string skeletonHash, IDictionary<int, IReadOnlyList<VertexWeight>> weights)
        {
            Checks.HashText(meshTopologyHash); Checks.HashText(skeletonHash);
            MeshTopologyHash = meshTopologyHash; SkeletonHash = skeletonHash;
            Weights = new ReadOnlyDictionary<int, IReadOnlyList<VertexWeight>>(new Dictionary<int, IReadOnlyList<VertexWeight>>(weights));
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write("skin-binding.v1"); writer.Write(MeshTopologyHash); writer.Write(SkeletonHash); writer.Write(Weights.Count);
                foreach (var pair in Weights.OrderBy(item => item.Key))
                {
                    writer.Write(pair.Key); writer.Write(pair.Value.Count);
                    foreach (var weight in pair.Value) { writer.Write(weight.BoneId); writer.Write(weight.Weight); }
                }
                ContentHash = Checks.Hash(stream.ToArray());
            }
        }

        public static SkinBinding Create(MeshData mesh, SkeletonDefinition skeleton, IEnumerable<VertexWeightInput> raw)
        {
            Checks.Require(mesh != null && skeleton != null && raw != null, "INVALID_SKIN", "Mesh, skeleton and weights are required.");
            var grouped = new Dictionary<int, List<VertexWeightInput>>();
            foreach (var item in raw)
            {
                Checks.Require(item != null, "INVALID_SKIN", "Null vertex weight.");
                Checks.Require(item.VertexIndex >= 0 && item.VertexIndex < mesh.VertexCount, "INVALID_VERTEX", "Weight vertex is outside the mesh domain.");
                Checks.Require(skeleton.ById.ContainsKey(item.BoneId), "BONE_NOT_FOUND", "Weight references an unknown bone.");
                Checks.Finite(item.Weight); Checks.Require(item.Weight > 0 && item.Weight <= 1, "INVALID_WEIGHT", "Weight must be greater than 0 and at most 1.");
                if (!grouped.TryGetValue(item.VertexIndex, out var list)) grouped[item.VertexIndex] = list = new List<VertexWeightInput>();
                Checks.Require(list.All(existing => existing.BoneId != item.BoneId), "DUPLICATE_WEIGHT", "A vertex cannot list the same bone twice.");
                Checks.Require(list.Count < MaxInfluencesPerVertex, "INFLUENCE_LIMIT", "A vertex may use at most 32 bones.");
                list.Add(item);
            }
            Checks.Require(grouped.Count == mesh.VertexCount, "UNWEIGHTED_VERTEX", "Every mesh vertex needs at least one positive weight.");
            var normalized = new Dictionary<int, IReadOnlyList<VertexWeight>>();
            foreach (var pair in grouped)
            {
                float total = pair.Value.Sum(v => v.Weight);
                Checks.Require(total > 0 && float.IsFinite(total), "INVALID_WEIGHT", "Vertex weight sum must be finite and positive.");
                normalized[pair.Key] = Array.AsReadOnly(pair.Value.OrderByDescending(v => v.Weight).ThenBy(v => v.BoneId, StringComparer.Ordinal)
                    .Select(v => new VertexWeight(v.VertexIndex, v.BoneId, v.Weight / total)).ToArray());
            }
            return new SkinBinding(mesh.TopologyHash, skeleton.ContentHash, normalized);
        }

        internal static SkinBinding FromSerialized(string meshTopologyHash, string skeletonHash, IEnumerable<VertexWeightInput> raw)
        {
            Checks.HashText(meshTopologyHash); Checks.HashText(skeletonHash); Checks.Require(raw != null, "INVALID_SKIN", "Serialized weights are required.");
            var grouped = new Dictionary<int, List<VertexWeightInput>>();
            foreach (var item in raw)
            {
                Checks.Require(item != null && item.VertexIndex >= 0, "INVALID_SKIN", "Invalid serialized vertex weight.");
                Checks.Require(item.BoneId != null, "INVALID_SKIN", "Serialized weight bone is missing.");
                Checks.Finite(item.Weight); Checks.Require(item.Weight > 0 && item.Weight <= 1, "INVALID_WEIGHT", "Weight must be greater than 0 and at most 1.");
                if (!grouped.TryGetValue(item.VertexIndex, out var list)) grouped[item.VertexIndex] = list = new List<VertexWeightInput>();
                Checks.Require(list.Count < MaxInfluencesPerVertex && list.All(existing => existing.BoneId != item.BoneId), "INVALID_SKIN", "Serialized influences are not canonical.");
                list.Add(item);
            }
            var normalized = new Dictionary<int, IReadOnlyList<VertexWeight>>();
            foreach (var pair in grouped)
            {
                float total = pair.Value.Sum(v => v.Weight);
                Checks.Require(total > 0 && float.IsFinite(total), "INVALID_WEIGHT", "Serialized vertex weights must be finite and positive.");
                // Serialized bindings already contain canonical, normalized float32 values.
                // Re-normalizing them here changes the low bits for large/multi-influence
                // bindings and makes a save/open cycle produce a different project hash.
                Checks.Require(Math.Abs(total - 1f) <= 1e-4f, "INVALID_WEIGHT", "Serialized vertex weights must sum to one.");
                var ordered = pair.Value.OrderByDescending(v => v.Weight).ThenBy(v => v.BoneId, StringComparer.Ordinal).ToArray();
                Checks.Require(pair.Value.Select((v, i) => v.BoneId == ordered[i].BoneId && v.Weight == ordered[i].Weight).All(v => v), "INVALID_SKIN", "Serialized influences are not canonical.");
                normalized[pair.Key] = Array.AsReadOnly(pair.Value
                    .Select(v => new VertexWeight(v.VertexIndex, v.BoneId, v.Weight)).ToArray());
            }
            return new SkinBinding(meshTopologyHash, skeletonHash, normalized);
        }

        public SkinBinding ValidateFor(MeshData mesh, SkeletonDefinition skeleton)
        {
            Checks.Require(mesh != null && skeleton != null, "INVALID_SKIN", "Mesh and skeleton are required.");
            Checks.Require(MeshTopologyHash == mesh.TopologyHash, "SKIN_TOPOLOGY_CHANGED", "Binding belongs to another mesh topology.");
            Checks.Require(SkeletonHash == skeleton.ContentHash, "SKIN_SKELETON_CHANGED", "Binding belongs to another skeleton.");
            foreach (var pair in Weights)
            {
                Checks.Require(pair.Key >= 0 && pair.Key < mesh.VertexCount, "INVALID_VERTEX", "Weight vertex is outside the mesh domain.");
                Checks.Require(pair.Value != null && pair.Value.Count > 0 && pair.Value.Count <= MaxInfluencesPerVertex, "UNWEIGHTED_VERTEX", "Every mesh vertex needs at least one positive weight.");
                foreach (var weight in pair.Value)
                {
                    Checks.Require(weight != null && weight.VertexIndex == pair.Key, "INVALID_SKIN", "Serialized weight vertex does not match its record.");
                    Checks.Require(skeleton.ById.ContainsKey(weight.BoneId), "BONE_NOT_FOUND", "Weight references an unknown bone.");
                }
            }
            var raw = Weights.OrderBy(p => p.Key).SelectMany(pair => pair.Value.Select(v => new VertexWeightInput(pair.Key, v.BoneId, v.Weight)));
            // ValidateFor must preserve the serialized float32 values. Calling Create
            // would normalize an already-normalized binding a second time.
            var validated = FromSerialized(MeshTopologyHash, SkeletonHash, raw);
            Checks.Require(validated.Weights.Count == mesh.VertexCount, "UNWEIGHTED_VERTEX", "Every mesh vertex needs at least one positive weight.");
            return validated;
        }

        /// <summary>Rebuilds this binding against a skeleton containing the same weighted BoneIds.</summary>
        public SkinBinding RebindToSkeleton(MeshData mesh, SkeletonDefinition skeleton)
        {
            if (mesh == null) throw new ArgumentNullException("mesh");
            if (skeleton == null) throw new ArgumentNullException("skeleton");
            Checks.Require(MeshTopologyHash == mesh.TopologyHash, "SKIN_TOPOLOGY_CHANGED", "Binding belongs to another mesh topology.");
            foreach (var value in Weights.Values.SelectMany(items => items))
                Checks.Require(skeleton.ById.ContainsKey(value.BoneId), "BONE_NOT_FOUND", "Weight references an unknown bone.");
            return Create(mesh, skeleton, Weights.OrderBy(pair => pair.Key)
                .SelectMany(pair => pair.Value.Select(value => new VertexWeightInput(pair.Key, value.BoneId, value.Weight))));
        }

        public sealed class VertexWeightInput
        {
            public int VertexIndex { get; }
            public string BoneId { get; }
            public float Weight { get; }
            public VertexWeightInput(int vertexIndex, string boneId, float weight)
            { Checks.Id(boneId); VertexIndex = vertexIndex; BoneId = boneId; Weight = weight; }
        }
    }
}
