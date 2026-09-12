using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>VRM0 source settings and external frames for the transient source-node rig.</summary>
    internal sealed class Vrm0SpringRuntime
    {
        readonly VrmSpringSession source;
        readonly IReadOnlyList<Vrm0ExpandedSpring> groups;
        internal ImportedPreviewRig Rig { get; }

        internal Vrm0SpringRuntime(VrmSpringSession source, ImportedRigSession rig, AuthoringGraph graph)
        {
            groups = Vrm0SpringExpansion.Resolve(source, rig, graph);
            Checks.Require(groups.Count > 0, "SPRING_EMPTY", "This model has no VRM0 spring roots.");
            this.source = source;
            var nodes = groups.SelectMany(g => g.Targets.Select(t => t.NodeIndex))
                .Concat(groups.Where(g => g.CenterNodeIndex >= 0).Select(g => g.CenterNodeIndex))
                .Concat(source.ColliderGroups.SelectMany(g => g.ColliderNodeIndices));
            Rig = new ImportedPreviewRig(rig, graph, nodes);
        }

        internal IReadOnlyList<SpringBoneChain> Chains(PoseSet pose)
        {
            var chains = new List<SpringBoneChain>();
            foreach (var group in groups)
            {
                var joints = new List<SpringBoneJointSettings>();
                foreach (var target in group.Targets)
                {
                    var settings = target.Settings;
                    Checks.Require(settings.GravityDirection.HasValue, "IMPORT_SPRING_DETAILS_MISSING", "Gravity direction is unknown; reimport the model.");
                    var gravity = settings.GravityDirection.Value;
                    string id = Rig.ByNode[target.NodeIndex];
                    joints.Add(new SpringBoneJointSettings(id, settings.HitRadius * PoseUniformScale.Require(pose.ByBoneId[id].Transform),
                        settings.Stiffness, settings.GravityPower, new Vec3(gravity.X, gravity.Y, -gravity.Z), settings.DragForce,
                        target.Tail - target.Head, new Vec3(), SpringIntegrationMode.VrmReference));
                }
                chains.Add(new SpringBoneChain(string.IsNullOrWhiteSpace(group.Name) ? "VRM0 spring " + chains.Count : group.Name, joints, group.ColliderGroupIndices));
            }
            return chains.AsReadOnly();
        }

        internal Dictionary<string, float> Scales(PoseSet pose) => groups.SelectMany(g => g.Targets).ToDictionary(
            t => Rig.ByNode[t.NodeIndex], t => PoseUniformScale.Require(pose.ByBoneId[Rig.ByNode[t.NodeIndex]].Transform));

        internal IReadOnlyDictionary<string, PoseTransform> Centers(PoseSet pose)
        {
            var result = new Dictionary<string, PoseTransform>();
            foreach (var group in groups)
            {
                var frame = group.CenterNodeIndex < 0 ? PoseTransform.Identity : pose.ByBoneId[Rig.ByNode[group.CenterNodeIndex]].Transform;
                foreach (var target in group.Targets) result.Add(Rig.ByNode[target.NodeIndex], frame);
            }
            return result;
        }

        internal IReadOnlyList<SpringBoneColliderGroup> Colliders(PoseSet pose)
            => VrmSpringColliderAdapter.Convert(source,
                (node, point) => pose.ByBoneId[Rig.ByNode[node]].Transform.TransformPoint(point),
                node => PoseUniformScale.Require(pose.ByBoneId[Rig.ByNode[node]].Transform));
    }
}
