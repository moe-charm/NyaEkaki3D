using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Simulation;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Label vrmSpringStatus;
        SecondaryMotionDocument importedSecondaryMotionDocument;
        SecondaryMotionAsset importedSecondaryMotionAsset;
        Label secondaryMotionStatus;
        Button secondaryMotionRebindButton;

        void BuildVrmSpringStatus(VisualElement parent)
        {
            vrmSpringStatus = new Label { name = "vrm-spring-status" };
            vrmSpringStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(vrmSpringStatus);
            secondaryMotionStatus = new Label { name = "secondary-motion-status" };
            secondaryMotionStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(secondaryMotionStatus);
            secondaryMotionRebindButton = Button("同じBoneIdで再bind", () => Try(RebindSecondaryMotionIdentity), "secondary-motion-rebind");
            parent.Add(secondaryMotionRebindButton);
            var rebindHelp = new Label("骨格の位置変更などでstaleになったとき、stable BoneIdを同一のまま再bindします。ID名の推測や頂点の自動対応は行いません。");
            rebindHelp.style.whiteSpace = WhiteSpace.Normal; parent.Add(rebindHelp);
            BuildSpringPlayback(parent);
        }

        void RefreshVrmSpringStatus()
        {
            if (vrmSpringStatus == null) return;
            RefreshSpringPlayback();
            if (importedVrmSpringSession == null) { vrmSpringStatus.text = "SpringBone設定: なし"; return; }
            int joints = 0; foreach (var group in importedVrmSpringSession.SpringBones) joints += group.Joints.Count;
            vrmSpringStatus.text = "SpringBone設定: " + importedVrmSpringSession.SpringBones.Count + " chain · " + joints + " joint · " + importedVrmSpringSession.ColliderGroups.Count + " collider group";
            if (!importedVrmSpringSession.HasCompleteDetails) vrmSpringStatus.text += " 詳細不足：再取込または元設定の確認が必要です。";
        }

        void RefreshSecondaryMotionStatus()
        {
            if (secondaryMotionStatus == null) return;
            secondaryMotionRebindButton?.SetEnabled(false);
            if (importedSecondaryMotionDocument == null) { secondaryMotionStatus.text = "共通揺れ設定: なし"; return; }
            if (!importedSecondaryMotionDocument.IsSupported)
            {
                secondaryMotionStatus.text = "共通揺れ設定: 未対応wire version " + importedSecondaryMotionDocument.WireVersion + "（保持のみ・再生不可）";
                return;
            }
            try
            {
                var skeleton = FindCurrentSkeleton(); var mesh = workspace?.Evaluate();
                importedSecondaryMotionAsset.ValidateFor(skeleton, mesh);
                secondaryMotionStatus.text = "共通揺れ設定: " + importedSecondaryMotionAsset.Profile.SimulatorId + " · " + importedSecondaryMotionAsset.Chains.Count + " chain · 保存済み";
            }
            catch (AuthoringException error)
            {
                secondaryMotionStatus.text = "共通揺れ設定: 再bindが必要です（" + error.Code + "）。元の設定は保持されています。";
                secondaryMotionRebindButton?.SetEnabled(CanIdentityRebind());
            }
        }

        bool CanIdentityRebind()
        {
            if (importedSecondaryMotionAsset == null || !importedSecondaryMotionDocument.IsSupported || workspace == null) return false;
            var skeleton = FindCurrentSkeleton();
            // A rig edit can legitimately leave the pose/binding stale until its own
            // rebind. Use the source mesh as a topology witness in that intermediate
            // state so the explicit BoneId action remains available.
            MeshData mesh = CurrentSecondaryMotionMesh();
            if (importedSecondaryMotionAsset.SkeletonHash != "" && skeleton == null) return false;
            if (importedSecondaryMotionAsset.MeshTopologyHash != "" && mesh == null) return false;
            if (importedSecondaryMotionAsset.FixedVertexIndices.Count > 0 && mesh != null && importedSecondaryMotionAsset.MeshTopologyHash != mesh.TopologyHash) return false;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chain in importedSecondaryMotionAsset.Chains) foreach (var id in chain.BoneIds) ids.Add(id);
            foreach (var group in importedSecondaryMotionAsset.ColliderGroups) foreach (var collider in group.Colliders) if (collider.BoneId != "") ids.Add(collider.BoneId);
            return skeleton != null && ids.All(id => skeleton.ById.ContainsKey(id));
        }

        void RebindSecondaryMotionIdentity()
        {
            if (!CanIdentityRebind()) throw new InvalidOperationException("同一BoneIdで再bindできる状態ではありません。必要な対応表を指定してください。");
            var skeleton = FindCurrentSkeleton();
            MeshData mesh = CurrentSecondaryMotionMesh();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var chain in importedSecondaryMotionAsset.Chains) foreach (var id in chain.BoneIds) ids.Add(id);
            foreach (var group in importedSecondaryMotionAsset.ColliderGroups) foreach (var collider in group.Colliders) if (collider.BoneId != "") ids.Add(collider.BoneId);
            var map = ids.ToDictionary(id => id, id => id, StringComparer.Ordinal);
            var rebound = SecondaryMotionRebind.Apply(importedSecondaryMotionAsset, skeleton, mesh, map);
            var sessions = ReadSecondaryMotionSessions(workspace);
            string graphId = workspace.Document.ActiveObject?.Graph?.GraphId;
            if (graphId == null) throw new InvalidOperationException("共通揺れ設定の対象graphがありません。");
            sessions[graphId] = rebound;
            var owned = new Dictionary<string, byte[]>(); foreach (var name in workspace.Attachments.Hashes.Keys) owned[name] = workspace.Attachments.Read(name);
            owned[ProjectAttachments.SecondaryMotion] = SecondaryMotionSessionsCodec.Write(sessions);
            ClearSpringPlayback(true); workspace.SetAttachmentsWithHistory(new ProjectAttachments(owned));
            importedSecondaryMotionSessions.Clear(); foreach (var item in sessions) importedSecondaryMotionSessions.Add(item.Key, item.Value);
            SelectSecondaryMotionForActiveGraph();
            Refresh(); SetStatus("共通揺れ設定を同じBoneIdで再bindしました。保存すると新しいskeleton/topology identityを記録します。");
        }

        MeshData CurrentSecondaryMotionMesh()
        {
            if (importedSecondaryMotionAsset == null || importedSecondaryMotionAsset.MeshTopologyHash == "") return null;
            try { return workspace.Evaluate(); }
            catch (AuthoringException)
            {
                // Keep the action usable while another rig identity is stale. This is
                // only a topology witness; final graph evaluation still belongs to the
                // rig rebind workflow.
                return workspace.Document.Objects.SelectMany(item => item.Graph == null ? Array.Empty<GraphNode>() : item.Graph.Nodes.Values)
                    .Select(node => node.SourceMesh).FirstOrDefault(mesh => mesh != null);
            }
        }

        void ClearImportedVrmSpring()
        {
            ClearImportedVrmSpringTable();
            RefreshVrmSpringStatus();
        }

        void ClearImportedSecondaryMotion()
        {
            importedSecondaryMotionSessions.Clear();
            importedSecondaryMotionDocument = null;
            importedSecondaryMotionAsset = null;
            RefreshSecondaryMotionStatus();
        }

        void RefreshSecondaryMotionAttachmentFromWorkspace()
        {
            if (workspace == null) { ClearImportedSecondaryMotion(); return; }
            var bytes = workspace.Attachments.Read(ProjectAttachments.SecondaryMotion);
            importedSecondaryMotionSessions.Clear();
            if (bytes != null && SecondaryMotionSessionsCodec.IsTable(bytes))
            {
                foreach (var item in SecondaryMotionSessionsCodec.Read(bytes)) importedSecondaryMotionSessions.Add(item.Key, item.Value);
                SelectSecondaryMotionForActiveGraph();
                return;
            }
            // Legacy projects used one unkeyed asset. Keep it available for the
            // active graph until the next save migrates it to the keyed table.
            var document = bytes == null ? null : SecondaryMotionCodec.ReadDocument(bytes);
            importedSecondaryMotionDocument = document;
            importedSecondaryMotionAsset = document?.Asset;
            string graphId = workspace.Document.ActiveObject?.Graph?.GraphId;
            if (graphId != null && importedSecondaryMotionAsset != null) importedSecondaryMotionSessions[graphId] = importedSecondaryMotionAsset;
        }

        void SelectSecondaryMotionForActiveGraph()
        {
            string graphId = workspace?.Document?.ActiveObject?.Graph?.GraphId;
            if (graphId != null && importedSecondaryMotionSessions.TryGetValue(graphId, out var asset))
            {
                importedSecondaryMotionAsset = asset;
                importedSecondaryMotionDocument = SecondaryMotionCodec.ReadDocument(SecondaryMotionCodec.Write(asset));
            }
            else
            {
                importedSecondaryMotionAsset = null;
                importedSecondaryMotionDocument = null;
            }
        }

        Dictionary<string, SecondaryMotionAsset> ReadSecondaryMotionSessions(AuthoringWorkspace value)
        {
            var result = new Dictionary<string, SecondaryMotionAsset>(StringComparer.Ordinal);
            var bytes = value?.Attachments?.Read(ProjectAttachments.SecondaryMotion);
            if (bytes != null && SecondaryMotionSessionsCodec.IsTable(bytes))
            {
                foreach (var item in SecondaryMotionSessionsCodec.Read(bytes)) result.Add(item.Key, item.Value);
                return result;
            }
            if (bytes != null)
            {
                var legacy = SecondaryMotionCodec.Read(bytes);
                string graphId = value?.Document?.ActiveObject?.Graph?.GraphId;
                if (graphId != null) result[graphId] = legacy;
            }
            return result;
        }
    }
}
