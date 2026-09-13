using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Geometry;
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
        FloatField accessoryFitOffsetMm, accessoryFitMaxDistanceMm;
        TextField accessorySurfaceTriangleIds;
        TextField accessoryClothingVertexIds;
        Button attachmentApply, attachmentRemove, accessorySkinBind, accessoryPolygonMaterialize, accessoryAutoWeight, accessorySurfaceWeight, accessorySurfaceFit, accessoryPoseCopy;
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
            accessoryPolygonMaterialize = Button("Polygon造形をskin衣装へ派生", MaterializePolygonAccessory, "object-polygon-materialize");
            accessoryAutoWeight = Button("衣装の自動weight初期化（骨近傍）", TransferAccessoryWeights, "object-skin-auto-weight");
            accessorySurfaceWeight = Button("衣装の自動weight初期化（avatar表面）", TransferAccessorySurfaceWeights, "object-skin-surface-weight");
            accessoryFitOffsetMm = Number(attachmentPanel, "avatar表面からのfit offset (mm)", 2, "object-surface-fit-offset-mm");
            accessoryFitMaxDistanceMm = Number(attachmentPanel, "surface fit最大距離 (mm)", 50, "object-surface-fit-max-distance-mm");
            accessorySurfaceTriangleIds = new TextField("avatar面ID（カンマ区切り・空欄=全て）") { name = "object-surface-triangle-ids" };
            accessorySurfaceTriangleIds.tooltip = "avatarのrest meshを三角形の通し番号で限定します。面IDはsubmesh順に0から数え、空欄なら全三角形を対象にします。fitとweightで同じ領域を使います。";
            attachmentPanel.Add(accessorySurfaceTriangleIds);
            accessoryClothingVertexIds = new TextField("衣装頂点ID（カンマ区切り・空欄=全て）") { name = "object-surface-clothing-vertex-ids" };
            accessoryClothingVertexIds.tooltip = "衣装EditMeshの頂点IDを限定します。空欄なら全頂点を対象にし、指定時は未選択頂点の位置・weightを保持します。";
            attachmentPanel.Add(accessoryClothingVertexIds);
            accessorySurfaceFit = Button("衣装をavatar表面へfit", FitAccessoryToAvatarSurface, "object-surface-fit");
            accessoryPoseCopy = Button("avatarの現在poseを衣装へコピー", CopyAvatarPose, "object-skin-pose-copy");
            attachmentPanel.Add(attachmentApply); attachmentPanel.Add(attachmentRemove); attachmentPanel.Add(accessorySkinBind); attachmentPanel.Add(accessoryPolygonMaterialize); attachmentPanel.Add(accessoryAutoWeight); attachmentPanel.Add(accessorySurfaceWeight); attachmentPanel.Add(accessorySurfaceFit); attachmentPanel.Add(accessoryPoseCopy);
            var help = new Label("明示したstable BoneIdへ剛体追従します。衣装skin-bindは選択avatarの骨格をコピーし、全頂点をRootへ初期化してRig panelでweight paintできます。Polygon造形をskin衣装へ派生すると、元のPolygon graphを残したまま編集結果をMeshSourceへ確定し、新しい衣装objectを作成します。自動weight初期化（骨近傍）はrest骨segmentへの距離から最大4本を選ぶ簡易初期値です。avatar表面が評価できる場合は、表面上の最近三角形から既存avatar weightを補間するavatar表面方式を推奨します。avatar面IDを指定するとfitとweightの対象面を同じ領域へ限定できます。衣装頂点IDを指定すると未選択頂点の位置・weightを保持できます。空欄は全てを対象にします。どちらも必ず動作確認・Rig panelで手修正してください。skin-bind後はavatarの現在poseをボタンで衣装へコピーして保存できます。名前で推測せず、装着offsetは基準姿勢のbone localメートルで保存します。自動fitや貫通判定は別機能です。");
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
                attachmentApply.SetEnabled(false); attachmentRemove.SetEnabled(false); accessorySkinBind.SetEnabled(false); accessoryPolygonMaterialize.SetEnabled(false); accessoryAutoWeight.SetEnabled(false); accessorySurfaceWeight.SetEnabled(false); accessorySurfaceFit.SetEnabled(false); accessoryPoseCopy.SetEnabled(false); return;
            }
            var targets = workspace.Document.Objects.Where(item => item.ObjectId != workspace.Document.ActiveObjectId && item.Graph != null).ToArray();
            attachmentTargetIds.AddRange(targets.Select(item => item.ObjectId));
            var targetLabels = targets.Select(item => "graph · " + item.ObjectId.Substring(0, Math.Min(8, item.ObjectId.Length))).ToList();
            if (targetLabels.Count == 0) targetLabels.Add("対象なし");
            attachmentTarget.choices = targetLabels;
            var poseSource = workspace.Document.ActiveObject.Graph.Nodes.Values.FirstOrDefault(item => item.TypeId == BuiltinNodes.PoseSource);
            string requestedTarget = string.IsNullOrEmpty(attachmentTargetChoice) ? (node?.AttachmentTargetObjectId ?? poseSource?.PoseSourceObjectId) : attachmentTargetChoice;
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
                nodes.Any(node => node.TypeId == BuiltinNodes.Skeleton || node.TypeId == BuiltinNodes.SkinBind ||
                    node.TypeId == BuiltinNodes.SkinDeform || node.TypeId == BuiltinNodes.Pose || node.TypeId == BuiltinNodes.Attachment ||
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
                var result = AccessorySkinMaterializer.Materialize(graph, skeleton, root.BoneId, target.ObjectId);
                Execute(AuthoringOperation.AddGraph(result.Graph));
                attachmentTargetChoice = target.ObjectId;
                SetStatus("Polygon造形をskin衣装へ派生しました。元graphは保持されています。新しい衣装objectでweight・fit・poseを確認して保存してください。");
            });
        }

        bool TargetAvatarSurfaceAvailable(AuthoringObject target)
        {
            try
            {
                var evaluation = target.EvaluateGraph();
                var bind = target.Graph.Nodes.Values.SingleOrDefault(item => item.TypeId == BuiltinNodes.SkinBind && item.Binding != null);
                return bind != null && evaluation.MeshInputs.TryGetValue(bind.NodeId, out var mesh) && mesh != null && mesh.Mesh != null &&
                    evaluation.SkinBindingOutputs.TryGetValue(bind.NodeId, out var binding) && binding != null && binding.Binding != null;
            }
            catch (AuthoringException) { return false; }
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
                var avatarEvaluation = target.EvaluateGraph();
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
                SetStatus("衣装をavatar rest表面へfitしました（" + region + "、" + vertices + "、" + fit.MovedVertexCount + "/" + fitted.Length + "頂点移動、最大投影距離 " +
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
                var avatarEvaluation = target.EvaluateGraph();
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
                var targetPose = target.EvaluateGraph().PoseOutputs.Values.Select(value => value.Pose)
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
