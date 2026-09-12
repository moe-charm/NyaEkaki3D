namespace NyaForge.Authoring.Rig
{
    /// <summary>Immutable rig payload returned by the dedicated rig codec.</summary>
    public sealed class RigAsset
    {
        public SkeletonDefinition Skeleton { get; }
        public SkinBinding Binding { get; }
        internal RigAsset(SkeletonDefinition skeleton, SkinBinding binding) { Skeleton = skeleton; Binding = binding; }
    }
}
