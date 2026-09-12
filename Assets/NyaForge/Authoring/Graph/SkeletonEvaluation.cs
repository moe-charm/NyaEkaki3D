using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Typed graph value for a rest skeleton. Skin binding nodes will consume this port in the next C2 slice.</summary>
    public sealed class GraphSkeletonValue
    {
        public SkeletonDefinition Skeleton { get; }
        internal GraphSkeletonValue(SkeletonDefinition skeleton) { Skeleton = skeleton; }
    }
}
