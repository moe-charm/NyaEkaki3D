using System;
using System.Collections.Generic;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Owns transient VRM0 simulation, publishing only poses projected to the authored skin.</summary>
    public sealed class Vrm0SpringPreview : IVrmSpringPreview
    {
        readonly VrmSpringSession source;
        readonly ImportedRigSession rig;
        Vrm0SpringRuntime runtime;
        SpringPreviewController controller;
        Dictionary<string, float> scales;
        public PoseSet Pose { get; private set; }
        public SpringBoneState State => controller.State;
        public bool IsPlaying => controller.IsPlaying;
        public long CompletedSteps => controller.CompletedSteps;
        public double PendingSeconds => controller.PendingSeconds;

        public Vrm0SpringPreview(VrmSpringSession source, ImportedRigSession rig, AuthoringGraph graph, PoseSet pose)
        {
            Checks.Require(source != null && rig != null, "INVALID_VRM", "Spring and rig sessions are required.");
            this.source = source; this.rig = rig; Reset(graph, pose);
        }

        public void Play() => controller.Play();
        public void Pause() => controller.Pause();

        public void Reset(AuthoringGraph graph, PoseSet pose)
        {
            var nextRuntime = new Vrm0SpringRuntime(source, rig, graph);
            var input = nextRuntime.Rig.FromAuthored(graph, pose);
            var nextScales = nextRuntime.Scales(input);
            var next = new SpringPreviewController(nextRuntime.Rig.Skeleton, nextRuntime.Chains(input), input, nextRuntime.Colliders(input), nextRuntime.Centers(input));
            var projected = nextRuntime.Rig.ToAuthored(graph, input);
            runtime = nextRuntime; scales = nextScales; controller = next; Pose = projected;
        }

        public PoseSet Advance(AuthoringGraph graph, PoseSet pose, float elapsedSeconds)
        {
            var input = runtime.Rig.FromAuthored(graph, pose);
            foreach (var pair in runtime.Scales(input))
                Checks.Require(Math.Abs(pair.Value - scales[pair.Key]) <= scales[pair.Key] * 1e-5f,
                    "SPRING_SCALE_CHANGED", "Spring scale changed; reset to rebuild radii and history.");
            var next = controller.Fork();
            var simulated = next.Advance(input, runtime.Colliders(input), runtime.Centers(input), elapsedSeconds);
            var projected = runtime.Rig.ToAuthored(graph, simulated);
            controller = next; Pose = projected;
            return Pose;
        }
    }
}
