using System;
using System.Collections.Generic;
using System.IO;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
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
            workspace = next; commands = new AuthoringCommandService(workspace);
            saveIncomplete = false;
            savedDirectory = loadedPath == null ? null : Path.GetFullPath(loadedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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
            var expressionSession = rigSession == null ? null : expressionSessions.TryGetValue(rigSession.GraphId, out var activeExpression) ? activeExpression : null;
            var springSession = rigSession == null ? null : springSessions.TryGetValue(rigSession.GraphId, out var activeSpring) ? activeSpring : null;
            if (expressionSession == null && next.Document.ActiveObject?.Graph != null) expressionSessions.TryGetValue(next.Document.ActiveObject.Graph.GraphId, out expressionSession);
            if (springSession == null && next.Document.ActiveObject?.Graph != null) springSessions.TryGetValue(next.Document.ActiveObject.Graph.GraphId, out springSession);
            // A legacy project can have a single metadata blob and no graph id at all
            // (the save-failure guard intentionally exercises this shape). Keep that
            // payload available to the Workbench instead of dropping it on refresh.
            if (expressionSession == null && expressionBytes != null && !VrmExpressionSessionsCodec.IsTable(expressionBytes))
                expressionSession = VrmExpressionSessionCodec.Read(expressionBytes);
            if (springSession == null && springBytes != null && !VrmSpringSessionsCodec.IsTable(springBytes))
                springSession = VrmSpringSessionCodec.Read(springBytes);
            var physBonesDocument = physBonesBytes == null ? null : PhysBonesTargetCodec.ReadDocument(physBonesBytes);
            var secondaryMotionDocument = secondaryMotionBytes == null ? null : SecondaryMotionCodec.ReadDocument(secondaryMotionBytes);
            if (rigSession != null && expressionSession != null) rigSession.ValidateSource(expressionSession.SourceHash);
            if (rigSession != null && springSession != null) rigSession.ValidateSource(springSession.SourceHash);
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
            importedRigSession = rigSession;
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
            RefreshSecondaryMotionStatus();
        }

        void Export() => Try(() =>
        {
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "bake-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            if (workspace.Document.Objects.Count > 1 && !ProjectExportService.RequiresNativeProjectExport(workspace.Document))
            {
                string manifest = MultiObjectExportService.Export(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory).ManifestPath;
                SetStatus("Unity用に複数対象を書き出しました: " + manifest);
            }
            else
            {
                string manifest=ProjectExportService.Export(workspace,workspace.InstanceId,workspace.Document.DocumentId,workspace.Document.DocumentRevision,directory).ManifestPath;
                SetStatus("Unity用に書き出しました: " + manifest);
            }
        });

        void ExportGlbStatic() => Try(() =>
        {
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "glb-static-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var result = GlbExportService.ExportStatic(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory);
            SetStatus("標準GLB（表示形状）を書き出しました: " + result.Path);
        });

        void ExportGlbSkinned() => Try(() =>
        {
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "glb-skinned-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var result = GlbExportService.ExportSkinnedWithTransforms(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices());
            SetStatus("標準GLB（skin/morph保持）を書き出しました: " + result.Path);
        });

        void ExportGlbSkinnedExtended() => Try(() =>
        {
            var directory = Path.Combine(Path.GetFullPath(projectPath.value), "exports", "glb-skinned-extended-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6));
            var result = GlbExportService.ExportSkinnedExtendedWithTransforms(workspace, workspace.InstanceId, workspace.Document.DocumentId, workspace.Document.DocumentRevision, directory, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices());
            SetStatus("拡張GLB（全weight保持）を書き出しました: " + result.Path);
        });

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

        // glTF skinning uses the joint world frames and inverse-bind matrices;
        // the selected skinned mesh node affine is metadata only and must not be
        // emitted as a second post-skin matrix.
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
                    result[item.ObjectId] = session.SourceSkin.InverseBindMatrices;
            }
            if (importedRigSession?.SourceSkin?.InverseBindMatrices != null && workspace.Document.ActiveObject?.Graph != null)
                result[workspace.Document.ActiveObject.ObjectId] = importedRigSession.SourceSkin.InverseBindMatrices;
            return result;
        }

    }
}
