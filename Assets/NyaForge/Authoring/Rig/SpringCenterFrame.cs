using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Detached center-to-avatar transforms for every simulated bone at one instant.</summary>
    public sealed class SpringCenterFrame
    {
        public string SkeletonHash { get; }
        public string ChainHash { get; }
        public IReadOnlyDictionary<string, PoseTransform> ByBoneId { get; }

        public SpringCenterFrame(SpringBoneState state, IReadOnlyDictionary<string, PoseTransform> transforms)
        {
            Checks.Require(state != null && transforms != null, "INVALID_SPRING_CENTER", "State and center transforms are required.");
            Checks.Require(transforms.Count == state.CurrentTails.Count, "INVALID_SPRING_CENTER", "Center transforms must cover every simulated bone.");
            var copy = new Dictionary<string, PoseTransform>();
            foreach (var pair in transforms)
            {
                Checks.Require(state.CurrentTails.ContainsKey(pair.Key), "INVALID_SPRING_CENTER", "Center transform refers to an unknown simulated bone.");
                var t = pair.Value;
                // Validate default(struct) too; public struct fields alone do not imply a valid basis.
                copy.Add(pair.Key, new PoseTransform(t.XAxis, t.YAxis, t.ZAxis, t.Translation));
            }
            SkeletonHash = state.SkeletonHash; ChainHash = state.ChainHash;
            ByBoneId = new ReadOnlyDictionary<string, PoseTransform>(copy);
        }

        internal void ValidateFor(SpringBoneState state)
        {
            Checks.Require(SkeletonHash == state.SkeletonHash && ChainHash == state.ChainHash && ByBoneId.Count == state.CurrentTails.Count,
                "SPRING_CENTER_CHANGED", "Center frame belongs to another skeleton or chain configuration.");
            foreach (var id in state.CurrentTails.Keys)
                Checks.Require(ByBoneId.ContainsKey(id), "SPRING_CENTER_CHANGED", "Center frame refers to different simulated bones.");
        }
    }
}
