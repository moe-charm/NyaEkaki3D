using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>One resolved source head/tail pair; source settings retain their original units.</summary>
    public sealed class VrmSpringPairBinding
    {
        public string HeadBoneId { get; }
        public string TailBoneId { get; }
        public Vec3 SourceHeadOrigin { get; }
        public Vec3 SourceTailOrigin { get; }
        public VrmSpringJoint Settings { get; }

        internal VrmSpringPairBinding(string head, string tail, Vec3 headOrigin, Vec3 tailOrigin, VrmSpringJoint settings)
        {
            HeadBoneId = head; TailBoneId = tail; SourceHeadOrigin = headOrigin; SourceTailOrigin = tailOrigin; Settings = settings;
        }
    }

    public sealed class VrmSpringChainBinding
    {
        public string Name { get; }
        public string CenterBoneId { get; }
        public IReadOnlyList<VrmSpringPairBinding> Pairs { get; }
        public IReadOnlyList<int> ColliderGroupIndices { get; }

        internal VrmSpringChainBinding(string name, string center, IEnumerable<VrmSpringPairBinding> pairs, IEnumerable<int> colliders)
        {
            Name = name; CenterBoneId = center; Pairs = System.Array.AsReadOnly(pairs.ToArray());
            ColliderGroupIndices = System.Array.AsReadOnly(colliders.ToArray());
        }
    }
}
