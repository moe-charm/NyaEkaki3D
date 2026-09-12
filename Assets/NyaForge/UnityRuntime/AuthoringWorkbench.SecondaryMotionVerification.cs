using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Rig;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifySecondaryMotionRebind(List<string> checks)
        {
            Check(importedSecondaryMotionAsset != null && importedSecondaryMotionDocument != null && importedSecondaryMotionDocument.IsSupported,
                "Secondary-motion rebind verification needs a supported imported asset");
            var graph = workspace.Document.Objects[0].Graph;
            var skeletonNode = graph.Nodes.Values.First(node => node.TypeId == BuiltinNodes.Skeleton);
            var moved = SkeletonEditing.MoveBone(skeletonNode.Skeleton, skeletonNode.Skeleton.Bones[0].BoneId, new Vec3(.001f, 0, 0), new Vec3(.001f, 0, 0));
            Execute(AuthoringOperation.ReplaceGraph(graph.ReplaceNode(GraphNode.SkeletonNode(skeletonNode.NodeId, moved))));
            Check(secondaryMotionStatus.text.Contains("再bind") && secondaryMotionRebindButton.enabledSelf, "Stale secondary-motion setup did not expose the explicit identity rebind action");
            // The rig binding and pose have their own identity and must be repaired
            // before the evaluated graph can be used again. Keep this separate from
            // the secondary-motion action so both explicit boundaries are exercised.
            RebindRig();
            string beforeRebind = importedSecondaryMotionDocument.RawHash;
            RebindSecondaryMotionIdentity();
            Check(importedSecondaryMotionAsset.SkeletonHash == moved.ContentHash && secondaryMotionStatus.text.Contains("保存済み"), "Identity rebind did not repin the secondary-motion skeleton");
            string afterRebind = importedSecondaryMotionDocument.RawHash;
            Execute(AuthoringOperation.Undo());
            Check(importedSecondaryMotionDocument != null && importedSecondaryMotionDocument.RawHash == beforeRebind, "Secondary-motion rebind Undo did not restore the imported attachment");
            Execute(AuthoringOperation.Redo());
            Check(importedSecondaryMotionDocument != null && importedSecondaryMotionDocument.RawHash == afterRebind, "Secondary-motion rebind Redo did not restore the rebound attachment");
            Check(workspace.IsDirty, "Identity rebind should require a project save");
            checks.Add("Secondary-motion GUI rebind: stale skeleton exposes same-BoneId rebind, repins hash, and Undo/Redo restores attachment bytes");
        }
    }
}
