using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    /// <summary>Transient source-node skeleton and reversible projection to the authored skin pose.</summary>
    public sealed class ImportedPreviewRig
    {
        readonly ImportedRigSession rig;
        readonly SkeletonDefinition authored;
        public SkeletonDefinition Skeleton { get; }
        public IReadOnlyDictionary<int, string> ByNode { get; }

        public ImportedPreviewRig(ImportedRigSession rig, AuthoringGraph graph, IEnumerable<int> requiredNodes)
        {
            Checks.Require(rig != null && requiredNodes != null, "INVALID_IMPORT", "Rig and required source nodes are required.");
            rig.Resolve(graph);
            Checks.Require(rig.Hierarchy != null, "IMPORT_NODE_HIERARCHY_MISSING", "Preview needs the original node hierarchy.");
            this.rig = rig; authored = graph.Nodes[rig.SkeletonNodeId].Skeleton;
            var hierarchy = rig.Hierarchy; var selected = new HashSet<int>();
            foreach (int start in requiredNodes.Concat(rig.NodeToBone.Keys))
            {
                Checks.Require(start >= 0 && start < hierarchy.Parents.Count, "IMPORT_BONE_UNMAPPED", "Required source node is out of range.");
                int node = start;
                while (node >= 0 && selected.Add(node)) node = hierarchy.Parents[node];
            }
            Checks.Require(selected.Count <= SkeletonDefinition.MaxBones, "BUDGET_EXCEEDED", "Required source nodes and ancestors exceed the 512-bone preview budget.");
            var ids = selected.ToDictionary(n => n, n => rig.NodeToBone.TryGetValue(n, out var id) ? id : PreviewId(rig.SourceHash, n));
            var bones = new List<BoneDefinition>();
            foreach (int node in selected.OrderBy(n => n))
            {
                int parent = hierarchy.Parents[node]; var head = hierarchy.Origins[node];
                // Display tail only. Dynamics supplies the explicit VRM endpoint separately.
                var tail = head + new Vec3(0, .05f, 0);
                bones.Add(new BoneDefinition(ids[node], "Source " + node, parent < 0 ? "" : ids[parent], head, tail));
            }
            Skeleton = new SkeletonDefinition(bones); ByNode = new ReadOnlyDictionary<int, string>(ids);
        }

        public PoseSet FromAuthored(AuthoringGraph graph, PoseSet pose)
        {
            rig.Resolve(graph); Checks.Require(pose != null, "INVALID_POSE", "Authored pose is required."); pose.ValidateFor(authored);
            var result = new List<BonePose>();
            foreach (var pair in ByNode)
            {
                int ancestor = pair.Key;
                while (ancestor >= 0 && !rig.NodeToBone.ContainsKey(ancestor)) ancestor = rig.Hierarchy.Parents[ancestor];
                var origin = rig.Hierarchy.Origins[pair.Key]; PoseTransform frame;
                if (ancestor < 0) frame = PoseTransform.FromTranslation(origin);
                else
                {
                    string id = rig.NodeToBone[ancestor]; var transform = pose.ByBoneId[id].Transform;
                    frame = Rebase(transform, origin - authored.ById[id].Head);
                }
                result.Add(new BonePose(pair.Value, frame));
            }
            return PoseSet.Create(Skeleton, result);
        }

        public PoseSet ToAuthored(AuthoringGraph graph, PoseSet pose)
        {
            rig.Resolve(graph); Checks.Require(pose != null, "INVALID_POSE", "Preview pose is required."); pose.ValidateFor(Skeleton);
            var result = new List<BonePose>();
            foreach (var pair in rig.NodeToBone)
            {
                var transform = pose.ByBoneId[ByNode[pair.Key]].Transform;
                result.Add(new BonePose(pair.Value, Rebase(transform, authored.ById[pair.Value].Head - rig.Hierarchy.Origins[pair.Key])));
            }
            return PoseSet.Create(authored, result);
        }

        static PoseTransform Rebase(PoseTransform transform, Vec3 offset)
            => new PoseTransform(transform.XAxis, transform.YAxis, transform.ZAxis, transform.TransformPoint(offset));

        static string PreviewId(string hash, int node)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(hash + ":preview-source:" + node.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                return new Guid(bytes.Take(16).ToArray()).ToString("D");
            }
        }
    }
}
