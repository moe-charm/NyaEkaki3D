using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using NyaForge.Authoring.Simulation;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void CancelReplace() { confirmRow.style.display = UnityEngine.UIElements.DisplayStyle.None; }
        void AddSample(float scale)
        {
            Execute(AuthoringOperation.AddMesh(AuthoringFixtures.Panel(scale), new RestTransform(scale, new Vec3())));
            if (!workspace.Document.IsEmpty) { Select(new[] { 0 }); Frame(); }
        }
        void ConfirmReplace(Action action)
        {
            if (!HasUnsaved) { Try(action); return; }
            confirmRow.Clear(); confirmRow.style.display = DisplayStyle.Flex;
            confirmRow.Add(new Label("未保存の変更があります。保存するか、変更を破棄して進んでください。"));
            confirmRow.Add(Button("変更を破棄して進む", () => { confirmRow.style.display = DisplayStyle.None; Try(action); }, "authoring-discard"));
            confirmRow.Add(Button("キャンセル", CancelReplace, "authoring-cancel"));
            controls.schedule.Execute(() => controls.ScrollTo(confirmRow));
        }

        void ReplaceWorkspace(AuthoringWorkspace next, string loadedPath)
        {
            paintCanvas?.CancelStroke();
            ClearSurfacePreparation();
            string oldStage = projection.PreviewNodeId;
            projection.PreviewNodeId = "";
            try { using (var prepared = projection.PrepareGraph(next.Document, next.Preview)) prepared.Commit(); }
            catch { projection.PreviewNodeId = oldStage; throw; }
            ClearSpringPlayback(false);
            activeEditContext = null;
            selectedFaces.Clear(); faceMode.SetValueWithoutNotify(false);
            importedRigSession = null;
            importedRigSessions.Clear();
            ClearImportedPhysBones();
            ClearImportedVrmExpressions();
            ClearImportedVrmSpring();
            ClearImportedSecondaryMotion();
            if (session == null)
            {
                session = new AuthoringWorkbenchSession();
                session.StateChanged += OnSessionStateChanged;
            }
            session.Replace(next, loadedPath);
            selectionContext.SetObject(next.Document.ActiveObjectId);
            selectionContext.SetEditNode("");
            RefreshReferenceProtectionFromWorkspace();
            RefreshDeliveryAllowlistFromWorkspace();
            projectPath.SetValueWithoutNotify(loadedPath ?? Path.Combine(Application.persistentDataPath, "Authoring", "Project-" + Guid.NewGuid().ToString("N").Substring(0, 8)));
            Select(projection.Points.Length == 0 ? Array.Empty<int>() : new[] { 0 }); Frame(); Refresh();
            SetStatus(loadedPath == null ? "新しい制作プロジェクトです。形を追加して始めてください。" : "制作状態を開きました。ここから新しい履歴を始めます。旧形式は別フォルダへ保存してください。");
        }


        void OpenProject()
        {
            var directory = Path.GetFullPath(projectPath.value);
            var next = ProjectStore.Open(directory);
            var rigBytes = next.Attachments.Read(ProjectAttachments.Rig);
            var rigSession = rigBytes == null ? null : ImportedRigSessionCodec.Read(rigBytes);
            var rigSessionsBytes = next.Attachments.Read(ProjectAttachments.RigSessions);
            var rigSessions = rigSessionsBytes == null ? null : ImportedRigSessionsCodec.Read(rigSessionsBytes);
            if (rigSessions == null && rigSession != null)
                rigSessions = new Dictionary<string, ImportedRigSession>(StringComparer.Ordinal) { [rigSession.GraphId] = rigSession };
            var expressionBytes = next.Attachments.Read(ProjectAttachments.Expressions);
            var springBytes = next.Attachments.Read(ProjectAttachments.Springs);
            var physBonesBytes = next.Attachments.Read(ProjectAttachments.PhysBones);
            var secondaryMotionBytes = next.Attachments.Read(ProjectAttachments.SecondaryMotion);
            var secondaryMotionSessions = new Dictionary<string, SecondaryMotionAsset>(StringComparer.Ordinal);
            var expressionSessions = new Dictionary<string, VrmExpressionSession>(StringComparer.Ordinal);
            var springSessions = new Dictionary<string, VrmSpringSession>(StringComparer.Ordinal);
            if (expressionBytes != null)
            {
                if (VrmExpressionSessionsCodec.IsTable(expressionBytes))
                    foreach (var item in VrmExpressionSessionsCodec.Read(expressionBytes)) expressionSessions.Add(item.Key, item.Value);
                else
                {
                    var legacy = VrmExpressionSessionCodec.Read(expressionBytes); var graphId = LegacyVrmSessionGraphId(next, rigSession);
                    if (graphId != null) expressionSessions.Add(graphId, legacy);
                }
            }
            if (springBytes != null)
            {
                if (VrmSpringSessionsCodec.IsTable(springBytes))
                    foreach (var item in VrmSpringSessionsCodec.Read(springBytes)) springSessions.Add(item.Key, item.Value);
                else
                {
                    var legacy = VrmSpringSessionCodec.Read(springBytes); var graphId = LegacyVrmSessionGraphId(next, rigSession);
                    if (graphId != null) springSessions.Add(graphId, legacy);
                }
            }
            string selectedGraphId = next.Document.ActiveObject?.Graph?.GraphId;
            ImportedRigSession activeRigSession = selectedGraphId != null && rigSessions != null && rigSessions.TryGetValue(selectedGraphId, out var graphRig) ? graphRig
                : rigSession != null && rigSession.GraphId == selectedGraphId ? rigSession : null;
            var expressionSession = selectedGraphId != null && expressionSessions.TryGetValue(selectedGraphId, out var activeExpression) ? activeExpression : null;
            var springSession = selectedGraphId != null && springSessions.TryGetValue(selectedGraphId, out var activeSpring) ? activeSpring : null;
            if (activeRigSession == null && rigSession != null && rigSessions == null) activeRigSession = rigSession;
            // A legacy project can have a single metadata blob and no graph id at all
            // (the save-failure guard intentionally exercises this shape). Keep that
            // payload available to the Workbench instead of dropping it on refresh.
            // A legacy single-session sidecar has no graph key. It is safe to
            // use only when the project has one graph, or when its companion
            // legacy rig explicitly names the active graph. Never let an
            // ambiguous legacy payload become the metadata for whichever
            // object happens to be selected after a multi-object Open.
            bool legacySessionCanBind = next.Document.Objects.Count <= 1 ||
                (rigSession != null && selectedGraphId != null && rigSession.GraphId == selectedGraphId);
            bool ambiguousLegacySession = !legacySessionCanBind &&
                ((expressionBytes != null && !VrmExpressionSessionsCodec.IsTable(expressionBytes)) ||
                 (springBytes != null && !VrmSpringSessionsCodec.IsTable(springBytes)));
            if (expressionSession == null && expressionBytes != null && !VrmExpressionSessionsCodec.IsTable(expressionBytes) && legacySessionCanBind)
                expressionSession = VrmExpressionSessionCodec.Read(expressionBytes);
            if (springSession == null && springBytes != null && !VrmSpringSessionsCodec.IsTable(springBytes) && legacySessionCanBind)
                springSession = VrmSpringSessionCodec.Read(springBytes);
            var physBonesDocument = physBonesBytes == null ? null : PhysBonesTargetCodec.ReadDocument(physBonesBytes);
            SecondaryMotionDocument secondaryMotionDocument = null;
            if (secondaryMotionBytes != null && SecondaryMotionSessionsCodec.IsTable(secondaryMotionBytes))
            {
                foreach (var item in SecondaryMotionSessionsCodec.Read(secondaryMotionBytes)) secondaryMotionSessions.Add(item.Key, item.Value);
                string activeGraphId = next.Document.ActiveObject?.Graph?.GraphId;
                if (activeGraphId != null && secondaryMotionSessions.TryGetValue(activeGraphId, out var activeSecondary))
                    secondaryMotionDocument = SecondaryMotionCodec.ReadDocument(SecondaryMotionCodec.Write(activeSecondary));
            }
            else if (secondaryMotionBytes != null)
            {
                secondaryMotionDocument = SecondaryMotionCodec.ReadDocument(secondaryMotionBytes);
                string legacyGraphId = next.Document.ActiveObject?.Graph?.GraphId;
                if (legacyGraphId != null && secondaryMotionDocument.Asset != null) secondaryMotionSessions[legacyGraphId] = secondaryMotionDocument.Asset;
            }
            if (activeRigSession != null && expressionSession != null) activeRigSession.ValidateSource(expressionSession.SourceHash);
            if (activeRigSession != null && springSession != null) activeRigSession.ValidateSource(springSession.SourceHash);
            // Validate every graph-keyed metadata pair before replacing the live
            // workspace. This catches a stale A/B combination even when the
            // currently active object is not the rig attachment stored in the
            // legacy single-session field.
            if (rigSessions != null)
                foreach (var pair in rigSessions)
                {
                    if (expressionSessions.TryGetValue(pair.Key, out var expression)) pair.Value.ValidateSource(expression.SourceHash);
                    if (springSessions.TryGetValue(pair.Key, out var spring)) pair.Value.ValidateSource(spring.SourceHash);
                }
            ReplaceWorkspace(next, directory);
            if (rigSessions != null)
                foreach (var item in rigSessions) importedRigSessions.Add(item.Key, item.Value);
            foreach (var item in expressionSessions) importedVrmSessions.Add(item.Key, item.Value);
            foreach (var item in springSessions) importedVrmSpringSessions.Add(item.Key, item.Value);
            foreach (var item in secondaryMotionSessions) importedSecondaryMotionSessions.Add(item.Key, item.Value);
            importedRigSession = activeRigSession;
            importedVrmSession = expressionSession; Refresh();
            importedVrmSpringSession = springSession;
            // Refresh can clear graph-keyed fields when opening a legacy static
            // document that has no active graph. Preserve the decoded single
            // session as the compatibility fallback in that case.
            if (importedVrmSession == null && expressionSession != null) importedVrmSession = expressionSession;
            if (importedVrmSpringSession == null && springSession != null) importedVrmSpringSession = springSession;
            RefreshVrmSpringStatus();
            SetImportedPhysBones(physBonesDocument);
            importedSecondaryMotionDocument = secondaryMotionDocument;
            importedSecondaryMotionAsset = secondaryMotionDocument?.Asset;
            SelectSecondaryMotionForActiveGraph();
            RefreshSecondaryMotionStatus();
            if (ambiguousLegacySession)
                SetStatus("旧形式のVRM expression／Spring sidecarは複数graphの割当先を特定できないため未割当です。元モデルを再取込するか、GraphId付きprojectへ移行してください。sidecar自体は保存されています。");
        }

        void Export() => Try(() =>
        {
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "bake-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var deliveryIds = DeliveryObjectIdsForExport();
            EnsureGenericDeliveryExportAllowed();
            if (deliveryIds.Count > 1 && !ProjectExportService.RequiresNativeProjectExport(workspace.Document))
            {
                string manifest = MultiObjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, deliveryIds).ManifestPath;
                SetStatus("Unity用に複数対象を書き出しました: " + manifest);
            }
            else
            {
                string manifest = (deliveryIds.Count == 1 && workspace.Document.Objects.Count > 1 && !ProjectExportService.RequiresNativeProjectExport(workspace.Document)
                    ? ProjectExportService.ExportObject(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, deliveryIds[0], directory)
                    : ProjectExportService.Export(workspace,workspace.InstanceId,workspace.Document.DocumentId,workspace.Document.DocumentRevision,directory)).ManifestPath;
                SetStatus("Unity用に書き出しました: " + manifest);
            }
        });

        void ExportGlbStatic() => Try(() =>
        {
            var deliveryIds = DeliveryObjectIdsForExport();
            EnsureGenericDeliveryExportAllowed();
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "glb-static-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var result = GlbExportService.ExportStaticWithOverrides(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, StaticDisplayMeshesForExport(), deliveryIds);
            SetStatus("標準GLB（表示形状）を書き出しました: " + result.Path + " · report: " + result.ReportPath);
        });

        IReadOnlyDictionary<string, GraphMeshValue> StaticDisplayMeshesForExport()
        {
            var result = new Dictionary<string, GraphMeshValue>(StringComparer.Ordinal);
            if (workspace?.Document?.Objects == null) return result;
            foreach (var item in workspace.Document.Objects)
            {
                if (item?.Graph == null || !importedRigSessions.TryGetValue(item.Graph.GraphId, out var session) || session?.SourceSkin == null)
                    continue;
                var evaluation = item == workspace.Document.ActiveObject ? workspace.Preview.Evaluation : item.EvaluateGraph();
                var bindingNode = item.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null);
                var authoredBinding = bindingNode != null && evaluation.SkinBindingOutputs.TryGetValue(bindingNode.NodeId, out var bindingValue) ? bindingValue.Binding : bindingNode?.Binding;
                // A source-skin correction is part of the static display
                // contract. Falling back to the uncorrected graph output
                // would publish a GLB that visibly differs from the
                // Workbench while hiding a stale/missing binding.
                var corrected = SourceSkinGraphAdapter.ApplyToEvaluation(evaluation, item.Graph, session, authoredBinding);
                if (corrected?.Mesh != null) result[item.ObjectId] = corrected;
            }
            return result;
        }

        void ExportGlbSkinned() => Try(() =>
        {
            var deliveryIds = DeliveryObjectIdsForExport();
            EnsureGenericDeliveryExportAllowed();
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "glb-skinned-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var result = GlbExportService.ExportSkinnedWithTransforms(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices(), SkinnedJointLocalTransforms(), deliveryIds);
            SetStatus("標準GLB（skin/morph保持）を書き出しました: " + result.Path + " · report: " + result.ReportPath);
        });

        void ExportGlbSkinnedExtended() => Try(() =>
        {
            var deliveryIds = DeliveryObjectIdsForExport();
            EnsureGenericDeliveryExportAllowed();
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "glb-skinned-extended-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var result = GlbExportService.ExportSkinnedExtendedWithTransforms(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices(), SkinnedJointLocalTransforms(), deliveryIds);
            SetStatus("拡張GLB（全weight保持）を書き出しました: " + result.Path + " · report: " + result.ReportPath);
        });

        void ExportSelectedClothingPackage() => Try(() =>
        {
            var item = workspace?.Document?.ActiveObject;
            if (item == null || item.IsStaticProfile || item.Graph == null)
                throw new InvalidOperationException("選択衣装のskin graphを選択してください。");
            if (referenceProtectedObjectIds.Contains(item.ObjectId))
                throw new AuthoringException("REFERENCE_EXPORT_BLOCKED", "参照として保護したobjectは衣装納品対象にできません。保護を解除するか、衣装objectを選択してください。");
            var evaluation = item.EvaluateGraph();
            if (!evaluation.IsComplete || evaluation.Output?.Mesh == null)
                throw new InvalidOperationException("選択衣装のgraphが未完成です。");
            var skeletonNode = item.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Skeleton && node.Skeleton != null);
            var bindingNode = item.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.SkinBind && node.Binding != null);
            if (skeletonNode == null || bindingNode == null)
                throw new InvalidOperationException("選択衣装へskeletonとskin-bindを先に接続してください。");
            if (!evaluation.SkinBindingOutputs.TryGetValue(bindingNode.NodeId, out var bindingValue) || bindingValue?.Binding == null)
                throw new InvalidOperationException("選択衣装のskin-bind評価結果を取得できません。");
            var packageSkeleton = SkeletonBindingSubset.ForBinding(skeletonNode.Skeleton, bindingValue.Binding);
            var packageBinding = bindingValue.Binding.RebindToSkeleton(evaluation.Output.Mesh, packageSkeleton);
            var inverseMap = SkinnedInverseBindMatrices();
            inverseMap.TryGetValue(item.ObjectId, out var inverseBinds);
            var jointMap = SkinnedJointLocalTransforms();
            jointMap.TryGetValue(item.ObjectId, out var jointLocals);
            string root = Path.Combine(Path.GetFullPath(projectPath.value), "exports");
            string packageDirectory = Path.Combine(root, "clothing-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            // Keep the temporary GLB staging path short. The package export
            // itself already adds an atomic staging suffix; nesting another
            // long GUID below a deep project path can exceed MAX_PATH on
            // Windows before the package is published.
            string temporaryGlbDirectory = Path.Combine(root, ".glb-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                var glb = GlbExportService.ExportSkinnedObject(workspace, workspace.InstanceId, workspace.Document.DocumentId,
                    workspace.Document.DocumentRevision, item.ObjectId, temporaryGlbDirectory, false, inverseBinds, jointLocals,
                    packageSkeleton, packageBinding);
                byte[] glbBytes = File.ReadAllBytes(glb.Path);
                string manifest = SkinnedClothingPackage.Export(packageDirectory, glbBytes, evaluation.Output.Mesh,
                    packageSkeleton, packageBinding, workspace.Document.DocumentId, item.ObjectId,
                    item.Graph.GraphId, workspace.Document.StateHash, item.Graph.ContentHash);
                SetStatus("選択衣装だけのskin packageを書き出しました: " + manifest + " · GLBとBoneId付きsidecarを同梱");
            }
            finally
            {
                if (Directory.Exists(temporaryGlbDirectory)) Directory.Delete(temporaryGlbDirectory, true);
            }
        });

        void ExportVrm1() => Try(() =>
        {
            var item = workspace?.Document?.ActiveObject;
            if (item == null || item.IsStaticProfile)
                throw new InvalidOperationException("VRM 1.0出力にはhumanoid avatar graphを選択してください。");
            if (!importedRigSessions.TryGetValue(item.Graph.GraphId, out var session) || session == null || session.HumanoidNodes.Count == 0)
                throw new InvalidOperationException("VRM 1.0出力には、humanoid mappingを持つVRM/GLB avatarの取込が必要です。");
            var skeletonNode = item.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Skeleton && node.Skeleton != null);
            if (skeletonNode == null) throw new InvalidOperationException("VRM 1.0出力用のskeletonがありません。");
            if (workspace.Document.Objects.Any(candidate => candidate.IsStaticProfile || candidate.Graph == null ||
                candidate.Graph.Nodes.Values.Any(node => node.TypeId == BuiltinNodes.Attachment)))
                throw new InvalidOperationException("VRM 1.0出力にはskin済みgraphだけを含めてください。剛体装着は先にskin-bindへ変換してください。");
            var skeletons = workspace.Document.Objects.Select(candidate => candidate.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.Skeleton && node.Skeleton != null)?.Skeleton).ToArray();
            if (skeletons.Any(value => value == null || value.ContentHash != skeletonNode.Skeleton.ContentHash))
                throw new InvalidOperationException("VRM 1.0出力のavatarと衣装は同じavatar骨格へskin-bindしてください。");
            var boneIndices = skeletonNode.Skeleton.Bones.Select((bone, index) => new { bone.BoneId, index }).ToDictionary(value => value.BoneId, value => value.index, StringComparer.Ordinal);
            var humanoid = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in session.HumanoidNodes)
            {
                if (!session.NodeToBone.TryGetValue(pair.Value, out var boneId) || !boneIndices.TryGetValue(boneId, out var boneIndex))
                    throw new InvalidOperationException("VRM humanoid mappingを出力skeletonへ対応できません: " + pair.Key);
                // Keep a stable authored token here. VrmExportService resolves
                // it through GlbExportNodeMap, so this remains correct even
                // when the writer places joint nodes before mesh nodes.
                humanoid[pair.Key] = 1 + boneIndex;
            }
            var expressions = new List<VrmExpressionExport>();
            if (importedVrmSessions.TryGetValue(item.Graph.GraphId, out var expressionSession) && expressionSession != null && expressionSession.Expressions.Count > 0)
            {
                var morphNode = item.Graph.Nodes.Values.FirstOrDefault(node => node.TypeId == BuiltinNodes.MorphSet && node.Morphs != null);
                if (morphNode == null) throw new InvalidOperationException("VRM表情を出力するには、MorphSetが必要です。");
                var morphIndices = morphNode.Morphs.Targets.Select((target, index) => new { target.TargetId, index }).ToDictionary(value => value.TargetId, value => value.index, StringComparer.Ordinal);
                foreach (var expression in expressionSession.Expressions)
                {
                    var binds = new List<VrmMorphBind>();
                    foreach (var weight in expression.Weights)
                    {
                        if (!morphIndices.TryGetValue(weight.Key, out var morphIndex))
                            throw new InvalidOperationException("VRM表情のmorph targetを出力へ対応できません: " + expression.Name);
                        binds.Add(new VrmMorphBind(0, morphIndex, weight.Value));
                    }
                    if (binds.Count > 0) expressions.Add(new VrmExpressionExport(expression.Name, expression.Preset, expression.IsCustom, binds));
                }
            }
            VrmSpringExport springs = null;
            if (importedVrmSpringSessions.TryGetValue(item.Graph.GraphId, out var springSession) && springSession != null && springSession.SpringBones.Count > 0)
            {
                if (springSession.Format != "vrm1") throw new InvalidOperationException("VRM 1.0出力のSpringBoneは、VRM 1.0由来の設定だけに対応しています。VRM 0.x設定は変換後に出力してください。");
                if (!springSession.HasCompleteDetails) throw new InvalidOperationException("VRM SpringBoneの詳細形状またはgravityDirが不足しています。元モデルを再読込してから出力してください。");
                int MapNode(int sourceNode, string label)
                {
                    if (!session.NodeToBone.TryGetValue(sourceNode, out var boneId) || !boneIndices.TryGetValue(boneId, out var boneIndex))
                        throw new InvalidOperationException("VRM SpringBoneの" + label + "を出力skeletonへ対応できません: node " + sourceNode);
                    return 1 + boneIndex;
                }
                var colliderValues = new List<VrmSpringColliderExport>();
                var colliderGroups = new List<VrmSpringColliderGroupExport>();
                foreach (var group in springSession.ColliderGroups)
                {
                    if (group.Shapes == null || group.Shapes.Count != group.ColliderCount || group.ColliderCount == 0)
                        throw new InvalidOperationException("VRM SpringBoneのcollider groupに詳細形状がありません。空のgroupは出力できません。");
                    int first = colliderValues.Count;
                    for (int index = 0; index < group.ColliderCount; index++)
                    {
                        var shape = group.Shapes[index];
                        if (!shape.Offset.HasValue || (shape.Kind == "capsule" && !shape.Tail.HasValue))
                            throw new InvalidOperationException("VRM SpringBone colliderのoffset/tailが不足しています。");
                        colliderValues.Add(new VrmSpringColliderExport(MapNode(group.ColliderNodeIndices[index], "collider"), shape.Kind, shape.Offset.Value, shape.Radius, shape.Tail));
                    }
                    colliderGroups.Add(new VrmSpringColliderGroupExport("ColliderGroup " + colliderGroups.Count, Enumerable.Range(first, group.ColliderCount)));
                }
                var springValues = new List<VrmSpringExport.SpringExportGroup>();
                foreach (var group in springSession.SpringBones)
                {
                    var joints = group.Joints.Select(joint =>
                    {
                        if (!joint.GravityDirection.HasValue) throw new InvalidOperationException("VRM SpringBone jointのgravityDirが不足しています。");
                        return new VrmSpringJointExport(MapNode(joint.NodeIndex, "joint"), joint.HitRadius, joint.Stiffness, joint.GravityPower, joint.GravityDirection.Value, joint.DragForce);
                    });
                    var colliderIndices = group.ColliderGroupIndices.Select(index =>
                    {
                        if (index < 0 || index >= colliderGroups.Count) throw new InvalidOperationException("VRM SpringBone collider group indexが範囲外です。");
                        return index;
                    });
                    int? center = group.CenterNodeIndex < 0 ? (int?)null : MapNode(group.CenterNodeIndex, "center");
                    springValues.Add(new VrmSpringExport.SpringExportGroup(group.Name, joints, colliderIndices, center));
                }
                springs = new VrmSpringExport(colliderValues, colliderGroups, springValues);
            }
            var authors = (vrmAuthors?.value ?? "").Split(new[] { ',', '、', ';' }, StringSplitOptions.RemoveEmptyEntries).Select(value => value.Trim()).Where(value => value.Length > 0).ToArray();
            var metadata = new VrmExportMetadata(vrmName?.value, authors, vrmLicenseUrl?.value, humanoid, expressions: expressions, springs: springs, usesAuthoredNodeTokens: true);
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "vrm1-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var result = VrmExportService.ExportVrm1(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, metadata, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices(), SkinnedJointLocalTransforms(), item.ObjectId);
            SetStatus("VRM 1.0（humanoid）を書き出しました: " + result.Path + " · report: " + result.ReportPath);
        });

    }
}
