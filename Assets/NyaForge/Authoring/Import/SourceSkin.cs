using System;
using System.Collections.Generic;
using System.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Immutable source joint-slot mapping and bind data, independent of authored bone limits.</summary>
    public sealed class SourceSkin
    {
        public SourceNodeTransforms Nodes { get; }
        public int SkinIndex { get; }
        public int? SkeletonRoot { get; }
        public IReadOnlyList<int> Joints { get; }
        public IReadOnlyList<SourceAffine> InverseBindMatrices { get; }
        public bool HasExplicitInverseBindMatrices { get; }

        /// <param name="inverseBindMatrices">Null means omitted in a freshly decoded source, never unknown legacy data.</param>
        public SourceSkin(SourceNodeTransforms nodes, int skinIndex, IReadOnlyList<int> joints,
            IReadOnlyList<SourceAffine> inverseBindMatrices, int? skeletonRoot = null)
        {
            Checks.Require(nodes != null && skinIndex >= 0, "INVALID_IMPORT", "Source skin requires nodes and a nonnegative source index.");
            Checks.Require(joints != null && joints.Count > 0 && joints.Count <= nodes.Local.Count,
                "INVALID_IMPORT", "Source skin requires bounded joint slots.");
            var jointCopy = joints.ToArray();
            Checks.Require(jointCopy.All(node => node >= 0 && node < nodes.Local.Count) && jointCopy.Distinct().Count() == jointCopy.Length,
                "INVALID_IMPORT", "Skin joints must be unique source node indices.");
            if (skeletonRoot.HasValue)
            {
                int root = skeletonRoot.Value;
                Checks.Require(root >= 0 && root < nodes.Local.Count, "INVALID_IMPORT", "Skeleton root is outside source nodes.");
                foreach (int joint in jointCopy)
                {
                    int ancestor = joint;
                    while (ancestor >= 0 && ancestor != root) ancestor = nodes.Hierarchy.Parents[ancestor];
                    Checks.Require(ancestor == root, "INVALID_IMPORT", "Skeleton root must be an ancestor of every joint.");
                }
            }
            // Keep extra accessor entries too: glTF permits count >= joint count.
            Checks.Require(inverseBindMatrices == null || (inverseBindMatrices.Count >= jointCopy.Length &&
                inverseBindMatrices.Count <= AuthoringLimits.MaxBlobBytes / 64 && inverseBindMatrices.All(matrix => matrix != null)),
                "INVALID_IMPORT", "Inverse-bind entries must cover all joint slots and fit the import budget.");
            Nodes = nodes; SkinIndex = skinIndex; SkeletonRoot = skeletonRoot;
            Joints = Array.AsReadOnly(jointCopy);
            HasExplicitInverseBindMatrices = inverseBindMatrices != null;
            InverseBindMatrices = Array.AsReadOnly(inverseBindMatrices == null
                ? Enumerable.Repeat(SourceAffine.Identity, jointCopy.Length).ToArray() : inverseBindMatrices.ToArray());
        }

        /// <summary>Maps a mesh-space point to source world at the supplied joint frame. No mesh-node transform is added.</summary>
        public SourceAffine JointMatrix(int jointSlot, SourceAffine jointWorld)
        {
            Checks.Require(jointSlot >= 0 && jointSlot < Joints.Count && jointWorld != null,
                "INVALID_IMPORT", "A valid joint slot and world frame are required.");
            return jointWorld.Compose(InverseBindMatrices[jointSlot]);
        }
    }
}
