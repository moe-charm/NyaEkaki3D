using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Resolves the imported VRM humanoid inventory to the exact skeleton's stable bone IDs.</summary>
    public sealed class VrmHumanoidBinding
    {
        public string SourceHash { get; }
        public string SkeletonHash { get; }
        public IReadOnlyDictionary<string, string> BoneIds { get; }

        VrmHumanoidBinding(ImportedBoneMap map, Dictionary<string, string> values)
        {
            SourceHash = map.SourceHash; SkeletonHash = map.SkeletonHash;
            BoneIds = new ReadOnlyDictionary<string, string>(values);
        }

        public static VrmHumanoidBinding Create(VrmMetadata metadata, ImportedBoneMap map, SkeletonDefinition skeleton)
        {
            Checks.Require(metadata != null && map != null, "INVALID_VRM", "VRM metadata and imported bone mapping are required.");
            map.ValidateFor(metadata.SourceHash, skeleton);
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var pair in metadata.HumanoidNodes) values.Add(pair.Key, map.Resolve(pair.Value));
            return new VrmHumanoidBinding(map, values);
        }
    }
}
