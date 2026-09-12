using System;
using System.Linq;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        ImportedRigSession importedRigSession;
        readonly Dictionary<string, ImportedRigSession> importedRigSessions = new Dictionary<string, ImportedRigSession>(StringComparer.Ordinal);
        Label importedRigStatus;

        void BuildImportedRigStatus(VisualElement parent)
        {
            importedRigStatus = new Label { name = "imported-rig-status" };
            importedRigStatus.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(importedRigStatus);
        }

        void RefreshImportedRigStatus()
        {
            if (importedRigStatus == null) return;
            if (workspace?.Document?.ActiveObject?.Graph != null)
            {
                var activeGraphId = workspace.Document.ActiveObject.Graph.GraphId;
                importedRigSession = importedRigSessions.TryGetValue(activeGraphId, out var active) ? active
                    : importedRigSession?.GraphId == activeGraphId ? importedRigSession : null;
            }
            if (importedRigSession == null) { importedRigStatus.text = "取込骨対応: なし"; return; }
            var graph = workspace?.Document.Objects.Select(item => item.Graph).FirstOrDefault(value => value != null && value.GraphId == importedRigSession.GraphId);
            try
            {
                importedRigSession.ResolveHumanoid(graph);
                importedRigStatus.text = "取込骨対応: " + importedRigSession.NodeToBone.Count + " bone · humanoid " + importedRigSession.HumanoidNodes.Count;
                if (importedRigSession.MeshInstanceTransform != null) importedRigStatus.text += " · node affine保存済み";
                if (importedRigSession.SourceNodeOrigins == null) importedRigStatus.text += " 元node座標なし：座標変換には再取込が必要です。";
            }
            catch (AuthoringException error)
            {
                importedRigStatus.text = "取込骨対応: 更新が必要です（" + error.Code + "）。元データと現在の骨格を確認してください。";
            }
        }
    }
}
