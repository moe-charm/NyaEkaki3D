using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void RefreshReferenceProtectionFromWorkspace()
        {
            referenceProtectedObjectIds.Clear();
            if (workspace == null) return;
            var bytes = workspace.Attachments.Read(ProjectAttachments.ReferenceProtection);
            if (bytes == null) return;
            foreach (var id in ReferenceProtectionCodec.Read(bytes)) referenceProtectedObjectIds.Add(id);
            referenceProtectedObjectIds.RemoveWhere(id => workspace.Document.Objects.All(item => item.ObjectId != id));
        }

        void SetReferenceProtection(bool enabled)
        {
            if (workspace == null || workspace.Document.IsEmpty) return;
            string objectId = workspace.Document.ActiveObjectId;
            if (string.IsNullOrEmpty(objectId)) return;
            if (enabled) referenceProtectedObjectIds.Add(objectId);
            else referenceProtectedObjectIds.Remove(objectId);
            if (enabled) deliveryAllowedObjectIds.Remove(objectId);

            var owned = new Dictionary<string, byte[]>(StringComparer.Ordinal);
            foreach (var name in workspace.Attachments.Hashes.Keys)
                if (name != ProjectAttachments.ReferenceProtection && name != ProjectAttachments.DeliveryAllowlist) owned[name] = workspace.Attachments.Read(name);
            if (referenceProtectedObjectIds.Count > 0)
                owned[ProjectAttachments.ReferenceProtection] = ReferenceProtectionCodec.Write(referenceProtectedObjectIds);
            if (deliveryAllowedObjectIds.Count > 0)
                owned[ProjectAttachments.DeliveryAllowlist] = DeliveryAllowlistCodec.Write(deliveryAllowedObjectIds);
            workspace.SetAttachmentsWithHistory(new ProjectAttachments(owned));
            Refresh();
            SetStatus(enabled ? "選択中のobjectを参照として保護しました。編集操作は停止します。" : "選択中objectの参照保護を解除しました。");
        }

        bool ReferenceProtectionBlocks(IEnumerable<AuthoringOperation> operations)
        {
            if (workspace == null || workspace.Document.IsEmpty) return false;
            // A single MCP batch may select a different object before editing
            // it.  Checking only Document.ActiveObjectId at the start lets a
            // batch enter through an unprotected clothing object and then
            // mutate a protected avatar.  Resolve the simulated active object
            // in operation order so every mutating operation is guarded.
            string activeObjectId = workspace.Document.ActiveObjectId;
            foreach (var operation in operations ?? Array.Empty<AuthoringOperation>())
            {
                if (operation == null) continue;
                // Object selection and history remain available so the user can
                // leave the protected avatar or undo an earlier edit.
                if (operation.Kind == "object.select")
                {
                    activeObjectId = operation.NewObjectId;
                    continue;
                }
                if (operation.Kind == "object.add_graph" || operation.Kind == "object.add_mesh")
                {
                    activeObjectId = operation.NewObjectId;
                    continue;
                }
                if (operation.Kind != "history.undo" && operation.Kind != "history.redo" &&
                    referenceProtectedObjectIds.Contains(activeObjectId)) return true;
            }
            return false;
        }

        string ReferenceProtectionMessage => "このobjectは参照として保護されています。編集するには保護を解除してください。";

    }
}
