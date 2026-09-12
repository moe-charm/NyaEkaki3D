using System;
using System.Collections.Generic;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Owns one source-pinned VRM1 preview and refreshes external transforms for each frame.</summary>
    public sealed class Vrm1SpringPreview : IVrmSpringPreview
    {
        readonly VrmSpringSession source;
        readonly ImportedRigSession rig;
        IReadOnlyList<VrmSpringChainBinding> bindings;
        Dictionary<string, float> scales;
        SpringPreviewController controller;

        public PoseSet Pose => controller.Pose;
        public SpringBoneState State => controller.State;
        public bool IsPlaying => controller.IsPlaying;
        public long CompletedSteps => controller.CompletedSteps;
        public double PendingSeconds => controller.PendingSeconds;

        public Vrm1SpringPreview(VrmSpringSession source, ImportedRigSession rig, AuthoringGraph graph, PoseSet pose)
        {
            Checks.Require(source != null && rig != null, "INVALID_VRM", "Spring and rig sessions are required.");
            this.source = source; this.rig = rig;
            Reset(graph, pose);
        }

        public void Play() { controller.Play(); }
        public void Pause() { controller.Pause(); }

        public void Reset(AuthoringGraph graph, PoseSet pose)
        {
            var nextBindings = Vrm1SpringChainResolver.Resolve(source, rig, graph);
            Checks.Require(nextBindings.Count > 0, "SPRING_EMPTY", "This model has no playable Spring chains.");
            var chains = Vrm1SpringRuntimeAdapter.CreateChains(source, rig, graph, pose);
            var colliders = VrmSpringColliderAdapter.Convert(source, rig, graph, pose);
            var centers = VrmSpringCenterAdapter.Create(nextBindings, rig, graph, pose);
            var nextScales = ReadScales(nextBindings, pose);
            var next = new SpringPreviewController(graph.Nodes[rig.SkeletonNodeId].Skeleton, chains, pose, colliders, centers);
            // A failed rebuild must not discard the last working preview.
            bindings = nextBindings; scales = nextScales; controller = next;
        }

        public PoseSet Advance(AuthoringGraph graph, PoseSet pose, float elapsedSeconds)
        {
            rig.Resolve(graph);
            Checks.Require(pose != null, "INVALID_POSE", "Preview requires a base pose.");
            pose.ValidateFor(graph.Nodes[rig.SkeletonNodeId].Skeleton);
            var currentScales = ReadScales(bindings, pose);
            foreach (var pair in currentScales)
                Checks.Require(Math.Abs(pair.Value - scales[pair.Key]) <= scales[pair.Key] * 1e-5f,
                    "SPRING_SCALE_CHANGED", "Spring scale changed; reset the preview to rebuild hit radii and history.");
            var colliders = VrmSpringColliderAdapter.Convert(source, rig, graph, pose);
            var centers = VrmSpringCenterAdapter.Create(bindings, rig, graph, pose);
            return controller.Advance(pose, colliders, centers, elapsedSeconds);
        }

        static Dictionary<string, float> ReadScales(IReadOnlyList<VrmSpringChainBinding> chains, PoseSet pose)
        {
            var result = new Dictionary<string, float>();
            foreach (var chain in chains) foreach (var pair in chain.Pairs)
                result.Add(pair.HeadBoneId, PoseUniformScale.Require(pose.ByBoneId[pair.HeadBoneId].Transform));
            return result;
        }
    }
}
