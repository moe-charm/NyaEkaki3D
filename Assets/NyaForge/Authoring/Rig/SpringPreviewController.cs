using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Rig
{
    /// <summary>Owns transient fixed-step playback; never writes authoring documents or Undo history.</summary>
    public sealed class SpringPreviewController
    {
        readonly SkeletonDefinition skeleton;
        readonly IReadOnlyList<SpringBoneChain> chains;
        public SpringBoneState State { get; private set; }
        public SpringCenterFrame Centers { get; private set; }
        public PoseSet Pose { get; private set; }
        public bool IsPlaying { get; private set; }
        public double PendingSeconds { get; private set; }
        public long CompletedSteps { get; private set; }

        public SpringPreviewController(SkeletonDefinition skeleton, IReadOnlyList<SpringBoneChain> chains, PoseSet pose,
            IReadOnlyList<SpringBoneColliderGroup> colliders, IReadOnlyDictionary<string, PoseTransform> centers)
        {
            Checks.Require(skeleton != null && chains != null, "INVALID_SPRING", "Preview skeleton and chains are required.");
            this.skeleton = skeleton; this.chains = System.Array.AsReadOnly(chains.ToArray());
            Reset(pose, colliders, centers);
        }

        // Configuration, poses, states and center frames are immutable. A fork can publish
        // a frame only after an outer adapter has also validated its projected output.
        internal SpringPreviewController Fork() => (SpringPreviewController)MemberwiseClone();

        public void Play() { IsPlaying = true; }
        public void Pause() { IsPlaying = false; }

        public void Reset(PoseSet pose, IReadOnlyList<SpringBoneColliderGroup> colliders, IReadOnlyDictionary<string, PoseTransform> centers)
        {
            var state = SpringBoneSimulator.CreateInitialState(skeleton, pose, chains, colliders);
            var frame = new SpringCenterFrame(state, centers);
            State = state; Centers = frame; Pose = pose; PendingSeconds = 0; CompletedSteps = 0; IsPlaying = false;
        }

        public PoseSet Advance(PoseSet basePose, IReadOnlyList<SpringBoneColliderGroup> colliders,
            IReadOnlyDictionary<string, PoseTransform> centers, float elapsedSeconds)
        {
            // Validate elapsed time even when paused; paused wall time is never accumulated.
            int steps = SpringFixedClock.Plan(PendingSeconds, elapsedSeconds, out var remainder);
            if (!IsPlaying) { steps = 0; remainder = PendingSeconds; }
            var frame = new SpringCenterFrame(State, centers);
            var state = SpringCenterMotion.Transfer(State, Centers, frame);
            PoseSet output;
            if (steps == 0)
                output = SpringBoneSimulator.Step(skeleton, basePose, chains, colliders, state, 0).Pose;
            else
            {
                output = basePose;
                for (int i = 0; i < steps; i++)
                {
                    var result = SpringBoneSimulator.Step(skeleton, basePose, chains, colliders, state, (float)SpringFixedClock.Interval);
                    state = result.State; output = result.Pose;
                }
            }
            long completed = checked(CompletedSteps + steps);
            // Publish only after every substep succeeds. A redraw must not commit transported history.
            if (steps > 0) { State = state; Centers = frame; }
            Pose = output; PendingSeconds = remainder; CompletedSteps = completed;
            return output;
        }
    }
}
