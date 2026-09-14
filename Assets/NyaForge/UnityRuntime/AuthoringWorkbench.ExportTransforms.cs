using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Keeps source-skin transforms grouped at the export boundary. The
        /// export actions consume these maps, while import/session ownership
        /// remains in the Workbench state and graph.
        /// </summary>
        IReadOnlyDictionary<string, SourceAffine> SkinnedInstanceTransforms()
        {
            var result = new Dictionary<string, SourceAffine>(StringComparer.Ordinal);
            if (workspace?.Document?.Objects == null) return result;
            foreach (var item in workspace.Document.Objects)
            {
                if (item.Graph == null) continue;
                if (importedRigSessions.TryGetValue(item.Graph.GraphId, out var session) && session?.MeshInstanceTransform != null)
                    result[item.ObjectId] = session.MeshInstanceTransform;
            }
            if (importedRigSession?.MeshInstanceTransform != null && workspace.Document.ActiveObject?.Graph != null)
                result[workspace.Document.ActiveObject.ObjectId] = importedRigSession.MeshInstanceTransform;
            return result;
        }

        // glTF skinning uses joint world frames and inverse-bind matrices;
        // the selected skinned mesh affine is metadata only.
        IReadOnlyDictionary<string, SourceAffine> SkinnedNodeTransformsForExport()
            => new Dictionary<string, SourceAffine>(StringComparer.Ordinal);

        IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> SkinnedInverseBindMatrices()
        {
            var result = new Dictionary<string, IReadOnlyList<SourceAffine>>(StringComparer.Ordinal);
            if (workspace?.Document?.Objects == null) return result;
            foreach (var item in workspace.Document.Objects)
            {
                if (item.Graph == null) continue;
                if (importedRigSessions.TryGetValue(item.Graph.GraphId, out var session) && session?.SourceSkin?.InverseBindMatrices != null)
                    result[item.ObjectId] = ReorderInverseBinds(item.Graph, session);
            }
            if (importedRigSession?.SourceSkin?.InverseBindMatrices != null && workspace.Document.ActiveObject?.Graph != null)
                result[workspace.Document.ActiveObject.ObjectId] = ReorderInverseBinds(workspace.Document.ActiveObject.Graph, importedRigSession);
            return result;
        }

        IReadOnlyList<SourceAffine> ReorderInverseBinds(AuthoringGraph graph, ImportedRigSession session)
        {
            var skeletonNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Skeleton && node.Skeleton != null);
            if (skeletonNode == null) throw new InvalidOperationException("出力skeletonがありません。");
            var byBone = new Dictionary<string, SourceAffine>(StringComparer.Ordinal);
            for (int slot = 0; slot < session.SourceSkin.Joints.Count; slot++)
            {
                int sourceNode = session.SourceSkin.Joints[slot];
                if (!session.NodeToBone.TryGetValue(sourceNode, out var boneId)) continue;
                byBone[boneId] = session.SourceSkin.InverseBindMatrices[slot];
            }
            var reordered = new List<SourceAffine>(skeletonNode.Skeleton.Bones.Count);
            foreach (var bone in skeletonNode.Skeleton.Bones)
            {
                if (!byBone.TryGetValue(bone.BoneId, out var matrix))
                    throw new InvalidOperationException("inverse-bind matrixをBoneIdへ対応できません: " + bone.Name);
                reordered.Add(matrix);
            }
            return reordered;
        }

        IReadOnlyDictionary<string, IReadOnlyList<SourceAffine>> SkinnedJointLocalTransforms()
        {
            var result = new Dictionary<string, IReadOnlyList<SourceAffine>>(StringComparer.Ordinal);
            if (workspace?.Document?.Objects == null) return result;
            foreach (var item in workspace.Document.Objects)
            {
                if (item.Graph == null || !importedRigSessions.TryGetValue(item.Graph.GraphId, out var session) || session?.SourceSkin == null) continue;
                var skeletonNode = item.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Skeleton && node.Skeleton != null);
                if (skeletonNode == null) continue;
                var byBone = new Dictionary<string, SourceAffine>(StringComparer.Ordinal);
                for (int slot = 0; slot < session.SourceSkin.Joints.Count; slot++)
                    if (session.NodeToBone.TryGetValue(session.SourceSkin.Joints[slot], out var boneId)) byBone[boneId] = session.SourceSkin.Nodes.World[session.SourceSkin.Joints[slot]];
                var values = skeletonNode.Skeleton.Bones.Select(bone => byBone.TryGetValue(bone.BoneId, out var world)
                    ? (bone.ParentBoneId == "" ? world : byBone.TryGetValue(bone.ParentBoneId, out var parentWorld) ? parentWorld.Inverse().Compose(world) : null)
                    : null).ToArray();
                if (values.All(value => value != null)) result[item.ObjectId] = values;
            }
            if (importedRigSession?.SourceSkin != null && workspace.Document.ActiveObject?.Graph != null && !result.ContainsKey(workspace.Document.ActiveObject.ObjectId))
            {
                var graph = workspace.Document.ActiveObject.Graph;
                var skeletonNode = graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Skeleton && node.Skeleton != null);
                if (skeletonNode != null)
                {
                    var byBone = new Dictionary<string, SourceAffine>(StringComparer.Ordinal);
                    for (int slot = 0; slot < importedRigSession.SourceSkin.Joints.Count; slot++)
                        if (importedRigSession.NodeToBone.TryGetValue(importedRigSession.SourceSkin.Joints[slot], out var boneId)) byBone[boneId] = importedRigSession.SourceSkin.Nodes.World[importedRigSession.SourceSkin.Joints[slot]];
                    var values = skeletonNode.Skeleton.Bones.Select(bone => byBone.TryGetValue(bone.BoneId, out var world)
                        ? (bone.ParentBoneId == "" ? world : byBone.TryGetValue(bone.ParentBoneId, out var parentWorld) ? parentWorld.Inverse().Compose(world) : null)
                        : null).ToArray();
                    if (values.All(value => value != null)) result[workspace.Document.ActiveObject.ObjectId] = values;
                }
            }
            return result;
        }
    }
}
