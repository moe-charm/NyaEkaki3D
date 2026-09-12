using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Persisted source identity and node mapping, pinned to a graph's skeleton node and revision.</summary>
    public sealed class ImportedRigSession
    {
        public string SourceHash { get; }
        public string SkeletonHash { get; }
        public string GraphId { get; }
        public string SkeletonNodeId { get; }
        public IReadOnlyDictionary<int, string> NodeToBone { get; }
        public IReadOnlyDictionary<string, int> HumanoidNodes { get; }

        internal ImportedRigSession(string sourceHash, string skeletonHash, string graphId, string skeletonNodeId,
            IDictionary<int, string> nodes, IDictionary<string, int> humanoid)
        {
            Checks.HashText(sourceHash); Checks.HashText(skeletonHash); Checks.Id(graphId); Checks.Id(skeletonNodeId);
            Checks.Require(nodes != null && nodes.Count > 0 && nodes.Count <= SkeletonDefinition.MaxBones && humanoid != null && humanoid.Count <= SkeletonDefinition.MaxBones, "INVALID_IMPORT", "Imported rig mapping exceeds capacity.");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in nodes)
            {
                Checks.Id(pair.Value);
                Checks.Require(pair.Key >= 0 && pair.Key < AuthoringLimits.MaxVertices && seen.Add(pair.Value), "INVALID_IMPORT", "Imported rig node mapping is invalid or repeated.");
            }
            foreach (var pair in humanoid)
                Checks.Require(!string.IsNullOrWhiteSpace(pair.Key) && pair.Key.Length <= 128 && pair.Value >= 0 && pair.Value < AuthoringLimits.MaxVertices, "INVALID_IMPORT", "Imported humanoid mapping is invalid.");
            SourceHash = sourceHash; SkeletonHash = skeletonHash; GraphId = graphId; SkeletonNodeId = skeletonNodeId;
            NodeToBone = new ReadOnlyDictionary<int, string>(new Dictionary<int, string>(nodes));
            HumanoidNodes = new ReadOnlyDictionary<string, int>(new Dictionary<string, int>(humanoid, StringComparer.Ordinal));
        }

        public static ImportedRigSession Create(ImportedSkinnedMeshSource source, VrmMetadata metadata, string graphId, string skeletonNodeId)
        {
            Checks.Require(source != null, "INVALID_IMPORT", "Imported skin is required.");
            Checks.Require(metadata == null || metadata.SourceHash == source.SourceHash, "IMPORT_SOURCE_CHANGED", "Rig metadata belongs to another source.");
            return new ImportedRigSession(source.SourceHash, source.Skeleton.ContentHash, graphId, skeletonNodeId,
                new Dictionary<int, string>(source.BoneMap.ByNode), metadata == null ? new Dictionary<string, int>() : new Dictionary<string, int>(metadata.HumanoidNodes));
        }

        public ImportedBoneMap Resolve(AuthoringGraph graph)
        {
            Checks.Require(graph != null && graph.GraphId == GraphId, "IMPORT_GRAPH_CHANGED", "Imported rig belongs to another graph.");
            Checks.Require(graph.Nodes.TryGetValue(SkeletonNodeId, out var node) && node.Skeleton != null, "IMPORT_SKELETON_CHANGED", "Imported skeleton node no longer exists.");
            Checks.Require(node.Skeleton.ContentHash == SkeletonHash, "IMPORT_SKELETON_CHANGED", "Imported skeleton has changed; mapping needs review.");
            return new ImportedBoneMap(SourceHash, node.Skeleton, new Dictionary<int, string>(NodeToBone));
        }

        public void ValidateSource(string sourceHash)
        {
            Checks.Require(sourceHash == SourceHash, "IMPORT_SOURCE_CHANGED", "Imported rig metadata belongs to another source.");
        }

        public IReadOnlyDictionary<string, string> ResolveHumanoid(AuthoringGraph graph)
        {
            var map = Resolve(graph); var result = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in HumanoidNodes) result.Add(pair.Key, map.Resolve(pair.Value));
            return new ReadOnlyDictionary<string, string>(result);
        }
    }
}
