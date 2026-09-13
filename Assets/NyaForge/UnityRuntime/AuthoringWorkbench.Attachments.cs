using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout attachmentPanel;
        Label attachmentStatus;
        DropdownField attachmentTarget;
        DropdownField attachmentBone;
        FloatField attachmentOffsetX, attachmentOffsetY, attachmentOffsetZ;
        Button attachmentApply, attachmentRemove, accessorySkinBind, accessoryPoseCopy;
        readonly List<string> attachmentTargetIds = new List<string>();
        readonly List<string> attachmentBoneIds = new List<string>();
        string attachmentTargetChoice;
        string attachmentChoiceOwner;

        void BuildAttachments(VisualElement parent)
        {
            attachmentPanel = new Foldout { text = "小物をボーンへ装着", value = false, name = "object-attachment" };
            attachmentStatus = new Label { name = "object-attachment-status" };
            attachmentStatus.style.whiteSpace = WhiteSpace.Normal;
            attachmentPanel.Add(attachmentStatus);
            attachmentTarget = new DropdownField("アバター対象", new List<string> { "対象なし" }, 0) { name = "object-attachment-target" };
            attachmentTarget.RegisterValueChangedCallback(e =>
            {
                // DropdownField exposes the display label, while the authored
                // attachment contract uses the stable object ID. Keep the
                // selected ID so a subsequent Refresh cannot mistake the
                // label for an ID and fall back to the first avatar.
                int index = attachmentTarget.choices.IndexOf(e.newValue);
                attachmentTargetChoice = index >= 0 && index < attachmentTargetIds.Count ? attachmentTargetIds[index] : null;
                RefreshAttachmentControls();
            });
            attachmentPanel.Add(attachmentTarget);
            attachmentBone = new DropdownField("BoneId", new List<string> { "対象なし" }, 0) { name = "object-attachment-bone" };
            attachmentPanel.Add(attachmentBone);
            attachmentOffsetX = Number(attachmentPanel, "bone local X (mm)", 0, "object-attachment-offset-x");
            attachmentOffsetY = Number(attachmentPanel, "bone local Y (mm)", 0, "object-attachment-offset-y");
            attachmentOffsetZ = Number(attachmentPanel, "bone local Z (mm)", 0, "object-attachment-offset-z");
            attachmentApply = Button("この小物を装着", ApplyAttachment, "object-attachment-apply");
            attachmentRemove = Button("装着を解除", RemoveAttachment, "object-attachment-remove");
            accessorySkinBind = Button("衣装をavatar骨格へskin-bind（Root初期化）", BindAccessoryToAvatar, "object-skin-bind");
            accessoryPoseCopy = Button("avatarの現在poseを衣装へコピー", CopyAvatarPose, "object-skin-pose-copy");
            attachmentPanel.Add(attachmentApply); attachmentPanel.Add(attachmentRemove); attachmentPanel.Add(accessorySkinBind); attachmentPanel.Add(accessoryPoseCopy);
            var help = new Label("明示したstable BoneIdへ剛体追従します。衣装skin-bindは選択avatarの骨格をコピーし、全頂点をRootへ初期化してRig panelでweight paintできます。skin-bind後はavatarの現在poseをボタンで衣装へコピーして保存できます。名前で推測せず、装着offsetは基準姿勢のbone localメートルで保存します。自動fitや貫通判定は別機能です。");
            help.style.whiteSpace = WhiteSpace.Normal; attachmentPanel.Add(help);
            parent.Add(attachmentPanel);
        }

        GraphNode ActiveAttachmentNode()
        {
            if (!IsGraph) return null;
            return workspace.Document.ActiveObject.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Attachment);
        }

        ImportedRigSession RigFor(AuthoringObject avatar)
        {
            if (avatar?.Graph == null) return null;
            return importedRigSessions.TryGetValue(avatar.Graph.GraphId, out var session) ? session : null;
        }

        AuthoringObject FindObject(string id)
        {
            return workspace?.Document?.Objects.FirstOrDefault(item => item.ObjectId == id);
        }

        PoseTransform? ResolveAttachmentPose(AuthoringObject accessory, out string diagnostic, out string key)
        {
            diagnostic = ""; key = "";
            if (accessory?.Graph == null) return null;
            var nodes = accessory.Graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.Attachment).ToArray();
            if (nodes.Length == 0) return null;
            if (nodes.Length != 1) { diagnostic = "装着設定が複数あります。1件へ整理してください。"; return null; }
            var node = nodes[0]; var avatar = FindObject(node.AttachmentTargetObjectId);
            if (avatar == null || avatar.Graph == null) { diagnostic = "装着先avatar objectが見つかりません。"; return null; }
            var session = RigFor(avatar);
            if (session == null) { diagnostic = "装着先のimported skeletonがありません。先にVRM/GLB avatarを取り込んでください。"; return null; }
            try
            {
                var map = session.Resolve(avatar.Graph);
                ChecksForAttachment(node, map);
                var evaluation = avatar.ObjectId == workspace.Document.ActiveObjectId ? workspace.Preview.Evaluation : avatar.EvaluateGraph();
                var pose = evaluation.PoseOutputs.Values.FirstOrDefault(value => value.Pose.SkeletonHash == session.SkeletonHash)?.Pose;
                if (pose == null) { diagnostic = "装着先のposeが未評価です。avatar graphの接続を確認してください。"; return null; }
                if (!pose.ByBoneId.TryGetValue(node.AttachmentBoneId, out var bonePose)) { diagnostic = "指定BoneIdがposeにありません。再bindが必要です。"; return null; }
                var transform = bonePose.Transform;
                var rootTranslation = transform.TransformPoint(node.AttachmentOffset);
                var resolved = new PoseTransform(transform.XAxis, transform.YAxis, transform.ZAxis, rootTranslation);
                key = node.NodeId + ":" + avatar.Graph.GraphId + ":" + pose.ContentHash + ":" + node.AttachmentOffset.X + ":" + node.AttachmentOffset.Y + ":" + node.AttachmentOffset.Z;
                return resolved;
            }
            catch (AuthoringException error) { diagnostic = error.Code + "：" + error.Message; return null; }
        }

        static void ChecksForAttachment(GraphNode node, ImportedBoneMap map)
        {
            ChecksRequire(node.AttachmentSkeletonHash == map.SkeletonHash, "ATTACHMENT_SKELETON_CHANGED", "装着時と異なるskeletonです。再bindしてから表示してください。");
            ChecksRequire(map.ByNode.Values.Contains(node.AttachmentBoneId), "ATTACHMENT_BONE_MISSING", "指定BoneIdはimported skeletonにありません。");
        }

        static void ChecksRequire(bool condition, string code, string message)
        {
            if (!condition) throw new AuthoringException(code, message);
        }

        PoseTransform? ResolveAttachmentPoseForObject(AuthoringObject item)
        {
            return ResolveAttachmentPose(item, out _, out _);
        }

        void RefreshAttachmentProjection()
        {
            if (projection == null || workspace == null || workspace.Document.IsEmpty) return;
            var resolved = ResolveAttachmentPose(workspace.Document.ActiveObject, out var diagnostic, out var key);
            if (resolved.HasValue)
            {
                projection.AttachmentPose = resolved;
                if (key != attachmentProjectionKey)
                {
                    attachmentProjectionKey = key;
                    using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview, resolved)) prepared.Commit();
                }
            }
            else if (attachmentProjectionKey != "")
            {
                attachmentProjectionKey = ""; projection.AttachmentPose = null;
                using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview, null)) prepared.Commit();
            }
            else projection.AttachmentPose = null;
            if (attachmentStatus != null && diagnostic != "") attachmentStatus.text = "装着: " + diagnostic;
        }

        string attachmentProjectionKey = "";

        void RefreshAttachmentControls()
        {
            if (attachmentPanel == null) return;
            attachmentTargetIds.Clear(); attachmentBoneIds.Clear();
            var node = ActiveAttachmentNode();
            string owner = workspace?.Document?.ActiveObjectId;
            if (owner != attachmentChoiceOwner) { attachmentChoiceOwner = owner; attachmentTargetChoice = null; }
            if (!IsGraph)
            {
                attachmentStatus.text = "装着: graph objectを選択してください。";
                attachmentApply.SetEnabled(false); attachmentRemove.SetEnabled(false); accessorySkinBind.SetEnabled(false); accessoryPoseCopy.SetEnabled(false); return;
            }
            var targets = workspace.Document.Objects.Where(item => item.ObjectId != workspace.Document.ActiveObjectId && item.Graph != null).ToArray();
            attachmentTargetIds.AddRange(targets.Select(item => item.ObjectId));
            var targetLabels = targets.Select(item => "graph · " + item.ObjectId.Substring(0, Math.Min(8, item.ObjectId.Length))).ToList();
            if (targetLabels.Count == 0) targetLabels.Add("対象なし");
            attachmentTarget.choices = targetLabels;
            string requestedTarget = string.IsNullOrEmpty(attachmentTargetChoice) ? node?.AttachmentTargetObjectId : attachmentTargetChoice;
            int targetIndex = requestedTarget == null ? -1 : attachmentTargetIds.IndexOf(requestedTarget);
            if (targetIndex < 0) targetIndex = 0;
            attachmentTarget.SetValueWithoutNotify(targetLabels[targetIndex]);
            var target = targetIndex < targets.Length ? targets[targetIndex] : null;
            var session = RigFor(target);
            var skeleton = session == null || target == null ? null : TryResolveSkeleton(session, target.Graph);
            if (skeleton != null)
            {
                attachmentBoneIds.AddRange(skeleton.Bones.Select(bone => bone.BoneId));
                attachmentBone.choices = skeleton.Bones.Select(bone => bone.Name + " · " + bone.BoneId.Substring(0, 8)).ToList();
            }
            else attachmentBone.choices = new List<string> { "BoneIdなし" };
            int boneIndex = node == null ? -1 : attachmentBoneIds.IndexOf(node.AttachmentBoneId);
            if (boneIndex < 0) boneIndex = 0;
            attachmentBone.SetValueWithoutNotify(attachmentBone.choices[boneIndex]);
            if (node != null)
            {
                attachmentOffsetX.SetValueWithoutNotify(node.AttachmentOffset.X * 1000f);
                attachmentOffsetY.SetValueWithoutNotify(node.AttachmentOffset.Y * 1000f);
                attachmentOffsetZ.SetValueWithoutNotify(node.AttachmentOffset.Z * 1000f);
            }
            attachmentApply.SetEnabled(target != null && skeleton != null && attachmentBoneIds.Count > 0);
            attachmentRemove.SetEnabled(node != null);
            bool canSkinBind = target != null && skeleton != null && workspace.Preview.IsComplete &&
                workspace.Document.ActiveObject.Graph.Nodes.Values.Count(item => item.TypeId == BuiltinNodes.EditMesh) == 1 &&
                workspace.Document.ActiveObject.Graph.Nodes.Values.All(item => item.TypeId != BuiltinNodes.Skeleton && item.TypeId != BuiltinNodes.SkinBind && item.TypeId != BuiltinNodes.SkinDeform && item.TypeId != BuiltinNodes.Pose && item.TypeId != BuiltinNodes.Attachment);
            accessorySkinBind.SetEnabled(canSkinBind);
            bool hasSkinPose = workspace.Document.ActiveObject.Graph.Nodes.Values.Any(item => item.TypeId == BuiltinNodes.SkinBind) &&
                workspace.Document.ActiveObject.Graph.Nodes.Values.Any(item => item.TypeId == BuiltinNodes.Pose);
            accessoryPoseCopy.SetEnabled(target != null && skeleton != null && hasSkinPose);
            if (node == null) attachmentStatus.text = "装着: 未設定。対象avatarとBoneIdを選んでください。";
            else if (diagnosticFor(node, target) != "") attachmentStatus.text = "装着: " + diagnosticFor(node, target);
            else attachmentStatus.text = "装着: " + node.AttachmentBoneId.Substring(0, 8) + "へ固定 · 保存対象";
        }

        static SkeletonDefinition TryResolveSkeleton(ImportedRigSession session, AuthoringGraph graph)
        {
            try { return session.Resolve(graph).ByNode.Count == 0 ? null : graph.Nodes[session.SkeletonNodeId].Skeleton; }
            catch (AuthoringException) { return null; }
        }

        string diagnosticFor(GraphNode node, AuthoringObject target)
        {
            if (target == null) return "装着先avatar objectが見つかりません。";
            var session = RigFor(target); if (session == null) return "装着先skeletonがありません。";
            try { ChecksForAttachment(node, session.Resolve(target.Graph)); return ""; }
            catch (AuthoringException error) { return error.Code + "：再bindが必要です。"; }
        }

        void ApplyAttachment()
        {
            if (!IsGraph) throw new InvalidOperationException("小物のgraph objectを選択してください。");
            int targetIndex = attachmentTarget.index;
            if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("装着先avatarを選択してください。");
            var target = FindObject(attachmentTargetIds[targetIndex]); var session = RigFor(target); var skeleton = session == null ? null : TryResolveSkeleton(session, target.Graph);
            int boneIndex = attachmentBone.index;
            if (skeleton == null || boneIndex < 0) throw new InvalidOperationException("装着先skeletonとBoneIdを選択してください。");
            var existing = ActiveAttachmentNode(); var node = GraphNode.AttachmentNode(existing?.NodeId ?? Guid.NewGuid().ToString("D"), target.ObjectId, attachmentBoneIds[boneIndex], session.SkeletonHash,
                new Vec3(attachmentOffsetX.value / 1000f, attachmentOffsetY.value / 1000f, attachmentOffsetZ.value / 1000f));
            attachmentTargetChoice = target.ObjectId;
            Execute(existing == null ? AuthoringOperation.AddNode(node) : AuthoringOperation.UpdateNode(node));
            SetStatus("小物をstable BoneIdへ装着しました。pose変更時にプレビューが追従します。");
        }

        void RemoveAttachment()
        {
            var nodes = IsGraph ? workspace.Document.ActiveObject.Graph.Nodes.Values.Where(node => node.TypeId == BuiltinNodes.Attachment).ToArray() : Array.Empty<GraphNode>();
            if (nodes.Length == 0) return;
            Execute(nodes.Select(node => AuthoringOperation.RemoveNode(node.NodeId)).ToArray());
            SetStatus("小物の装着を解除しました。");
        }

        void BindAccessoryToAvatar()
        {
            Try(() =>
            {
                if (!IsGraph) throw new InvalidOperationException("衣装のgraph objectを選択してください。");
                int targetIndex = attachmentTarget.index;
                if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("skin-bind先avatarを選択してください。");
                var target = FindObject(attachmentTargetIds[targetIndex]);
                var session = RigFor(target);
                var skeleton = session == null ? null : TryResolveSkeleton(session, target.Graph);
                if (skeleton == null) throw new InvalidOperationException("skin-bind先avatarのimported skeletonがありません。");
                var graph = workspace.Document.ActiveObject.Graph;
                var edit = graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.EditMesh);
                if (edit == null) throw new InvalidOperationException("衣装へEditMeshを先に用意してください。");
                if (!workspace.Preview.Evaluation.MeshOutputs.TryGetValue(edit.NodeId, out var editValue) || editValue?.Mesh == null)
                    throw new InvalidOperationException("衣装EditMeshの評価結果を取得できません。");
                var root = skeleton.Bones.FirstOrDefault(bone => string.IsNullOrEmpty(bone.ParentBoneId));
                if (root == null) throw new InvalidOperationException("avatar skeletonにRoot boneがありません。");
                var changed = AccessorySkinBindingAdapter.BindToSkeleton(graph, editValue.Mesh, skeleton, root.BoneId);
                Execute(AuthoringOperation.ReplaceGraph(changed));
                attachmentTargetChoice = target.ObjectId;
                SetStatus("衣装をavatar骨格へskin-bindしました。全頂点をRootへ初期化済みです。Rig panelでweight paintし、poseと保存後の出力を確認してください。");
            });
        }

        void CopyAvatarPose()
        {
            Try(() =>
            {
                if (!IsGraph) throw new InvalidOperationException("衣装のgraph objectを選択してください。");
                int targetIndex = attachmentTarget.index;
                if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("poseのコピー元avatarを選択してください。");
                var target = FindObject(attachmentTargetIds[targetIndex]);
                if (target == null || target.Graph == null) throw new InvalidOperationException("poseのコピー元avatarが見つかりません。");
                if (!TryResolvePose(out var poseNode, out var clothingSkeleton)) throw new InvalidOperationException("衣装のpose nodeへskeletonを接続してください。");
                var targetPose = target.EvaluateGraph().PoseOutputs.Values.Select(value => value.Pose)
                    .FirstOrDefault(pose => pose.SkeletonHash == clothingSkeleton.ContentHash);
                if (targetPose == null) throw new InvalidOperationException("avatarのposeが衣装と同じskeletonではありません。");
                attachmentTargetChoice = target.ObjectId;
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(poseNode.NodeId, PoseEditing.Rebind(targetPose, clothingSkeleton))));
                RefreshAttachmentControls();
                SetStatus("avatarの現在poseを衣装へコピーしました。必要ならweightを調整して保存してください。");
            });
        }
    }
}
