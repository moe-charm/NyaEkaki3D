using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Pure, deterministic weight edits shared by GUI and future remote commands.</summary>
    public static class SkinBindingEditing
    {
        public static SkinBinding Rebind(SkinBinding binding, MeshData mesh, SkeletonDefinition skeleton)
        {
            Checks.Require(binding != null && mesh != null && skeleton != null, "INVALID_SKIN", "Binding, mesh and skeleton are required.");
            Checks.Require(binding.MeshTopologyHash == mesh.TopologyHash, "SKIN_TOPOLOGY_CHANGED", "Binding belongs to another mesh topology.");
            var raw = binding.Weights.OrderBy(p => p.Key).SelectMany(pair => pair.Value.Select(v => new SkinBinding.VertexWeightInput(pair.Key, v.BoneId, v.Weight)));
            return SkinBinding.Create(mesh, skeleton, raw);
        }
        public static SkinBinding AssignVertices(SkinBinding binding, MeshData mesh, SkeletonDefinition skeleton, IEnumerable<int> vertices, string boneId, float weight = 1f)
        {
            Checks.Require(binding != null && mesh != null && skeleton != null, "INVALID_SKIN", "Binding, mesh and skeleton are required.");
            Checks.Id(boneId); Checks.Require(skeleton.ById.ContainsKey(boneId), "BONE_NOT_FOUND", "Weight references an unknown bone.");
            Checks.Finite(weight); Checks.Require(weight > 0 && weight <= 1, "INVALID_WEIGHT", "Weight must be greater than 0 and at most 1.");
            var current = binding.ValidateFor(mesh, skeleton);
            var selected = new HashSet<int>(vertices ?? Enumerable.Empty<int>());
            Checks.Require(selected.Count > 0, "SELECTION_EMPTY", "At least one vertex must be selected.");
            foreach (int vertex in selected) Checks.Require(vertex >= 0 && vertex < mesh.VertexCount, "INVALID_VERTEX", "Selected vertex is outside the mesh domain.");
            var raw = new List<SkinBinding.VertexWeightInput>();
            for (int vertex = 0; vertex < mesh.VertexCount; vertex++)
            {
                if (selected.Contains(vertex)) raw.Add(new SkinBinding.VertexWeightInput(vertex, boneId, weight));
                else foreach (var influence in current.Weights[vertex]) raw.Add(new SkinBinding.VertexWeightInput(vertex, influence.BoneId, influence.Weight));
            }
            return SkinBinding.Create(mesh, skeleton, raw);
        }

        public static SkinBinding SetVerticesWeight(SkinBinding binding, MeshData mesh, SkeletonDefinition skeleton, IEnumerable<int> vertices, string boneId, float weight)
        {
            Checks.Require(binding != null && mesh != null && skeleton != null, "INVALID_SKIN", "Binding, mesh and skeleton are required.");
            Checks.Id(boneId); Checks.Require(skeleton.ById.ContainsKey(boneId), "BONE_NOT_FOUND", "Weight references an unknown bone.");
            Checks.Finite(weight); Checks.Require(weight >= 0 && weight <= 1, "INVALID_WEIGHT", "Weight must be between 0 and 1.");
            var current = binding.ValidateFor(mesh, skeleton); var selected = new HashSet<int>(vertices ?? Enumerable.Empty<int>());
            Checks.Require(selected.Count > 0, "SELECTION_EMPTY", "At least one vertex must be selected.");
            foreach (int vertex in selected) Checks.Require(vertex >= 0 && vertex < mesh.VertexCount, "INVALID_VERTEX", "Selected vertex is outside the mesh domain.");
            var raw = new List<SkinBinding.VertexWeightInput>();
            for (int vertex = 0; vertex < mesh.VertexCount; vertex++)
            {
                if (!selected.Contains(vertex)) { foreach (var influence in current.Weights[vertex]) raw.Add(new SkinBinding.VertexWeightInput(vertex, influence.BoneId, influence.Weight)); continue; }
                var others = current.Weights[vertex].Where(influence => influence.BoneId != boneId).ToArray();
                bool hasTarget = current.Weights[vertex].Any(influence => influence.BoneId == boneId);
                if (weight > 0 && !hasTarget) Checks.Require(others.Length < SkinBinding.MaxInfluencesPerVertex, "INFLUENCE_LIMIT", "A vertex may use at most four bones.");
                float otherTotal = others.Sum(influence => influence.Weight);
                if (weight > 0)
                {
                    if (others.Length == 0) Checks.Require(weight == 1, "INVALID_WEIGHT", "A sole influence must remain at weight 1.");
                    raw.Add(new SkinBinding.VertexWeightInput(vertex, boneId, weight));
                }
                if (weight < 1 && others.Length > 0)
                {
                    Checks.Require(otherTotal > 0, "INVALID_WEIGHT", "Remaining influences must have positive weight.");
                    foreach (var influence in others) raw.Add(new SkinBinding.VertexWeightInput(vertex, influence.BoneId, influence.Weight * (1 - weight) / otherTotal));
                }
                else if (weight == 0) Checks.Require(others.Length > 0, "INVALID_WEIGHT", "A vertex must retain one positive influence.");
            }
            return SkinBinding.Create(mesh, skeleton, raw);
        }
    }
}
