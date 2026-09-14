using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        readonly Dictionary<string, string> objectLabels = new Dictionary<string, string>(StringComparer.Ordinal);
        string objectLabelsAttachmentHash = "";
        AuthoringWorkspace objectLabelsWorkspace;

        void RefreshObjectLabelsFromWorkspace()
        {
            string hash = workspace?.Attachments?.Hashes != null && workspace.Attachments.Hashes.TryGetValue(ProjectAttachments.ObjectLabels, out var value) ? value : "";
            if (workspace == objectLabelsWorkspace && hash == objectLabelsAttachmentHash) return;
            objectLabelsWorkspace = workspace;
            objectLabelsAttachmentHash = hash;
            objectLabels.Clear();
            if (string.IsNullOrEmpty(hash)) return;
            try
            {
                var stored = ObjectLabelsCodec.Read(workspace.Attachments.Read(ProjectAttachments.ObjectLabels));
                foreach (var item in stored) objectLabels[item.Key] = item.Value;
            }
            catch (Exception error)
            {
                objectLabelsAttachmentHash = "";
                SetStatus("表示名metadataを読み込めません: " + error.Message);
            }
        }

        void SaveObjectDisplayName()
        {
            if (workspace?.Document == null || workspace.Document.IsEmpty) return;
            Try(() =>
            {
                string objectId = workspace.Document.ActiveObjectId;
                string name = (objectDisplayNameField.value ?? "").Trim();
                var next = new Dictionary<string, string>(objectLabels, StringComparer.Ordinal);
                if (string.IsNullOrEmpty(name)) next.Remove(objectId);
                else { Checks.Name(name); next[objectId] = name; }
                var owned = new Dictionary<string, byte[]>(StringComparer.Ordinal);
                foreach (var key in workspace.Attachments.Hashes.Keys)
                    if (key != ProjectAttachments.ObjectLabels) owned[key] = workspace.Attachments.Read(key);
                if (next.Count > 0) owned[ProjectAttachments.ObjectLabels] = ObjectLabelsCodec.Write(next);
                workspace.SetAttachmentsWithHistory(new ProjectAttachments(owned));
                objectLabelsAttachmentHash = "";
                Refresh();
                SetStatus(string.IsNullOrEmpty(name) ? "表示名を自動名へ戻しました。" : "表示名を保存しました。Undoで戻せます。");
            });
        }

        /// <summary>
        /// Keeps object-list wording in one place. ObjectId remains the stable
        /// identity; this label is only a human-facing role and can evolve
        /// without changing the native project format.
        /// </summary>
        string ObjectDisplayName(AuthoringObject item)
        {
            if (item == null) return "制作対象";
            if (objectLabels.TryGetValue(item.ObjectId, out var customName) && !string.IsNullOrWhiteSpace(customName)) return customName;
            if (item.IsStaticProfile) return "メッシュ小物";
            var nodes = item.Graph?.Nodes.Values;
            if (nodes == null) return "制作グラフ";
            bool hasSkeleton = nodes.Any(node => node.TypeId == BuiltinNodes.Skeleton);
            bool hasAttachment = nodes.Any(node => node.TypeId == BuiltinNodes.Attachment);
            bool hasPolygon = nodes.Any(node => node.TypeId == BuiltinNodes.PolygonSource);
            bool hasDerivedSource = nodes.Any(node => node.TypeId == BuiltinNodes.DerivedSource);
            if (hasAttachment) return "装着アクセサリー";
            if (hasDerivedSource && hasSkeleton) return "衣装／スキン小物";
            if (hasSkeleton && importedVrmSessions.ContainsKey(item.Graph.GraphId)) return "アバター（表情あり）";
            if (hasSkeleton) return "スキンモデル";
            if (hasPolygon) return "基本形状";
            return "制作グラフ";
        }

        string ObjectDisplayDetails(AuthoringObject item)
        {
            if (item == null) return "";
            var id = item.ObjectId ?? "";
            var shortId = id.Length > 8 ? id.Substring(0, 8) : id;
            var graphId = item.Graph?.GraphId ?? "";
            var graphShort = graphId.Length > 8 ? graphId.Substring(0, 8) : graphId;
            return ObjectDisplayName(item) + "\nobject " + shortId +
                (string.IsNullOrEmpty(graphShort) ? "" : "\ngraph " + graphShort) +
                (item.IsStaticProfile ? "\n静的メッシュ" : "\n編集可能な制作グラフ");
        }
    }
}
