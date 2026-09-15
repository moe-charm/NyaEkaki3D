using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Geometry;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
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
                RefreshAccessoryFitSummary();
                attachmentApply.SetEnabled(false); attachmentRemove.SetEnabled(false); accessorySkinBind.SetEnabled(false); accessoryPolygonMaterialize.SetEnabled(false); accessoryAutoWeight.SetEnabled(false); accessorySurfaceWeight.SetEnabled(false); accessorySurfaceFit.SetEnabled(false); accessorySurfaceInspect.SetEnabled(false); accessoryPoseCopy.SetEnabled(false); accessoryUseSelectedVertices.SetEnabled(false); accessorySurfacePickMode.SetEnabled(false); accessoryClearSurfaceSelection.SetEnabled(false); accessorySelectBoneRegion.SetEnabled(false); return;
            }
            var targets = workspace.Document.Objects.Where(item => item.ObjectId != workspace.Document.ActiveObjectId && item.Graph != null).ToArray();
            attachmentTargetIds.AddRange(targets.Select(item => item.ObjectId));
            var targetLabels = targets.Select(AttachmentTargetLabel).ToList();
            if (targetLabels.Count == 0) targetLabels.Add("対象なし");
            attachmentTarget.choices = targetLabels;
            var poseSource = workspace.Document.ActiveObject.Graph.Nodes.Values.FirstOrDefault(item => item.TypeId == BuiltinNodes.PoseSource);
            string requestedTarget = string.IsNullOrEmpty(attachmentTargetChoice) ? (node?.AttachmentTargetObjectId ?? poseSource?.PoseSourceObjectId) : attachmentTargetChoice;
            int targetIndex = requestedTarget == null ? -1 : attachmentTargetIds.IndexOf(requestedTarget);
            if (targetIndex < 0) targetIndex = 0;
            attachmentTarget.SetValueWithoutNotify(targetLabels[targetIndex]);
            var target = targetIndex < targets.Length ? targets[targetIndex] : null;
            attachmentTarget.tooltip = target == null
                ? "装着先avatarを選択してください。対象が複数ある場合は表示名と役割で照合できます。"
                : ObjectDisplayDetails(target) + "\n内部ID: " + target.ObjectId;
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
            if (skeleton != null && boneIndex >= 0 && boneIndex < skeleton.Bones.Count)
            {
                var selectedBone = skeleton.Bones[boneIndex];
                attachmentBone.tooltip = selectedBone.Name + "\nstable BoneId: " + selectedBone.BoneId;
            }
            else attachmentBone.tooltip = "装着先avatarの骨格を読み込むと、stable BoneIdを選択できます。";
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
            accessoryPolygonMaterialize.SetEnabled(target != null && skeleton != null && CanMaterializePolygonAccessory(workspace.Document.ActiveObject.Graph));
            bool canAutoWeight = target != null && skeleton != null && workspace.Preview.IsComplete &&
                workspace.Document.ActiveObject.Graph.Nodes.Values.Any(item => item.TypeId == BuiltinNodes.EditMesh) &&
                workspace.Document.ActiveObject.Graph.Nodes.Values.Any(item => item.TypeId == BuiltinNodes.SkinBind && item.Binding != null);
            accessoryAutoWeight.SetEnabled(canAutoWeight);
            bool canSurfaceWeight = canAutoWeight && target != null && target.Graph != null && TargetAvatarSurfaceAvailable(target);
            accessorySurfaceWeight.SetEnabled(canSurfaceWeight);
            bool canSurfaceFit = target != null && target.Graph != null && skeleton != null && workspace.Preview.IsComplete &&
                workspace.Document.ActiveObject.Graph.Nodes.Values.Any(item => item.TypeId == BuiltinNodes.EditMesh) &&
                TargetAvatarSurfaceAvailable(target);
            accessorySurfaceFit.SetEnabled(canSurfaceFit);
            accessorySurfaceInspect.SetEnabled(canSurfaceFit);
            accessorySurfacePickMode.SetEnabled(canSurfaceFit);
            accessoryClearSurfaceSelection.SetEnabled(canSurfaceFit && !string.IsNullOrWhiteSpace(accessorySurfaceTriangleIds.value));
            accessorySelectBoneRegion.SetEnabled(canSurfaceFit && skeleton != null && attachmentBoneIds.Count > 0);
            bool hasSkinPose = workspace.Document.ActiveObject.Graph.Nodes.Values.Any(item => item.TypeId == BuiltinNodes.SkinBind) &&
                workspace.Document.ActiveObject.Graph.Nodes.Values.Any(item => item.TypeId == BuiltinNodes.Pose);
            accessoryPoseCopy.SetEnabled(target != null && skeleton != null && hasSkinPose);
            var editNode = workspace.Document.ActiveObject.Graph.Nodes.Values.FirstOrDefault(item => item.TypeId == BuiltinNodes.EditMesh);
            accessoryUseSelectedVertices.SetEnabled(editNode != null && workspace.Preview.IsComplete && selection.Count > 0);
            if (node == null) attachmentStatus.text = "装着: 未設定。対象avatarとBoneIdを選んでください。";
            else if (diagnosticFor(node, target) != "") attachmentStatus.text = "装着: " + diagnosticFor(node, target);
            else
            {
                var selected = skeleton?.Bones.FirstOrDefault(bone => bone.BoneId == node.AttachmentBoneId);
                string boneName = selected == null ? "BoneId " + node.AttachmentBoneId.Substring(0, 8) : selected.Name;
                attachmentStatus.text = "装着: " + boneName + "へ固定 · 保存対象";
            }
            RefreshAccessoryFitSummary();
        }

        string AttachmentTargetLabel(AuthoringObject item)
        {
            if (item == null) return "対象なし";
            string id = item.ObjectId ?? "";
            string shortId = id.Length > 8 ? id.Substring(0, 8) : id;
            string role = item.IsStaticProfile ? "static" : "avatar graph";
            return ObjectDisplayName(item) + " · " + role + " · " + shortId;
        }

        void RefreshAccessoryFitSummary()
        {
            if (accessoryFitSummary == null) return;
            if (workspace == null || workspace.Document.IsEmpty || !IsGraph)
            {
                ClearAccessoryFitSummaryEvaluation();
                accessoryFitSummary.text = "fit対象: 衣装graphを選択してください。";
                accessoryFitSummary.tooltip = accessoryFitSummary.text;
                return;
            }

            var graph = workspace.Document.ActiveObject.Graph;
            var edit = graph?.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.EditMesh);
            int clothingCount = 0;
            try
            {
                if (edit != null && workspace.Preview.Evaluation.MeshOutputs.TryGetValue(edit.NodeId, out var editValue) && editValue?.Mesh != null)
                    clothingCount = editValue.Mesh.VertexCount;
            }
            catch (Exception) { }

            string clothingScope = "全頂点";
            try
            {
                var selected = ClothingVertexSelection()?.ToArray();
                if (selected != null) clothingScope = "指定 " + selected.Length.ToString(CultureInfo.InvariantCulture) + "頂点";
            }
            catch (Exception error) { clothingScope = "頂点ID入力エラー（" + error.Message + "）"; }

            string targetId = null;
            if (attachmentTarget != null && attachmentTarget.index >= 0 && attachmentTarget.index < attachmentTargetIds.Count)
                targetId = attachmentTargetIds[attachmentTarget.index];
            var attachment = ActiveAttachmentNode();
            if (string.IsNullOrEmpty(targetId)) targetId = attachment?.AttachmentTargetObjectId;
            var target = FindObject(targetId);
            int avatarTriangleCount = 0;
            bool surfaceReady = false;
            try
            {
                if (target?.Graph != null)
                {
                    // Selection refreshes call this summary frequently. Graphs
                    // are immutable, so reuse the last evaluation while the
                    // selected target still points at the same graph instance.
                    var evaluation = EvaluateAttachmentTarget(target);
                    var bind = target.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null);
                    if (bind != null && evaluation.MeshInputs.TryGetValue(bind.NodeId, out var meshValue) && meshValue?.Mesh != null)
                    {
                        avatarTriangleCount = meshValue.Mesh.TriangleCount;
                        surfaceReady = evaluation.SkinBindingOutputs.TryGetValue(bind.NodeId, out var binding) && binding?.Binding != null;
                    }
                }
            }
            catch (Exception) { }

            if (target == null || target.Graph == null)
                ClearAccessoryFitSummaryEvaluation();

            string avatarScope = "全三角形";
            try
            {
                var selected = SurfaceTriangleSelection()?.ToArray();
                if (selected != null) avatarScope = "指定 " + selected.Length.ToString(CultureInfo.InvariantCulture) + "面";
            }
            catch (Exception error) { avatarScope = "面ID入力エラー（" + error.Message + "）"; }

            bool hasSkinBind = graph?.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null) == true;
            var state = SurfaceFitInspectionState();
            bool measured = state["available"]?.Value<bool>() == true;
            string measurement = measured ? "計測済み" : (string.IsNullOrEmpty(surfaceFitInspectionStateHash) ? "計測未実施" : "再計測が必要（対象・範囲・形状が変更）");
            string targetText = target == null ? "avatar未選択" : "avatar " + target.ObjectId.Substring(0, Math.Min(8, target.ObjectId.Length));
            string bindText = hasSkinBind ? "skin-bind済み" : "skin-bind前";
            string fullSummary = "fit対象: 衣装 " + clothingScope + "（全 " + clothingCount.ToString(CultureInfo.InvariantCulture) + "頂点） · " +
                targetText + " " + avatarScope + "（全 " + avatarTriangleCount.ToString(CultureInfo.InvariantCulture) + "面） · " + bindText + " · " + measurement;
            if (!surfaceReady && target != null) fullSummary += " · avatar表面rest mesh／weightを確認";
            // Keep the actionable state visible without clipping the control
            // column. Detailed identity/counts remain available on hover.
            accessoryFitSummary.text = "fit対象: " + clothingScope + "・" + avatarScope + "・" + measurement;
            accessoryFitSummary.tooltip = fullSummary;
        }

        void ClearAccessoryFitSummaryEvaluation()
        {
            accessoryFitSummaryTargetGraph = null;
            accessoryFitSummaryTargetEvaluation = null;
            accessoryFitSummaryTargetObjectId = "";
        }

        GraphEvaluation EvaluateAttachmentTarget(AuthoringObject target)
        {
            if (target?.Graph == null) return null;
            if (target.ObjectId != accessoryFitSummaryTargetObjectId ||
                !ReferenceEquals(target.Graph, accessoryFitSummaryTargetGraph))
            {
                accessoryFitSummaryTargetObjectId = target.ObjectId;
                accessoryFitSummaryTargetGraph = target.Graph;
                accessoryFitSummaryTargetEvaluation = target.EvaluateGraph();
            }
            return accessoryFitSummaryTargetEvaluation;
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
                var changed = AccessorySkinBindingAdapter.BindToSkeleton(graph, editValue.Mesh, skeleton, root.BoneId, target.ObjectId);
                Execute(AuthoringOperation.ReplaceGraph(changed));
                attachmentTargetChoice = target.ObjectId;
                SetStatus("衣装をavatar骨格へskin-bindしました。全頂点をRootへ初期化済みです。Rig panelでweight paintし、poseと保存後の出力を確認してください。");
            });
        }

        bool CanMaterializePolygonAccessory(AuthoringGraph graph)
        {
            if (graph == null) return false;
            var nodes = graph.Nodes.Values.ToArray();
            if (nodes.Count(node => node.TypeId == BuiltinNodes.PolygonSource) != 1 ||
                nodes.Count(node => node.TypeId == BuiltinNodes.PolygonEdit) != 1 ||
                nodes.Count(node => node.TypeId == BuiltinNodes.Attachment) > 1 ||
                nodes.Any(node => node.TypeId == BuiltinNodes.Skeleton || node.TypeId == BuiltinNodes.SkinBind ||
                    node.TypeId == BuiltinNodes.SkinDeform || node.TypeId == BuiltinNodes.Pose ||
                    node.TypeId == BuiltinNodes.LayeredPaint || node.TypeId == BuiltinNodes.Mirror)) return false;
            try { return GraphEvaluator.Evaluate(graph).IsComplete; }
            catch (AuthoringException) { return false; }
        }

        void MaterializePolygonAccessory()
        {
            Try(() =>
            {
                if (!IsGraph) throw new InvalidOperationException("Polygon衣装のgraph objectを選択してください。");
                int targetIndex = attachmentTarget.index;
                if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("派生先avatarを選択してください。");
                var target = FindObject(attachmentTargetIds[targetIndex]);
                var session = RigFor(target);
                var skeleton = session == null || target == null ? null : TryResolveSkeleton(session, target.Graph);
                if (skeleton == null) throw new InvalidOperationException("派生先avatarのimported skeletonがありません。");
                var graph = workspace.Document.ActiveObject.Graph;
                if (!CanMaterializePolygonAccessory(graph)) throw new InvalidOperationException("PolygonSource→PolygonEditとappearanceだけのgraphを選択してください。");
                var root = skeleton.Bones.FirstOrDefault(bone => string.IsNullOrEmpty(bone.ParentBoneId));
                if (root == null) throw new InvalidOperationException("avatar skeletonにRoot boneがありません。");
                var attachment = graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.Attachment);
                PoseTransform? bakeAttachment = null;
                if (attachment != null)
                {
                    if (attachment.AttachmentTargetObjectId != target.ObjectId)
                        throw new InvalidOperationException("Polygon小物の装着先avatarと派生先が一致していません。");
                    ChecksForAttachment(attachment, session.Resolve(target.Graph));
                    // Attachment offsets are authored in the current avatar
                    // skeleton's rest bone-local frame. The authored rest pose
                    // uses identity axes around BoneDefinition.Head, so baking
                    // the frame keeps the visible rigid placement when the
                    // derived graph switches to root-initialized skin weights.
                    var bone = skeleton.ById[attachment.AttachmentBoneId];
                    var restFrame = PoseTransform.FromTranslation(bone.Head);
                    bakeAttachment = new PoseTransform(restFrame.XAxis, restFrame.YAxis, restFrame.ZAxis,
                        restFrame.TransformPoint(attachment.AttachmentOffset));
                }
                var result = AccessorySkinMaterializer.Materialize(graph, skeleton, root.BoneId, target.ObjectId,
                    bakeAttachmentTransform: bakeAttachment);
                Execute(AuthoringOperation.AddGraph(result.Graph));
                attachmentTargetChoice = target.ObjectId;
                SetStatus(attachment == null
                    ? "Polygon造形をskin衣装へ派生しました。元graphは保持されています。新しい衣装objectでweight・fit・poseを確認して保存してください。"
                    : "装着位置をavatar rest座標へ焼き込み、Polygon造形をskin衣装へ派生しました。元graphは保持されています。新しい衣装objectでweight・fit・poseを確認して保存してください。");
            });
        }

        bool TargetAvatarSurfaceAvailable(AuthoringObject target)
        {
            try
            {
                var evaluation = EvaluateAttachmentTarget(target);
                var bind = target.Graph.Nodes.Values.SingleOrDefault(item => item.TypeId == BuiltinNodes.SkinBind && item.Binding != null);
                return bind != null && evaluation.MeshInputs.TryGetValue(bind.NodeId, out var mesh) && mesh != null && mesh.Mesh != null &&
                    evaluation.SkinBindingOutputs.TryGetValue(bind.NodeId, out var binding) && binding != null && binding.Binding != null;
            }
            catch (AuthoringException) { return false; }
        }

        void UseSelectedClothingVertices()
        {
            Try(() =>
            {
                if (!IsGraph) throw new InvalidOperationException("衣装graph objectを選択してください。");
                var edit = workspace.Document.ActiveObject.Graph.Nodes.Values.FirstOrDefault(item => item.TypeId == BuiltinNodes.EditMesh);
                if (edit == null) throw new InvalidOperationException("衣装へEditMeshを先に用意してください。");
                if (!workspace.Preview.Evaluation.MeshOutputs.TryGetValue(edit.NodeId, out var value) || value?.Mesh == null)
                    throw new InvalidOperationException("衣装EditMeshの評価結果を取得できません。");
                var indices = selection.OrderBy(index => index).ToArray();
                if (indices.Any(index => index < 0 || index >= value.Mesh.VertexCount))
                    throw new InvalidOperationException("現在の選択にEditMeshの頂点範囲外が含まれています。EditMeshを表示して選択してください。");
                accessoryClothingVertexIds.SetValueWithoutNotify(string.Join(",", indices));
                RefreshAccessoryFitSummary();
                SetStatus("現在の衣装頂点選択をfit／weight対象へ設定しました（" + indices.Length.ToString(CultureInfo.InvariantCulture) + "頂点）。");
            });
        }

        void RefreshAvatarSurfaceSelection()
        {
            if (avatarSurfaceSelection == null) return;
            try
            {
                var node = ActiveAttachmentNode();
                string targetId = string.IsNullOrEmpty(attachmentTargetChoice) ? node?.AttachmentTargetObjectId : attachmentTargetChoice;
                if (string.IsNullOrEmpty(targetId) && IsGraph)
                    targetId = workspace.Document.ActiveObject.Graph.Nodes.Values.FirstOrDefault(item => item.TypeId == BuiltinNodes.PoseSource)?.PoseSourceObjectId;
                var target = FindObject(targetId);
                var value = EvaluateAttachmentTarget(target)?.Output;
                var selected = SurfaceTriangleSelection();
                avatarSurfaceSelection.Refresh(value?.Mesh, value?.Transform ?? new RestTransform(1, new Vec3()), selected);
            }
            catch (Exception)
            {
                avatarSurfaceSelection.Refresh(null, new RestTransform(1, new Vec3()), null);
            }
        }

        void ClearSurfaceTriangleSelection()
        {
            selectedAvatarSurfaceTriangles.Clear();
            accessorySurfaceTriangleIds.SetValueWithoutNotify("");
            RefreshAvatarSurfaceSelection();
            RefreshAccessoryFitSummary();
            SetStatus("avatar面領域を解除しました。fit／weightは全三角形を対象にします。");
        }

        void SelectBoneSurfaceRegion()
        {
            Try(() =>
            {
                int targetIndex = attachmentTarget.index;
                if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count)
                    throw new InvalidOperationException("面を選ぶavatarを指定してください。");
                int boneIndex = attachmentBone.index;
                if (boneIndex < 0 || boneIndex >= attachmentBoneIds.Count)
                    throw new InvalidOperationException("面領域の基準にするBoneIdを選んでください。");
                if (accessorySurfaceRegionRadiusMm == null || accessorySurfaceRegionRadiusMm.value <= 0f)
                    throw new InvalidOperationException("Bone近傍面の選択半径は0より大きくしてください。");
                var avatar = FindObject(attachmentTargetIds[targetIndex]);
                var session = RigFor(avatar);
                var skeleton = session == null || avatar == null ? null : TryResolveSkeleton(session, avatar.Graph);
                if (skeleton == null || boneIndex >= skeleton.Bones.Count)
                    throw new InvalidOperationException("選択avatarのskeletonを取得できません。");
                var bind = avatar.Graph.Nodes.Values.SingleOrDefault(item => item.TypeId == BuiltinNodes.SkinBind && item.Binding != null);
                if (bind == null) throw new InvalidOperationException("avatarへ有効なSkinBindがありません。");
                var evaluation = EvaluateAttachmentTarget(avatar);
                if (!evaluation.MeshInputs.TryGetValue(bind.NodeId, out var meshValue) || meshValue?.Mesh == null)
                    throw new InvalidOperationException("avatarのrest mesh評価結果を取得できません。");
                var bone = skeleton.Bones[boneIndex];
                var triangleIds = MeshSurfaceRegion.SelectTrianglesNearBone(meshValue.Mesh, meshValue.Transform,
                    bone.Head, bone.Tail, accessorySurfaceRegionRadiusMm.value / 1000f);
                if (triangleIds.Length == 0)
                    throw new InvalidOperationException("指定半径内にavatar面が見つかりません。半径を少し広げてください。");
                selectedAvatarSurfaceTriangles.Clear();
                foreach (var id in triangleIds) selectedAvatarSurfaceTriangles.Add(id);
                accessorySurfaceTriangleIds.SetValueWithoutNotify(string.Join(",", triangleIds));
                RefreshAvatarSurfaceSelection();
                RefreshAccessoryFitSummary();
                SetStatus(bone.Name + "近傍のavatar面を自動選択しました（" + triangleIds.Length.ToString(CultureInfo.InvariantCulture) + "面、半径 " +
                    accessorySurfaceRegionRadiusMm.value.ToString("0.###", CultureInfo.InvariantCulture) + " mm）。fit／weightへ共通適用されます。");
            });
        }

        bool SurfaceTrianglePickingActive => accessorySurfacePickMode != null && accessorySurfacePickMode.value && accessorySurfacePickMode.enabledSelf;

        void PickAvatarSurfaceTriangle(Vector2 panelPosition, bool add)
        {
            Try(() =>
            {
                if (!SurfaceTrianglePickingActive) return;
                int targetIndex = attachmentTarget.index;
                if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("面を選ぶavatarを指定してください。");
                var avatar = FindObject(attachmentTargetIds[targetIndex]);
                var evaluation = EvaluateAttachmentTarget(avatar);
                var value = evaluation?.Output;
                if (value?.Mesh == null) throw new InvalidOperationException("avatarのrest mesh評価結果を取得できません。");
                var rect = view.worldBound;
                var ray = camera.ViewportPointToRay(new Vector3((panelPosition.x - rect.x) / rect.width,
                    1 - (panelPosition.y - rect.y) / rect.height, 0));
                var points = value.Mesh.Positions.Select(point => stage.transform.TransformPoint(
                    OwnedMeshProjection.ToUnity(value.Transform.ToAvatarPoint(point)))).ToArray();
                float nearest = float.PositiveInfinity; int hit = -1, triangle = 0;
                foreach (var submesh in value.Mesh.Submeshes)
                    for (int i = 0; i < submesh.Length; i += 3, triangle++)
                    {
                        var a = points[submesh[i]]; var b = points[submesh[i + 1]]; var c = points[submesh[i + 2]];
                        var e1 = b - a; var e2 = c - a; var p = Vector3.Cross(ray.direction, e2); float determinant = Vector3.Dot(e1, p);
                        if (Mathf.Abs(determinant) < 1e-10f) continue;
                        float inverse = 1 / determinant; var t = ray.origin - a; float u = Vector3.Dot(t, p) * inverse;
                        if (u < 0 || u > 1) continue; var q = Vector3.Cross(t, e1); float v = Vector3.Dot(ray.direction, q) * inverse;
                        if (v < 0 || u + v > 1) continue; float distance = Vector3.Dot(e2, q) * inverse;
                        if (distance >= 0 && distance < nearest) { nearest = distance; hit = triangle; }
                    }
                var current = SurfaceTriangleSelection();
                selectedAvatarSurfaceTriangles.Clear();
                if (current != null) foreach (int id in current) selectedAvatarSurfaceTriangles.Add(id);
                if (!add) selectedAvatarSurfaceTriangles.Clear();
                if (hit >= 0 && (!add || !selectedAvatarSurfaceTriangles.Remove(hit))) selectedAvatarSurfaceTriangles.Add(hit);
                accessorySurfaceTriangleIds.SetValueWithoutNotify(string.Join(",", selectedAvatarSurfaceTriangles.OrderBy(id => id)));
                RefreshAvatarSurfaceSelection();
                RefreshAccessoryFitSummary();
                SetStatus(selectedAvatarSurfaceTriangles.Count == 0 ? "avatar面の選択を解除しました。" :
                    "avatar面領域を更新しました（" + selectedAvatarSurfaceTriangles.Count.ToString(CultureInfo.InvariantCulture) + "面）。fit／weightへ共通適用されます。");
            });
        }

        IEnumerable<int> ParseIndexSelection(string text, string label)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            var selected = new SortedSet<int>();
            foreach (string token in text.Split(new[] { ',', ';', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(token, NumberStyles.Integer, CultureInfo.InvariantCulture, out int triangle) || triangle < 0)
                    throw new InvalidOperationException(label + "は0以上の整数をカンマ区切りで指定してください。");
                selected.Add(triangle);
            }
            if (selected.Count == 0) throw new InvalidOperationException(label + "を1つ以上指定してください。");
            return selected.ToArray();
        }

        IEnumerable<int> SurfaceTriangleSelection() => ParseIndexSelection(
            accessorySurfaceTriangleIds == null ? "" : accessorySurfaceTriangleIds.value, "avatar面ID");

        IEnumerable<int> ClothingVertexSelection() => ParseIndexSelection(
            accessoryClothingVertexIds == null ? "" : accessoryClothingVertexIds.value, "衣装頂点ID");

        AttachmentSurfaceFitMeasurement MeasureAccessorySurfaceFit()
        {
            if (!IsGraph) throw new InvalidOperationException("衣装のgraph objectを選択してください。");
            int targetIndex = attachmentTarget.index;
            if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("fit検査対象avatarを選択してください。");
            var target = FindObject(attachmentTargetIds[targetIndex]);
            if (target == null || target.Graph == null) throw new InvalidOperationException("fit検査対象avatarが見つかりません。");
            var avatarBind = target.Graph.Nodes.Values.SingleOrDefault(item => item.TypeId == BuiltinNodes.SkinBind && item.Binding != null);
            if (avatarBind == null) throw new InvalidOperationException("avatarへ有効なSkinBindがありません。");
            var avatarEvaluation = EvaluateAttachmentTarget(target);
            if (!avatarEvaluation.MeshInputs.TryGetValue(avatarBind.NodeId, out var avatarMeshValue) || avatarMeshValue?.Mesh == null)
                throw new InvalidOperationException("avatarのrest mesh評価結果を取得できません。");
            var graph = workspace.Document.ActiveObject.Graph;
            var edit = graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.EditMesh);
            if (edit == null) throw new InvalidOperationException("衣装へEditMeshを先に用意してください。");
            var evaluation = workspace.Preview.Evaluation;
            if (!evaluation.MeshOutputs.TryGetValue(edit.NodeId, out var editValue) || editValue?.Mesh == null)
                throw new InvalidOperationException("衣装EditMeshの評価結果を取得できません。");
            float offset = accessoryFitOffsetMm.value / 1000f;
            float maxDistance = accessoryFitMaxDistanceMm.value / 1000f;
            var surfaceTriangles = SurfaceTriangleSelection()?.ToArray();
            var clothingVertices = ClothingVertexSelection()?.ToArray();
            var fit = MeshSurfaceFit.Project(editValue.Mesh, editValue.Transform,
                avatarMeshValue.Mesh, avatarMeshValue.Transform, offset, maxDistance, clothingVertices, surfaceTriangles);
            var clearance = MeshSurfaceClearance.Inspect(editValue.Mesh, editValue.Transform,
                avatarMeshValue.Mesh, avatarMeshValue.Transform, clothingVertices, surfaceTriangles);
            string region = surfaceTriangles == null ? "全三角形" : surfaceTriangles.Length.ToString(CultureInfo.InvariantCulture) + "面領域";
            string vertices = clothingVertices == null ? "全頂点" : clothingVertices.Length.ToString(CultureInfo.InvariantCulture) + "頂点";
            surfaceFitInspectionObjectId = workspace.Document.ActiveObjectId;
            surfaceFitInspectionTargetObjectId = target.ObjectId;
            surfaceFitInspectionStateHash = workspace.Document.StateHash;
            surfaceFitInspectionRevision = workspace.Document.DocumentRevision;
            surfaceFitInspectionEvaluatedVertexCount = fit.EvaluatedVertexCount;
            surfaceFitInspectionMovedVertexCount = fit.MovedVertexCount;
            surfaceFitInspectionMaxProjectionDistance = fit.MaxProjectionDistance;
            surfaceFitInspectionAverageProjectionDistance = fit.AverageProjectionDistance;
            surfaceFitInspectionMaxDisplacement = fit.MaxDisplacement;
            surfaceFitInspectionAverageDisplacement = fit.AverageDisplacement;
            surfaceFitInspectionOffset = offset;
            surfaceFitInspectionMaxDistance = maxDistance;
            surfaceFitInspectionBehindSurfaceVertexCount = clearance.BehindSurfaceVertexCount;
            surfaceFitInspectionMinimumSignedDistance = clearance.MinimumSignedDistance;
            surfaceFitInspectionMaximumSignedDistance = clearance.MaximumSignedDistance;
            surfaceFitInspectionTriangleIds = surfaceTriangles;
            surfaceFitInspectionVertexIds = clothingVertices;
            surfaceFitInspectionBehindSurfaceVertexIds = clearance.BehindSurfaceVertexIndices.ToArray();
            RefreshAccessoryFitSummary();
            return new AttachmentSurfaceFitMeasurement { Result = fit, Clearance = clearance, TargetObjectId = target.ObjectId, Region = region, Vertices = vertices,
                TriangleIds = surfaceTriangles, VertexIds = clothingVertices, Offset = offset, MaxDistance = maxDistance };
        }

        void InspectAccessorySurfaceFit()
        {
            Try(() =>
            {
                var measurement = MeasureAccessorySurfaceFit();
                var fit = measurement.Result;
                SetStatus("fit状態を測定しました（変更なし、" + measurement.Region + "、" + measurement.Vertices + "、評価 " + fit.EvaluatedVertexCount + "頂点、" +
                    fit.MovedVertexCount + "頂点が移動する候補、最大投影距離 " + (fit.MaxProjectionDistance * 1000f).ToString("0.###") +
                    " mm、平均 " + (fit.AverageProjectionDistance * 1000f).ToString("0.###") + " mm、最大移動量 " +
                    (fit.MaxDisplacement * 1000f).ToString("0.###") + " mm、裏側候補 " + measurement.Clearance.BehindSurfaceVertexCount + "頂点）。" +
                    "これは最近面の法線による候補値で、貫通ゼロの証明ではありません。見た目はposeで確認してください。" );
            });
        }

        internal JObject SurfaceFitInspectionForMcp()
        {
            var measurement = MeasureAccessorySurfaceFit();
            var state = SurfaceFitInspectionState();
            state["measuredTargetObjectId"] = measurement.TargetObjectId;
            return state;
        }

        bool SurfaceFitInspectionInputsMatchMeasurement()
        {
            try
            {
                if (attachmentTarget == null || attachmentTarget.index < 0 || attachmentTarget.index >= attachmentTargetIds.Count)
                    return false;
                string currentTarget = attachmentTargetIds[attachmentTarget.index];
                if (currentTarget != surfaceFitInspectionTargetObjectId) return false;
                var currentTriangles = SurfaceTriangleSelection()?.ToArray();
                var currentVertices = ClothingVertexSelection()?.ToArray();
                if (!(surfaceFitInspectionTriangleIds ?? Array.Empty<int>()).SequenceEqual(currentTriangles ?? Array.Empty<int>())) return false;
                if (!(surfaceFitInspectionVertexIds ?? Array.Empty<int>()).SequenceEqual(currentVertices ?? Array.Empty<int>())) return false;
                return accessoryFitOffsetMm != null && accessoryFitMaxDistanceMm != null &&
                    Math.Abs(accessoryFitOffsetMm.value / 1000f - surfaceFitInspectionOffset) < 1e-6f &&
                    Math.Abs(accessoryFitMaxDistanceMm.value / 1000f - surfaceFitInspectionMaxDistance) < 1e-6f;
            }
            catch (Exception) { return false; }
        }

        internal JObject SurfaceFitInspectionState()
        {
            bool available = workspace != null && !workspace.Document.IsEmpty &&
                workspace.Document.ActiveObjectId == surfaceFitInspectionObjectId &&
                workspace.Document.DocumentRevision == surfaceFitInspectionRevision &&
                workspace.Document.StateHash == surfaceFitInspectionStateHash &&
                SurfaceFitInspectionInputsMatchMeasurement();
            var result = new JObject { ["available"] = available };
            if (!available)
            {
                result["reason"] = string.IsNullOrEmpty(surfaceFitInspectionStateHash) ? "not_measured" : "stale_after_document_change";
                return result;
            }
            result["objectId"] = surfaceFitInspectionObjectId;
            result["targetObjectId"] = surfaceFitInspectionTargetObjectId;
            result["revision"] = surfaceFitInspectionRevision;
            result["stateHash"] = surfaceFitInspectionStateHash;
            result["evaluatedVertexCount"] = surfaceFitInspectionEvaluatedVertexCount;
            result["movedVertexCount"] = surfaceFitInspectionMovedVertexCount;
            result["maxProjectionDistanceMetres"] = surfaceFitInspectionMaxProjectionDistance;
            result["averageProjectionDistanceMetres"] = surfaceFitInspectionAverageProjectionDistance;
            result["maxDisplacementMetres"] = surfaceFitInspectionMaxDisplacement;
            result["averageDisplacementMetres"] = surfaceFitInspectionAverageDisplacement;
            result["offsetMetres"] = surfaceFitInspectionOffset;
            result["maxDistanceMetres"] = surfaceFitInspectionMaxDistance;
            result["behindSurfaceVertexCount"] = surfaceFitInspectionBehindSurfaceVertexCount;
            result["minimumSignedDistanceMetres"] = surfaceFitInspectionMinimumSignedDistance;
            result["maximumSignedDistanceMetres"] = surfaceFitInspectionMaximumSignedDistance;
            result["avatarTriangleIds"] = surfaceFitInspectionTriangleIds == null ? JValue.CreateNull() : new JArray(surfaceFitInspectionTriangleIds);
            result["clothingVertexIds"] = surfaceFitInspectionVertexIds == null ? JValue.CreateNull() : new JArray(surfaceFitInspectionVertexIds);
            result["behindSurfaceVertexIds"] = surfaceFitInspectionBehindSurfaceVertexIds == null ? JValue.CreateNull() : new JArray(surfaceFitInspectionBehindSurfaceVertexIds);
            return result;
        }

        void FitAccessoryToAvatarSurface()
        {
            Try(() =>
            {
                if (!IsGraph) throw new InvalidOperationException("衣装のgraph objectを選択してください。");
                int targetIndex = attachmentTarget.index;
                if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("fit対象avatarを選択してください。");
                var target = FindObject(attachmentTargetIds[targetIndex]);
                var session = RigFor(target);
                var skeleton = session == null || target == null ? null : TryResolveSkeleton(session, target.Graph);
                if (skeleton == null) throw new InvalidOperationException("fit対象avatarのskeletonがありません。");
                var avatarBind = target.Graph.Nodes.Values.SingleOrDefault(item => item.TypeId == BuiltinNodes.SkinBind && item.Binding != null);
                if (avatarBind == null) throw new InvalidOperationException("avatarへ有効なSkinBindがありません。");
                var avatarEvaluation = EvaluateAttachmentTarget(target);
                if (!avatarEvaluation.MeshInputs.TryGetValue(avatarBind.NodeId, out var avatarMeshValue) || avatarMeshValue?.Mesh == null)
                    throw new InvalidOperationException("avatarのrest mesh評価結果を取得できません。");
                var graph = workspace.Document.ActiveObject.Graph;
                var edit = graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.EditMesh);
                if (edit == null) throw new InvalidOperationException("衣装へEditMeshを先に用意してください。");
                var evaluation = workspace.Preview.Evaluation;
                if (!evaluation.MeshInputs.TryGetValue(edit.NodeId, out var editInput) || editInput?.Mesh == null ||
                    !evaluation.MeshOutputs.TryGetValue(edit.NodeId, out var editValue) || editValue?.Mesh == null)
                    throw new InvalidOperationException("衣装EditMeshの入力・評価結果を取得できません。");
                float offset = accessoryFitOffsetMm.value / 1000f;
                float maxDistance = accessoryFitMaxDistanceMm.value / 1000f;
                var surfaceTriangles = SurfaceTriangleSelection();
                var clothingVertices = ClothingVertexSelection();
                var fit = MeshSurfaceFit.Project(editValue.Mesh, editValue.Transform,
                    avatarMeshValue.Mesh, avatarMeshValue.Transform, offset, maxDistance, clothingVertices, surfaceTriangles);
                var fitted = fit.Positions;
                var offsets = new Dictionary<int, Vec3>();
                for (int vertex = 0; vertex < fitted.Length; vertex++)
                {
                    Vec3 before = editInput.Transform.ToAvatarPoint(editInput.Mesh.Positions[vertex]);
                    Vec3 after = editInput.Transform.ToAvatarPoint(fitted[vertex]);
                    Vec3 delta = after - before;
                    if (delta.X != 0f || delta.Y != 0f || delta.Z != 0f) offsets[vertex] = delta;
                }
                var changed = graph.ReplaceNode(GraphNode.Edit(edit.NodeId, true, offsets, editInput.SnapshotHash, editInput.DomainId));
                Execute(AuthoringOperation.ReplaceGraph(changed));
                attachmentTargetChoice = target.ObjectId;
                string region = surfaceTriangles == null ? "全三角形" : surfaceTriangles.Count().ToString(CultureInfo.InvariantCulture) + "面領域";
                string vertices = clothingVertices == null ? "全頂点" : clothingVertices.Count().ToString(CultureInfo.InvariantCulture) + "頂点";
                SetStatus("衣装をavatar rest表面へfitしました（" + region + "、" + vertices + "、" + fit.MovedVertexCount + "/" + fit.EvaluatedVertexCount + "評価頂点が移動、最大投影距離 " +
                    (fit.MaxProjectionDistance * 1000f).ToString("0.###") + " mm、最大移動量 " +
                    (fit.MaxDisplacement * 1000f).ToString("0.###") + " mm、offset " +
                    accessoryFitOffsetMm.value.ToString("0.###") + " mm）。Rig／poseで交差を確認してください。");
            });
        }

        void TransferAccessorySurfaceWeights()
        {
            Try(() =>
            {
                if (!IsGraph) throw new InvalidOperationException("衣装のgraph objectを選択してください。");
                int targetIndex = attachmentTarget.index;
                if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("weight移行元avatarを選択してください。");
                var target = FindObject(attachmentTargetIds[targetIndex]);
                var session = RigFor(target);
                var skeleton = session == null || target == null ? null : TryResolveSkeleton(session, target.Graph);
                if (skeleton == null) throw new InvalidOperationException("weight移行元avatarのskeletonがありません。");
                var avatarBind = target.Graph.Nodes.Values.SingleOrDefault(item => item.TypeId == BuiltinNodes.SkinBind && item.Binding != null);
                if (avatarBind == null) throw new InvalidOperationException("avatarへ有効なSkinBindがありません。");
                var avatarEvaluation = EvaluateAttachmentTarget(target);
                if (!avatarEvaluation.MeshInputs.TryGetValue(avatarBind.NodeId, out var avatarMeshValue) || avatarMeshValue?.Mesh == null ||
                    !avatarEvaluation.SkinBindingOutputs.TryGetValue(avatarBind.NodeId, out var avatarBindingValue) || avatarBindingValue?.Binding == null)
                    throw new InvalidOperationException("avatarのrest meshとweight評価結果を取得できません。");
                var graph = workspace.Document.ActiveObject.Graph;
                var edit = graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.EditMesh);
                var bind = graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null);
                if (edit == null || bind == null) throw new InvalidOperationException("衣装を先にskin-bindしてください。");
                var evaluation = workspace.Preview.Evaluation;
                if (!evaluation.MeshOutputs.TryGetValue(edit.NodeId, out var editValue) || editValue?.Mesh == null)
                    throw new InvalidOperationException("衣装EditMeshの評価結果を取得できません。");
                float maxDistance = accessoryFitMaxDistanceMm.value / 1000f;
                var surfaceTriangles = SurfaceTriangleSelection();
                var clothingVertices = ClothingVertexSelection();
                var transferred = SkinWeightTransfer.BySurfaceProjection(editValue.Mesh, editValue.Transform,
                    avatarMeshValue.Mesh, avatarMeshValue.Transform, avatarBindingValue.Binding, skeleton, 4,
                    maxDistance, surfaceTriangles, clothingVertices, bind.Binding);
                Execute(AuthoringOperation.UpdateNode(GraphNode.SkinBindNode(bind.NodeId, transferred)));
                attachmentTargetChoice = target.ObjectId;
                string region = surfaceTriangles == null ? "全三角形" : surfaceTriangles.Count().ToString(CultureInfo.InvariantCulture) + "面領域";
                string vertices = clothingVertices == null ? "全頂点" : clothingVertices.Count().ToString(CultureInfo.InvariantCulture) + "頂点";
                SetStatus("avatar表面の最近三角形から衣装weightを補間しました（" + region + "、" + vertices + "、最大距離 " +
                    accessoryFitMaxDistanceMm.value.ToString("0.###") + " mm）。Rig panelで必ず動作確認・手修正してください。自動fitや貫通判定は別機能です。");
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
                var targetPose = EvaluateAttachmentTarget(target).PoseOutputs.Values.Select(value => value.Pose)
                    .FirstOrDefault(pose => pose.SkeletonHash == clothingSkeleton.ContentHash);
                if (targetPose == null) throw new InvalidOperationException("avatarのposeが衣装と同じskeletonではありません。");
                attachmentTargetChoice = target.ObjectId;
                Execute(AuthoringOperation.UpdateNode(GraphNode.PoseNode(poseNode.NodeId, PoseEditing.Rebind(targetPose, clothingSkeleton))));
                var sourceNode = workspace.Document.ActiveObject.Graph.Nodes.Values.FirstOrDefault(item => item.TypeId == BuiltinNodes.PoseSource);
                var sourceUpdate = GraphNode.PoseSourceNode(sourceNode?.NodeId ?? Guid.NewGuid().ToString("D"), target.ObjectId);
                Execute(sourceNode == null ? AuthoringOperation.AddNode(sourceUpdate) : AuthoringOperation.UpdateNode(sourceUpdate));
                RefreshAttachmentControls();
                SetStatus("avatarの現在poseを衣装へコピーしました。必要ならweightを調整して保存してください。");
            });
        }

        void TransferAccessoryWeights()
        {
            Try(() =>
            {
                if (!IsGraph) throw new InvalidOperationException("衣装のgraph objectを選択してください。");
                int targetIndex = attachmentTarget.index;
                if (targetIndex < 0 || targetIndex >= attachmentTargetIds.Count) throw new InvalidOperationException("weight移行元avatarを選択してください。");
                var target = FindObject(attachmentTargetIds[targetIndex]); var session = RigFor(target);
                var skeleton = session == null ? null : TryResolveSkeleton(session, target.Graph);
                if (skeleton == null) throw new InvalidOperationException("weight移行元avatarのskeletonがありません。");
                var graph = workspace.Document.ActiveObject.Graph;
                var edit = graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.EditMesh);
                var bind = graph.Nodes.Values.SingleOrDefault(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null);
                if (edit == null || bind == null) throw new InvalidOperationException("衣装を先にskin-bindしてください。");
                if (!workspace.Preview.Evaluation.MeshOutputs.TryGetValue(edit.NodeId, out var editValue) || editValue?.Mesh == null)
                    throw new InvalidOperationException("衣装EditMeshの評価結果を取得できません。");
                var transferred = SkinWeightTransfer.ByBoneProximity(editValue.Mesh, skeleton, .05f, 4);
                Execute(AuthoringOperation.UpdateNode(GraphNode.SkinBindNode(bind.NodeId, transferred)));
                attachmentTargetChoice = target.ObjectId;
                SetStatus("骨segment近傍から衣装weightの初期値を作成しました。Rig panelで必ず確認・手修正してください。");
            });
        }
    }
}
