using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Builds the smallest valid skeleton that can deform one mesh.</summary>
    public static class SkeletonBindingSubset
    {
        public static SkeletonDefinition ForBinding(SkeletonDefinition skeleton, SkinBinding binding)
        {
            if (skeleton == null) throw new ArgumentNullException("skeleton");
            if (binding == null) throw new ArgumentNullException("binding");
            var required = new HashSet<string>(binding.Weights.Values.SelectMany(values => values).Select(value => value.BoneId), StringComparer.Ordinal);
            Checks.Require(required.Count > 0, "UNWEIGHTED_VERTEX", "A clothing binding must reference at least one bone.");
            var pending = new Queue<string>(required);
            while (pending.Count > 0)
            {
                string boneId = pending.Dequeue();
                BoneDefinition bone;
                Checks.Require(skeleton.ById.TryGetValue(boneId, out bone), "BONE_NOT_FOUND", "Clothing binding references an unknown bone.");
                if (!string.IsNullOrEmpty(bone.ParentBoneId) && required.Add(bone.ParentBoneId))
                    pending.Enqueue(bone.ParentBoneId);
            }
            if (required.Count == skeleton.Bones.Count) return skeleton;
            return new SkeletonDefinition(skeleton.Bones.Where(bone => required.Contains(bone.BoneId)));
        }
    }
}
