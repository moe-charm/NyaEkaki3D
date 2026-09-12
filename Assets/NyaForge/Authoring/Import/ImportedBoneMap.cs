using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Source-pinned glTF node identities for an imported skeleton; independent of names and joint slots.</summary>
    public sealed class ImportedBoneMap
    {
        public string SourceHash { get; }
        public string SkeletonHash { get; }
        public IReadOnlyDictionary<int, string> ByNode { get; }

        internal ImportedBoneMap(string sourceHash, SkeletonDefinition skeleton, IDictionary<int, string> mapping)
        {
            Checks.HashText(sourceHash);
            Checks.Require(skeleton != null && mapping != null, "INVALID_IMPORT", "Imported bone mapping is required.");
            Checks.Require(mapping.Count == skeleton.Bones.Count, "INVALID_IMPORT", "Imported bone mapping must cover the skeleton.");
            var copy = new Dictionary<int, string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var pair in mapping)
            {
                Checks.Require(pair.Key >= 0 && pair.Value != null && skeleton.ById.ContainsKey(pair.Value) && seen.Add(pair.Value), "INVALID_IMPORT", "Imported bone mapping is invalid or repeated.");
                copy.Add(pair.Key, pair.Value);
            }
            SourceHash = sourceHash; SkeletonHash = skeleton.ContentHash;
            ByNode = new ReadOnlyDictionary<int, string>(copy);
        }

        public void ValidateFor(string sourceHash, SkeletonDefinition skeleton)
        {
            Checks.Require(sourceHash == SourceHash, "IMPORT_SOURCE_CHANGED", "Bone mapping belongs to another source.");
            Checks.Require(skeleton != null && skeleton.ContentHash == SkeletonHash, "IMPORT_SKELETON_CHANGED", "Bone mapping belongs to another skeleton revision.");
        }

        public string Resolve(int nodeIndex)
        {
            Checks.Require(ByNode.TryGetValue(nodeIndex, out var boneId), "IMPORT_BONE_UNMAPPED", "Source node is not an imported skin joint: " + nodeIndex);
            return boneId;
        }
    }
}
