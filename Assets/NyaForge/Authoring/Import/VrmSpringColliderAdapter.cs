using System;
using System.Collections.Generic;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Converts retained source colliders to an immutable snapshot in posed avatar space.</summary>
    public static class VrmSpringColliderAdapter
    {
        public static IReadOnlyList<SpringBoneColliderGroup> Convert(VrmSpringSession springs, ImportedRigSession rig, AuthoringGraph graph, PoseSet pose)
        {
            Checks.Require(springs != null && rig != null, "INVALID_VRM", "Spring and imported rig sessions are required.");
            rig.ValidateSource(springs.SourceHash);
            Checks.Require(springs.Format == "vrm0" || springs.Format == "vrm1", "UNSUPPORTED_FORMAT", "Unknown VRM collider coordinate convention.");
            var space = new ImportedNodeSpace(rig, graph, pose);
            return Convert(springs, space.TransformPoint, space.UniformScale);
        }

        internal static IReadOnlyList<SpringBoneColliderGroup> Convert(VrmSpringSession springs, Func<int, Vec3, Vec3> transformPoint, Func<int, float> uniformScale)
        {
            var groups = new List<SpringBoneColliderGroup>();
            foreach (var group in springs.ColliderGroups)
            {
                Checks.Require(group != null, "INVALID_VRM", "Spring collider group cannot be null.");
                Checks.Require(group.ColliderCount <= SpringBoneColliderGroup.MaxColliders, "BUDGET_EXCEEDED", "Collider group exceeds runtime capacity.");
                Checks.Require(group.ColliderCount == 0 || group.Shapes != null, "IMPORT_COLLIDER_DETAILS_MISSING", "Collider geometry was not retained; reimport the source model.");
                var colliders = new List<SpringBoneCollider>();
                for (int i = 0; i < group.ColliderCount; i++)
                {
                    var shape = group.Shapes[i];
                    Checks.Require(shape.Offset.HasValue && (shape.Kind == "sphere" || shape.Tail.HasValue), "IMPORT_COLLIDER_DETAILS_MISSING", "Collider offset/tail is unknown.");
                    int node = group.ColliderNodeIndices[i];
                    float radius = shape.Radius * uniformScale(node);
                    Vec3 center = transformPoint(node, SourceLocal(springs.Format, shape.Offset.Value));
                    Vec3? tail = shape.Kind == "capsule" ? (Vec3?)transformPoint(node, SourceLocal(springs.Format, shape.Tail.Value)) : null;
                    colliders.Add(new SpringBoneCollider(center, radius, tail));
                }
                groups.Add(new SpringBoneColliderGroup("VRM collider group " + groups.Count, colliders));
            }
            return groups.AsReadOnly();
        }

        // VRM0 extension vectors retain Unity coordinates; standard VRM0 glTF export reverses Z.
        // VRM1 vectors already use the source glTF node space. No Unity viewport conversion here.
        static Vec3 SourceLocal(string format, Vec3 value) { return format == "vrm0" ? new Vec3(value.X, value.Y, -value.Z) : value; }
    }
}
