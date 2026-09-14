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
            var pending = required.ToArray();
            for (int i = 0; i < pending.Length; i++)
            {
                var bone = skeleton.ById[pending[i]];
                if (!string.IsNullOrEmpty(bone.ParentBoneId) && required.Add(bone.ParentBoneId))
                    pending = pending.Concat(new[] { bone.ParentBoneId }).ToArray();
            }
            if (required.Count == skeleton.Bones.Count) return skeleton;
            return new SkeletonDefinition(skeleton.Bones.Where(bone => required.Contains(bone.BoneId)));
        }
    }
}
