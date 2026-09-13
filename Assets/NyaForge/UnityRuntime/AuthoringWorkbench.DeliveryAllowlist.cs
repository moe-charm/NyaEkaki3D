using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        readonly HashSet<string> deliveryAllowedObjectIds = new HashSet<string>(StringComparer.Ordinal);

        void RefreshDeliveryAllowlistFromWorkspace()
        {
            deliveryAllowedObjectIds.Clear();
            if (workspace == null) return;
            var bytes = workspace.Attachments.Read(ProjectAttachments.DeliveryAllowlist);
            if (bytes == null) return;
            foreach (var id in DeliveryAllowlistCodec.Read(bytes)) deliveryAllowedObjectIds.Add(id);
            deliveryAllowedObjectIds.RemoveWhere(id => workspace.Document.Objects.All(item => item == null || item.ObjectId != id));
        }

        void SetDeliveryAllowlist(bool enabled)
        {
            if (workspace == null || workspace.Document.IsEmpty) return;
            string objectId = workspace.Document.ActiveObjectId;
            if (string.IsNullOrEmpty(objectId)) return;
            if (enabled && referenceProtectedObjectIds.Contains(objectId))
                throw new AuthoringException("REFERENCE_EXPORT_BLOCKED", "参照として保護したobjectは納品対象へ追加できません。衣装objectを選択してください。");
            var next = new HashSet<string>(deliveryAllowedObjectIds, StringComparer.Ordinal);
            if (enabled) next.Add(objectId); else next.Remove(objectId);
            var owned = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var name in workspace.Attachments.Hashes.Keys)
                if (name != ProjectAttachments.DeliveryAllowlist) owned[name] = workspace.Attachments.Read(name);
            if (next.Count > 0) owned[ProjectAttachments.DeliveryAllowlist] = DeliveryAllowlistCodec.Write(next);
            workspace.SetAttachmentsWithHistory(new ProjectAttachments(owned));
            RefreshDeliveryAllowlistFromWorkspace();
            Refresh();
            SetStatus(enabled ? "選択中のobjectを納品対象へ追加しました。" : "選択中のobjectを納品対象から外しました。空欄時は全objectが候補です。");
        }

        /// <summary>Returns document-order object IDs for a generic delivery.</summary>
        IReadOnlyList<string> DeliveryObjectIdsForExport()
        {
            if (workspace == null || workspace.Document?.Objects == null)
                return Array.Empty<string>();
            var selected = deliveryAllowedObjectIds.Count == 0
                ? null
                : new HashSet<string>(deliveryAllowedObjectIds, StringComparer.Ordinal);
            var result = workspace.Document.Objects
                .Where(item => item != null && (selected == null || selected.Contains(item.ObjectId)))
                .Select(item => item.ObjectId)
                .ToArray();
            if (selected != null)
            {
                var missing = selected.Where(id => !result.Contains(id, StringComparer.Ordinal)).ToArray();
                if (missing.Length > 0)
                    throw new AuthoringException("DELIVERY_ALLOWLIST_STALE", "納品対象allowlistに存在しないobjectがあります。対象を再選択してください。");
            }
            if (result.Length == 0)
                throw new AuthoringException("NO_DELIVERY_OBJECTS", "納品対象objectがありません。制作対象パネルでobjectを追加してください。");
            return result;
        }

        void EnsureGenericDeliveryExportAllowed()
        {
            var selected = DeliveryObjectIdsForExport();
            var protectedIds = new HashSet<string>(referenceProtectedObjectIds, StringComparer.Ordinal);
            protectedIds.IntersectWith(selected);
            if (protectedIds.Count > 0)
            {
                var names = protectedIds.OrderBy(id => id, StringComparer.Ordinal)
                    .Select(id => id.Length > 8 ? id.Substring(0, 8) : id);
                throw new AuthoringException("REFERENCE_EXPORT_BLOCKED",
                    "納品対象allowlistに参照object（" + string.Join(", ", names) +
                    "）が含まれています。参照を外し、衣装objectだけを納品対象にしてください。");
            }
        }
    }
}
