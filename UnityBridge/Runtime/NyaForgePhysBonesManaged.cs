using UnityEngine;

namespace NyaForge.UnityBridge
{
    /// <summary>Persistent ownership marker for a PhysBone component created by NyaForge.</summary>
    public sealed class NyaForgePhysBonesManaged : MonoBehaviour
    {
        [SerializeField] string targetId = "";
        [SerializeField] int chainIndex = -1;
        [SerializeField] string chainName = "";
        [SerializeField] string rootBoneId = "";
        [SerializeField] string profileHash = "";
        [SerializeField] Component managedComponent;

        public string TargetId { get { return targetId; } }
        public int ChainIndex { get { return chainIndex; } }
        public string ChainName { get { return chainName; } }
        public string RootBoneId { get { return rootBoneId; } }
        public string ProfileHash { get { return profileHash; } }
        public Component ManagedComponent { get { return managedComponent; } }

        public bool Matches(string target, int index, string root)
        {
            return targetId == target && chainIndex == index && rootBoneId == root;
        }

        /// <summary>Matches a chain by stable authoring labels, independent of profile array order.</summary>
        public bool MatchesStable(string target, string name, string root)
        {
            return targetId == target && chainName == (name ?? "") && rootBoneId == root;
        }

        /// <summary>Matches the stable root identity when a profile chain was renamed.</summary>
        public bool MatchesStableRoot(string target, string root)
        {
            return targetId == target && rootBoneId == root;
        }

        /// <summary>Called only by the Bridge after the component has been created or updated.</summary>
        public void Bind(Component component, string target, int index, string name, string root, string hash)
        {
            if (component == null) throw new System.ArgumentNullException("component");
            targetId = target ?? "";
            chainIndex = index;
            chainName = name ?? "";
            rootBoneId = root ?? "";
            profileHash = hash ?? "";
            managedComponent = component;
        }
    }
}
