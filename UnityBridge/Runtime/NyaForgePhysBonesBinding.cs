using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NyaForge.UnityBridge
{
    /// <summary>One explicit stable BoneId to scene Transform assignment.</summary>
    [Serializable]
    public sealed class NyaForgePhysBonesBoneBinding
    {
        [SerializeField] string boneId = "";
        [SerializeField] Transform transform;

        public string BoneId { get { return boneId; } }
        public Transform Transform { get { return transform; } }

        internal NyaForgePhysBonesBoneBinding(string id, Transform value)
        {
            boneId = id ?? "";
            transform = value;
        }
    }

    /// <summary>One explicit collider group to scene Component assignment.</summary>
    [Serializable]
    public sealed class NyaForgePhysBonesColliderGroupBinding
    {
        [SerializeField] int groupIndex;
        [SerializeField] Component[] colliders = Array.Empty<Component>();

        public int GroupIndex { get { return groupIndex; } }
        public IReadOnlyList<Component> Colliders { get { return colliders ?? Array.Empty<Component>(); } }

        internal NyaForgePhysBonesColliderGroupBinding(int index, IEnumerable<Component> values)
        {
            groupIndex = index;
            colliders = (values ?? Array.Empty<Component>()).Where(value => value != null).ToArray();
        }
    }

    /// <summary>
    /// Scene/prefab-persistent receiver mapping for a PhysBones target package.
    /// The mapping is valid only for the exact package identity captured with it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NyaForgePhysBonesBinding : MonoBehaviour
    {
        [SerializeField] string manifestPath = "";
        [SerializeField] string manifestHash = "";
        [SerializeField] string targetId = "";
        [SerializeField] string sdkVersion = "";
        [SerializeField] string profileHash = "";
        [SerializeField] string skeletonHash = "";
        [SerializeField] NyaForgePhysBonesBoneBinding[] bones = Array.Empty<NyaForgePhysBonesBoneBinding>();
        [SerializeField] NyaForgePhysBonesColliderGroupBinding[] colliderGroups = Array.Empty<NyaForgePhysBonesColliderGroupBinding>();

        public string ManifestPath { get { return manifestPath; } }
        public string ManifestHash { get { return manifestHash; } }
        public string TargetId { get { return targetId; } }
        public string SdkVersion { get { return sdkVersion; } }
        public string ProfileHash { get { return profileHash; } }
        public string SkeletonHash { get { return skeletonHash; } }
        public IReadOnlyList<NyaForgePhysBonesBoneBinding> Bones { get { return bones ?? Array.Empty<NyaForgePhysBonesBoneBinding>(); } }
        public IReadOnlyList<NyaForgePhysBonesColliderGroupBinding> ColliderGroups { get { return colliderGroups ?? Array.Empty<NyaForgePhysBonesColliderGroupBinding>(); } }

        /// <summary>Checks every identity field; moving the manifest alone does not invalidate a matching package.</summary>
        public bool Matches(string expectedManifestHash, string expectedTargetId, string expectedSdkVersion,
            string expectedProfileHash, string expectedSkeletonHash)
        {
            return manifestHash == (expectedManifestHash ?? "") && targetId == (expectedTargetId ?? "")
                && sdkVersion == (expectedSdkVersion ?? "") && profileHash == (expectedProfileHash ?? "")
                && skeletonHash == (expectedSkeletonHash ?? "");
        }

        /// <summary>Replaces the complete explicit mapping after the editor has validated it.</summary>
        public void Capture(string path, string packageManifestHash, string packageTargetId, string packageSdkVersion,
            string packageProfileHash, string packageSkeletonHash,
            IEnumerable<KeyValuePair<string, Transform>> boneValues,
            IEnumerable<KeyValuePair<int, IEnumerable<Component>>> colliderValues)
        {
            if (boneValues == null) throw new ArgumentNullException("boneValues");
            var orderedBones = boneValues.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            if (orderedBones.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null))
                throw new ArgumentException("Every PhysBones bone binding needs a stable ID and Transform.", "boneValues");
            if (orderedBones.Select(pair => pair.Key).Distinct(StringComparer.Ordinal).Count() != orderedBones.Length)
                throw new ArgumentException("PhysBones bone bindings must have unique stable IDs.", "boneValues");

            var groups = (colliderValues ?? Array.Empty<KeyValuePair<int, IEnumerable<Component>>>());
            var orderedGroups = groups.OrderBy(pair => pair.Key).ToArray();
            if (orderedGroups.Select(pair => pair.Key).Distinct().Count() != orderedGroups.Length)
                throw new ArgumentException("PhysBones collider groups must be unique.", "colliderValues");

            manifestPath = path ?? "";
            manifestHash = packageManifestHash ?? "";
            targetId = packageTargetId ?? "";
            sdkVersion = packageSdkVersion ?? "";
            profileHash = packageProfileHash ?? "";
            skeletonHash = packageSkeletonHash ?? "";
            bones = orderedBones.Select(pair => new NyaForgePhysBonesBoneBinding(pair.Key, pair.Value)).ToArray();
            colliderGroups = orderedGroups.Select(pair => new NyaForgePhysBonesColliderGroupBinding(pair.Key, pair.Value)).ToArray();
        }

        /// <summary>Returns a stable-ID lookup without guessing by names or array position.</summary>
        public bool TryGetBone(string id, out Transform value)
        {
            value = null;
            if (string.IsNullOrEmpty(id)) return false;
            var item = Bones.FirstOrDefault(binding => binding != null && binding.BoneId == id);
            if (item == null) return false;
            value = item.Transform;
            return value != null;
        }
    }
}
