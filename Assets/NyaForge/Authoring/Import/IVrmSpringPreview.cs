using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Workbench-facing playback contract; each format owns its coordinate adapters.</summary>
    public interface IVrmSpringPreview
    {
        PoseSet Pose { get; }
        SpringBoneState State { get; }
        bool IsPlaying { get; }
        long CompletedSteps { get; }
        double PendingSeconds { get; }
        void Play();
        void Pause();
        void Reset(AuthoringGraph graph, PoseSet pose);
        PoseSet Advance(AuthoringGraph graph, PoseSet pose, float elapsedSeconds);
    }
}
