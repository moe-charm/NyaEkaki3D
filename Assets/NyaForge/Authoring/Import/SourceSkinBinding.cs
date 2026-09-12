using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    public sealed class SourceSkinWeight
    {
        public int VertexIndex { get; }
        public int JointSlot { get; }
        public float Weight { get; }
        public SourceSkinWeight(int vertexIndex, int jointSlot, float weight)
        {
            Checks.Require(vertexIndex >= 0 && jointSlot >= 0, "INVALID_SKIN", "Source skin weight indices must be nonnegative.");
            Checks.Finite(weight); Checks.Require(weight > 0 && weight <= 1, "INVALID_WEIGHT", "Source skin weight must be positive and at most one.");
            VertexIndex = vertexIndex; JointSlot = jointSlot; Weight = weight;
        }
    }

    /// <summary>Source-indexed sparse weights. It retains slot identity before mapping to authored BoneId.</summary>
    public sealed class SourceSkinBinding
    {
        public const int MaxInfluencesPerVertex = 32;
        public string MeshTopologyHash { get; }
        public string SourceHash { get; }
        public int VertexCount { get; }
        public IReadOnlyDictionary<int, IReadOnlyList<SourceSkinWeight>> Weights { get; }

        SourceSkinBinding(string sourceHash, string topologyHash, IDictionary<int, IReadOnlyList<SourceSkinWeight>> weights)
        {
            Checks.HashText(sourceHash); Checks.HashText(topologyHash);
            SourceHash = sourceHash; MeshTopologyHash = topologyHash;
            VertexCount = weights.Count;
            Weights = new ReadOnlyDictionary<int, IReadOnlyList<SourceSkinWeight>>(new Dictionary<int, IReadOnlyList<SourceSkinWeight>>(weights));
        }

        public static SourceSkinBinding Create(MeshData mesh, SourceSkin skin, IEnumerable<SourceSkinWeight> raw)
        {
            Checks.Require(mesh != null && skin != null && raw != null, "INVALID_SKIN", "Mesh, source skin and weights are required.");
            var grouped = new Dictionary<int, List<SourceSkinWeight>>();
            foreach (var item in raw)
            {
                Checks.Require(item != null && item.VertexIndex < mesh.VertexCount && item.JointSlot < skin.Joints.Count,
                    "INVALID_SKIN", "Source skin weight is outside its mesh or joint domain.");
                if (!grouped.TryGetValue(item.VertexIndex, out var list)) grouped[item.VertexIndex] = list = new List<SourceSkinWeight>();
                Checks.Require(list.All(existing => existing.JointSlot != item.JointSlot), "DUPLICATE_WEIGHT", "A source vertex cannot list one joint slot twice.");
                Checks.Require(list.Count < MaxInfluencesPerVertex, "INFLUENCE_LIMIT", "A source vertex cannot use more than 32 distinct joint slots.");
                list.Add(item);
            }
            Checks.Require(grouped.Count == mesh.VertexCount, "UNWEIGHTED_VERTEX", "Every mesh vertex needs a source skin weight.");
            var normalized = new Dictionary<int, IReadOnlyList<SourceSkinWeight>>();
            foreach (var pair in grouped)
            {
                float total = pair.Value.Sum(item => item.Weight);
                Checks.Require(total > 0 && float.IsFinite(total), "INVALID_WEIGHT", "Source skin weights must sum to a finite positive value.");
                normalized[pair.Key] = Array.AsReadOnly(pair.Value.OrderByDescending(item => item.Weight).ThenBy(item => item.JointSlot)
                    .Select(item => new SourceSkinWeight(item.VertexIndex, item.JointSlot, item.Weight / total)).ToArray());
            }
            return new SourceSkinBinding(skin.Nodes.SourceHash, mesh.TopologyHash, normalized);
        }

        public SourceSkinBinding ValidateFor(MeshData mesh, SourceSkin skin)
        {
            Checks.Require(mesh != null && skin != null, "INVALID_SKIN", "Mesh and source skin are required.");
            Checks.Require(MeshTopologyHash == mesh.TopologyHash && SourceHash == skin.Nodes.SourceHash, "SKIN_SOURCE_CHANGED", "Source skin binding identity changed.");
            return Create(mesh, skin, Weights.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value));
        }

        internal static SourceSkinBinding FromSerialized(string sourceHash, string topologyHash, int vertexCount, SourceSkin skin, IEnumerable<SourceSkinWeight> raw)
        {
            Checks.Require(skin != null && skin.Nodes.SourceHash == sourceHash, "SKIN_SOURCE_CHANGED", "Serialized source skin binding belongs to another source.");
            Checks.Require(vertexCount > 0 && vertexCount <= AuthoringLimits.MaxVertices && raw != null, "INVALID_SKIN", "Serialized source binding domain is invalid.");
            var values = raw.ToArray();
            Checks.Require(values.All(weight => weight.VertexIndex < vertexCount), "INVALID_VERTEX", "Serialized source weight is outside the declared mesh domain.");
            var grouped = new Dictionary<int, List<SourceSkinWeight>>();
            foreach (var item in values)
            {
                if (!grouped.TryGetValue(item.VertexIndex, out var list)) grouped[item.VertexIndex] = list = new List<SourceSkinWeight>();
                Checks.Require(list.All(existing => existing.JointSlot != item.JointSlot), "DUPLICATE_WEIGHT", "Serialized source weight repeats a joint slot.");
                Checks.Require(list.Count < MaxInfluencesPerVertex, "INFLUENCE_LIMIT", "Serialized source weight count exceeds capacity."); list.Add(item);
            }
            Checks.Require(grouped.Count == vertexCount, "UNWEIGHTED_VERTEX", "Serialized source binding does not cover every vertex.");
            var normalized = new Dictionary<int, IReadOnlyList<SourceSkinWeight>>();
            foreach (var pair in grouped)
            {
                float total = pair.Value.Sum(item => item.Weight);
                Checks.Require(total > 0 && float.IsFinite(total), "INVALID_WEIGHT", "Serialized source weights must sum to a finite positive value.");
                normalized[pair.Key] = Array.AsReadOnly(pair.Value.OrderByDescending(item => item.Weight).ThenBy(item => item.JointSlot)
                    .Select(item => new SourceSkinWeight(item.VertexIndex, item.JointSlot, item.Weight / total)).ToArray());
            }
            return new SourceSkinBinding(sourceHash, topologyHash, normalized);
        }
    }
}
