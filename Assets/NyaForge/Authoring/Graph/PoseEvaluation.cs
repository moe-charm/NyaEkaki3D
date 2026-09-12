using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Graph
{
    /// <summary>Typed graph value for a complete rest-relative pose.</summary>
    public sealed class GraphPoseValue
    {
        public PoseSet Pose { get; }
        internal GraphPoseValue(PoseSet pose)
        {
            Checks.Require(pose != null, "INVALID_POSE", "Pose value is required."); Pose = pose;
        }
    }
}
