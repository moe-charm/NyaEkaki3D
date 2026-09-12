using System.Collections.Generic;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Builds executable VRM1 chains in fixed avatar coordinates from validated source pairs.</summary>
    public static class Vrm1SpringRuntimeAdapter
    {
        public static IReadOnlyList<SpringBoneChain> CreateChains(VrmSpringSession source, ImportedRigSession rig, AuthoringGraph graph, PoseSet pose)
        {
            var bindings = Vrm1SpringChainResolver.Resolve(source, rig, graph);
            var skeleton = graph.Nodes[rig.SkeletonNodeId].Skeleton;
            Checks.Require(pose != null, "INVALID_POSE", "VRM chain conversion requires a pose.");
            pose.ValidateFor(skeleton);
            var chains = new List<SpringBoneChain>();
            foreach (var binding in bindings)
            {
                var joints = new List<SpringBoneJointSettings>();
                foreach (var pair in binding.Pairs)
                {
                    var settings = pair.Settings;
                    Checks.Require(settings.GravityDirection.HasValue, "IMPORT_SPRING_DETAILS_MISSING", "Gravity direction was not retained; reimport the model.");
                    var bone = skeleton.ById[pair.HeadBoneId];
                    // Restrict the reference force direction and hit sphere to similarity transforms.
                    float scale = PoseUniformScale.Require(pose.ByBoneId[pair.HeadBoneId].Transform);
                    joints.Add(new SpringBoneJointSettings(pair.HeadBoneId, settings.HitRadius * scale, settings.Stiffness,
                        settings.GravityPower, settings.GravityDirection.Value, settings.DragForce,
                        pair.SourceTailOrigin - bone.Head, pair.SourceHeadOrigin - bone.Head, SpringIntegrationMode.VrmReference));
                }
                string name = string.IsNullOrWhiteSpace(binding.Name) ? "VRM spring " + chains.Count : binding.Name;
                chains.Add(new SpringBoneChain(name, joints, binding.ColliderGroupIndices));
            }
            return chains.AsReadOnly();
        }
    }
}
