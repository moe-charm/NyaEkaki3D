using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Graph;

namespace NyaForge.Authoring.Import
{
    /// <summary>A source node rotation target. TailNodeIndex -1 denotes a virtual endpoint.</summary>
    public sealed class Vrm0SpringTarget
    {
        public int NodeIndex { get; }
        public int TailNodeIndex { get; }
        public Vec3 Head { get; }
        public Vec3 Tail { get; }
        public VrmSpringJoint Settings { get; }
        internal Vrm0SpringTarget(int node, int tailNode, Vec3 head, Vec3 tail, VrmSpringJoint settings)
        {
            NodeIndex = node; TailNodeIndex = tailNode; Head = head; Tail = tail; Settings = settings;
        }
    }

    public sealed class Vrm0ExpandedSpring
    {
        public string Name { get; }
        public int CenterNodeIndex { get; }
        public IReadOnlyList<int> ColliderGroupIndices { get; }
        public IReadOnlyList<Vrm0SpringTarget> Targets { get; }
        internal Vrm0ExpandedSpring(VrmSpringBoneGroup group, IEnumerable<Vrm0SpringTarget> targets)
        {
            Name = group.Name; CenterNodeIndex = group.CenterNodeIndex;
            ColliderGroupIndices = Array.AsReadOnly(group.ColliderGroupIndices.ToArray());
            Targets = Array.AsReadOnly(targets.ToArray());
        }
    }

    /// <summary>Translation-profile VRM0 subtree expansion, independent of skin joint mapping.</summary>
    public static class Vrm0SpringExpansion
    {
        public static IReadOnlyList<Vrm0ExpandedSpring> Resolve(VrmSpringSession source, ImportedRigSession rig, AuthoringGraph graph)
        {
            Checks.Require(source != null && rig != null, "INVALID_VRM", "Spring and rig sessions are required.");
            rig.ValidateSource(source.SourceHash); rig.Resolve(graph);
            return Expand(source, rig.Hierarchy);
        }

        internal static IReadOnlyList<Vrm0ExpandedSpring> Expand(VrmSpringSession source, ImportedSourceHierarchy hierarchy)
        {
            Checks.Require(source != null && source.Format == "vrm0", "UNSUPPORTED_FORMAT", "VRM0 subtree expansion requires a legacy spring session.");
            Checks.Require(hierarchy != null, "IMPORT_NODE_HIERARCHY_MISSING", "Source node hierarchy is unknown; reimport the model.");
            var result = new List<Vrm0ExpandedSpring>(); var visited = new HashSet<int>();
            foreach (var group in source.SpringBones)
            {
                Checks.Require(group != null && group.RootBoneNodes.Count > 0 && group.Joints.Count == group.RootBoneNodes.Count,
                    "INVALID_VRM_SPRING_CHAIN", "VRM0 roots require matching source settings.");
                Checks.Require(group.CenterNodeIndex >= -1 && group.CenterNodeIndex < hierarchy.Parents.Count, "INVALID_VRM_SPRING_CENTER", "Source center is out of range.");
                foreach (int index in group.ColliderGroupIndices)
                    Checks.Require(index >= 0 && index < source.ColliderGroups.Count, "SPRING_COLLIDER_MISSING", "Referenced collider group is missing.");
                var targets = new List<Vrm0SpringTarget>();
                for (int r = 0; r < group.RootBoneNodes.Count; r++)
                {
                    int root = group.RootBoneNodes[r]; var settings = group.Joints[r];
                    Checks.Require(root >= 0 && root < hierarchy.Parents.Count && settings != null && settings.NodeIndex == root,
                        "INVALID_VRM_SPRING_CHAIN", "Source root settings are inconsistent.");
                    var stack = new Stack<int>(); stack.Push(root);
                    while (stack.Count > 0)
                    {
                        int node = stack.Pop();
                        Checks.Require(visited.Add(node), "DUPLICATE_SPRING_JOINT", "VRM0 root subtrees overlap; repeated simulation is unsupported.");
                        var children = hierarchy.Children[node]; int tailNode = children.Count == 0 ? -1 : children[0];
                        var head = hierarchy.Origins[node]; Vec3 tail;
                        if (tailNode >= 0) tail = hierarchy.Origins[tailNode];
                        else
                        {
                            int parent = hierarchy.Parents[node];
                            Checks.Require(parent >= 0, "INVALID_VRM_SPRING_ENDPOINT", "A parentless leaf has no terminal direction.");
                            var delta = head - hierarchy.Origins[parent];
                            double length = Length(delta);
                            Checks.Require(length > 1e-8, "INVALID_VRM_SPRING_ENDPOINT", "Coincident terminal and parent have no direction.");
                            tail = head + new Vec3((float)(delta.X / length * .07), (float)(delta.Y / length * .07), (float)(delta.Z / length * .07));
                        }
                        Checks.Finite(tail);
                        Checks.Require(Length(tail - head) > 1e-8, "INVALID_VRM_SPRING_ENDPOINT", "Spring head and first-child endpoint coincide.");
                        targets.Add(new Vrm0SpringTarget(node, tailNode, head, tail,
                            new VrmSpringJoint(node, settings.HitRadius, settings.Stiffness, settings.GravityPower, settings.DragForce, settings.GravityDirection)));
                        // Stack reverses insertion; visit in original source child order.
                        for (int i = children.Count - 1; i >= 0; i--) stack.Push(children[i]);
                    }
                }
                result.Add(new Vrm0ExpandedSpring(group, targets));
            }
            return result.AsReadOnly();
        }

        static double Length(Vec3 v) => Math.Sqrt((double)v.X * v.X + (double)v.Y * v.Y + (double)v.Z * v.Z);
    }
}
