using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NyaForge.UnityBridge
{
    /// <summary>One explicit stable BoneId to scene Transform assignment for a clothing package.</summary>
    [Serializable]
    public sealed class NyaForgeSkinnedClothingBoneBinding
    {
        [SerializeField] string boneId = "";
        [SerializeField] Transform transform;

        public string BoneId { get { return boneId; } }
        public Transform Transform { get { return transform; } }

        internal NyaForgeSkinnedClothingBoneBinding(string id, Transform value)
        {
            boneId = id ?? "";
            transform = value;
        }
    }

    /// <summary>
    /// Persistent ownership marker for one clothing package applied to an avatar.
    /// An avatar may have one component per package; the generated object may be
    /// replaced only when its stable ObjectId matches.
    /// </summary>
    public sealed class NyaForgeSkinnedClothingBinding : MonoBehaviour
    {
        [SerializeField] string manifestPath = "";
        [SerializeField] string objectId = "";
        [SerializeField] string graphId = "";
        [SerializeField] string stateHash = "";
        [SerializeField] string graphHash = "";
        [SerializeField] string glbHash = "";
        [SerializeField] string skeletonHash = "";
        [SerializeField] string bindingHash = "";
        [SerializeField] GameObject generatedObject;
        [SerializeField] NyaForgeSkinnedClothingBoneBinding[] bones = Array.Empty<NyaForgeSkinnedClothingBoneBinding>();

        public string ManifestPath { get { return manifestPath; } }
        public string ObjectId { get { return objectId; } }
        public string GraphId { get { return graphId; } }
        public string StateHash { get { return stateHash; } }
        public string GraphHash { get { return graphHash; } }
        public string GlbHash { get { return glbHash; } }
        public string SkeletonHash { get { return skeletonHash; } }
        public string BindingHash { get { return bindingHash; } }
        public GameObject GeneratedObject { get { return generatedObject; } }
        public IReadOnlyList<NyaForgeSkinnedClothingBoneBinding> Bones { get { return bones ?? Array.Empty<NyaForgeSkinnedClothingBoneBinding>(); } }

        public bool Matches(string expectedObjectId, string expectedStateHash, string expectedSkeletonHash, string expectedBindingHash)
        {
            return objectId == (expectedObjectId ?? "") && stateHash == (expectedStateHash ?? "")
                && skeletonHash == (expectedSkeletonHash ?? "") && bindingHash == (expectedBindingHash ?? "");
        }

        /// <summary>Allows a newer package revision to replace only the same authored object.</summary>
        public bool MatchesObject(string expectedObjectId)
        {
            return objectId == (expectedObjectId ?? "") && generatedObject != null;
        }

        /// <summary>Matches the authored package identity even when its generated
        /// scene object has not been applied yet or was removed.</summary>
        public bool MatchesAssignment(string expectedObjectId)
        {
            return objectId == (expectedObjectId ?? "");
        }

        /// <summary>Replaces the complete explicit mapping after the editor has validated it.</summary>
        public void Capture(string path, string packageObjectId, string packageGraphId, string packageStateHash,
            string packageGraphHash, string packageGlbHash, string packageSkeletonHash, string packageBindingHash,
            IEnumerable<KeyValuePair<string, Transform>> boneValues, GameObject generated)
        {
            if (boneValues == null) throw new ArgumentNullException("boneValues");
            var ordered = boneValues.OrderBy(pair => pair.Key, StringComparer.Ordinal).ToArray();
            if (ordered.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || pair.Value == null))
                throw new ArgumentException("Every clothing bone binding needs a stable ID and Transform.", "boneValues");
            if (ordered.Select(pair => pair.Key).Distinct(StringComparer.Ordinal).Count() != ordered.Length)
                throw new ArgumentException("Clothing bone bindings must have unique stable IDs.", "boneValues");
            manifestPath = path ?? "";
            objectId = packageObjectId ?? "";
            graphId = packageGraphId ?? "";
            stateHash = packageStateHash ?? "";
            graphHash = packageGraphHash ?? "";
            glbHash = packageGlbHash ?? "";
            skeletonHash = packageSkeletonHash ?? "";
            bindingHash = packageBindingHash ?? "";
            generatedObject = generated;
            bones = ordered.Select(pair => new NyaForgeSkinnedClothingBoneBinding(pair.Key, pair.Value)).ToArray();
        }

        /// <summary>
        /// Clears only the generated scene-object reference while retaining the
        /// package identity and explicit BoneId mapping. The editor calls this
        /// after recording the binding in Undo before destroying the managed
        /// object, so an Undo can restore the previous association.
        /// </summary>
        public void ClearGeneratedObject()
        {
            generatedObject = null;
        }

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
