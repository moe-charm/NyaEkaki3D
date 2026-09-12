using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyAttachment(string output, List<string> checks)
        {
            var previous = workspace; string previousPath = savedDirectory;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                string targetObjectId = Guid.NewGuid().ToString("D"), targetGraphId = Guid.NewGuid().ToString("D");
                string boneId = Guid.NewGuid().ToString("D"), skeletonNodeId = Guid.NewGuid().ToString("D"), poseNodeId = Guid.NewGuid().ToString("D");
                var bone = new BoneDefinition(boneId, "neck", "", new Vec3(0, .12f, 0), new Vec3(0, .22f, 0));
                var skeleton = new SkeletonDefinition(new[] { bone });
                var pose = PoseSet.Create(skeleton, new[] { new BonePose(boneId, PoseTransform.RotationZ(25, bone.Head)) });
                string planeId = Guid.NewGuid().ToString("D"), outputId = Guid.NewGuid().ToString("D");
                var targetGraph = new AuthoringGraph(targetGraphId,
                    new[] { GraphNode.Plane(planeId, .1f, .1f), GraphNode.Output(outputId), GraphNode.SkeletonNode(skeletonNodeId, skeleton), GraphNode.PoseNode(poseNodeId, pose) },
                    new[] { new GraphEdge(planeId, "mesh", outputId, "mesh"), new GraphEdge(skeletonNodeId, "skeleton", poseNodeId, "skeleton") }, outputId);
                Execute(AuthoringOperation.AddGraph(targetGraph, targetObjectId));
                string sourceHash = Checks.Hash(new byte[] { 1, 7, 3 });
                var session = new ImportedRigSession(sourceHash, skeleton.ContentHash, targetGraphId, skeletonNodeId,
                    new Dictionary<int, string> { [0] = boneId }, new Dictionary<string, int>());
                importedRigSessions[targetGraphId] = session;
                var owned = new Dictionary<string, byte[]> { [ProjectAttachments.RigSessions] = ImportedRigSessionsCodec.Write(importedRigSessions) };
                workspace.SetAttachments(new ProjectAttachments(owned));
                CreateChokerGraph();
                var accessory = workspace.Document.ActiveObject;
                Check(root.Q<Foldout>("object-attachment") != null && root.Q<DropdownField>("object-attachment-target") != null && root.Q<DropdownField>("object-attachment-bone") != null,
                    "Attachment GUI controls were not built");
                var attachment = GraphNode.AttachmentNode(Guid.NewGuid().ToString("D"), targetObjectId, boneId, skeleton.ContentHash, new Vec3());
                Execute(AuthoringOperation.AddNode(attachment));
                Refresh();
                Check(attachmentTarget.choices.Count == 1 && attachmentBone.choices.Count == 1, "Attachment GUI did not expose the resolved target and BoneId");
                var expectedRoot = new UnityEngine.Vector3(bone.Head.X, bone.Head.Y, bone.Head.Z);
                Check(UnityEngine.Vector3.Distance(projection.DisplayObject.transform.position, expectedRoot) < 1e-5f, "Accessory root did not resolve to the target bone rest position");
                var initialRotation = projection.DisplayObject.transform.localRotation;
                Check(initialRotation != UnityEngine.Quaternion.identity, "Accessory attachment did not expose the resolved bone pose");
                Execute(AuthoringOperation.SelectObject(targetObjectId));
                var movedPose = PoseSet.Create(skeleton, new[] { new BonePose(boneId, PoseTransform.RotationZ(65, bone.Head)) });
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(poseNodeId, movedPose)));
                Execute(AuthoringOperation.SelectObject(accessory.ObjectId));
                Check(UnityEngine.Quaternion.Angle(initialRotation, projection.DisplayObject.transform.localRotation) > 1f, "Accessory root did not follow a changed target pose");
                string project = Path.Combine(output, "attachment-project"); projectPath.SetValueWithoutNotify(project); SaveProject();
                string savedHash = workspace.Document.StateHash; OpenProject();
                Check(workspace.Document.StateHash == savedHash && !workspace.IsDirty, "Attachment project Save/Open changed the document");
                var reopened = workspace.Document.ActiveObject.Graph.Nodes.Values.Single(node => node.TypeId == BuiltinNodes.Attachment);
                Check(reopened.AttachmentBoneId == boneId && reopened.AttachmentTargetObjectId == targetObjectId && reopened.AttachmentSkeletonHash == skeleton.ContentHash, "Attachment identity was not restored");
                Check(importedRigSessions.ContainsKey(targetGraphId), "Rig session table was not restored for attachment resolution");
                checks.Add("accessory object.attachment: stable BoneId/root pose, preview follow-through, native Save/Open and rig-session restore");
            }
            finally { ReplaceWorkspace(previous, previousPath); }
        }
    }
}
