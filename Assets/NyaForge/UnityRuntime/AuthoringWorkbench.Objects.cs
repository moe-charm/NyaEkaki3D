using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout objectSelectionPanel;
        VisualElement objectList;
        Toggle showAllObjects;
        Toggle referenceProtectionToggle;
        Toggle deliveryAllowlistToggle;
        TextField objectDisplayNameField;
        Button objectDisplayNameSave;
        Label objectDisplayNameStatus;
        string objectListKey = "";
        readonly HashSet<string> referenceProtectedObjectIds = new HashSet<string>(StringComparer.Ordinal);

        void BuildObjectSelection(VisualElement parent)
        {
            objectSelectionPanel = new Foldout { text = "制作対象", value = true, name = "object-selection" };
            var selectionHelp = new Label("制作対象を選ぶと、中央ビューと右側の編集内容が切り替わります。●が現在の対象です。")
            {
                name = "object-selection-help"
            };
            selectionHelp.style.whiteSpace = WhiteSpace.Normal;
            objectSelectionPanel.Add(selectionHelp);
            showAllObjects = new Toggle("他の制作対象も表示") { value = true, name = "object-show-all" };
            showAllObjects.RegisterValueChangedCallback(_ => { if (objectProjection != null) { objectProjection.Visible = showAllObjects.value; Refresh(); } });
            objectSelectionPanel.Add(showAllObjects);
            referenceProtectionToggle = new Toggle("選択中を参照として保護（編集不可）") { name = "object-reference-protection" };
            referenceProtectionToggle.tooltip = "avatarなどの基準objectを保護します。選択・表示・保存はできますが、頂点・材質・リグ・graphの変更と汎用納品出力は停止します。衣装は選択衣装skin packageで明示出力します。設定はnative projectへ保存されます。";
            referenceProtectionToggle.RegisterValueChangedCallback(e => Try(() => SetReferenceProtection(e.newValue)));
            objectSelectionPanel.Add(referenceProtectionToggle);
            deliveryAllowlistToggle = new Toggle("選択中を納品対象に含める") { name = "object-delivery-allowlist" };
            deliveryAllowlistToggle.tooltip = "汎用multi-object／GLB納品の対象を明示します。1つでも指定すると指定したobjectだけを出力し、native projectへ保存されます。空欄では全objectが候補です。";
            deliveryAllowlistToggle.RegisterValueChangedCallback(e => Try(() => SetDeliveryAllowlist(e.newValue)));
            objectSelectionPanel.Add(deliveryAllowlistToggle);
            var displayNameRow = Row(objectSelectionPanel);
            objectDisplayNameField = new TextField("表示名") { name = "object-display-name" };
            objectDisplayNameField.tooltip = "制作対象の役割名を分かりやすい名前へ変更します。空欄で自動の役割名へ戻ります。保存対象です。";
            objectDisplayNameField.style.flexGrow = 1;
            displayNameRow.Add(objectDisplayNameField);
            objectDisplayNameSave = Button("表示名を保存", SaveObjectDisplayName, "object-display-name-save");
            displayNameRow.Add(objectDisplayNameSave);
            objectDisplayNameStatus = new Label { name = "object-display-name-status" };
            objectDisplayNameStatus.style.whiteSpace = WhiteSpace.Normal;
            objectSelectionPanel.Add(objectDisplayNameStatus);
            objectList = new VisualElement { name = "object-list" };
            objectSelectionPanel.Add(objectList);
            parent.Add(objectSelectionPanel);
        }

        void RefreshObjectSelection()
        {
            if (objectSelectionPanel == null) return;
            RefreshObjectLabelsFromWorkspace();
            if (workspace?.Document?.ActiveObject?.Graph != null)
            {
                var activeGraphId = workspace.Document.ActiveObject.Graph.GraphId;
                importedRigSession = importedRigSessions.TryGetValue(activeGraphId, out var active) ? active
                    : importedRigSession?.GraphId == activeGraphId ? importedRigSession : null;
            }
            RefreshImportedVrmSessionsForActiveGraph();
            SelectSecondaryMotionForActiveGraph();
            if (workspace == null || workspace.Document.IsEmpty)
            {
                ClearFitSelectionForObjectChange("");
                referenceProtectionToggle.SetValueWithoutNotify(false); referenceProtectionToggle.SetEnabled(false);
                deliveryAllowlistToggle.SetValueWithoutNotify(false); deliveryAllowlistToggle.SetEnabled(false);
                objectDisplayNameField.SetValueWithoutNotify(""); objectDisplayNameField.SetEnabled(false);
                objectDisplayNameSave.SetEnabled(false);
                objectDisplayNameStatus.text = "対象を追加すると、ここで名前を付けられます。";
                if (objectListKey != "<empty>")
                {
                    objectList.Clear();
                    objectList.Add(new Label("制作対象はまだありません。"));
                    objectListKey = "<empty>";
                }
                return;
            }
            string activeId = workspace.Document.ActiveObjectId;
            selectionContext.SetObject(activeId);
            ClearFitSelectionForObjectChange(activeId);
            referenceProtectionToggle.SetValueWithoutNotify(referenceProtectedObjectIds.Contains(activeId));
            referenceProtectionToggle.SetEnabled(!string.IsNullOrEmpty(activeId));
            deliveryAllowlistToggle.SetValueWithoutNotify(deliveryAllowedObjectIds.Contains(activeId));
            deliveryAllowlistToggle.SetEnabled(!string.IsNullOrEmpty(activeId) && !referenceProtectedObjectIds.Contains(activeId));
            string customName;
            objectLabels.TryGetValue(activeId, out customName);
            objectDisplayNameField.SetValueWithoutNotify(customName ?? "");
            objectDisplayNameField.SetEnabled(true);
            objectDisplayNameSave.SetEnabled(true);
            objectDisplayNameStatus.text = string.IsNullOrEmpty(customName)
                ? "自動名: " + ObjectDisplayName(workspace.Document.ActiveObject) + "（空欄でこの状態）"
                : "現在の表示名: " + customName + "（内部IDはtooltipに残ります）";
            string nextObjectListKey = string.Join("|", workspace.Document.Objects.Select(item =>
                item.ObjectId + ":" + ObjectDisplayName(item))) + "|active:" + activeId;
            if (nextObjectListKey == objectListKey) return;

            objectList.Clear();
            foreach (var item in workspace.Document.Objects)
            {
                string id = item.ObjectId;
                string kind = item.IsStaticProfile ? "static" : "graph";
                string displayName = ObjectDisplayName(item);
                var button = new Button(() => Execute(AuthoringOperation.SelectObject(id)))
                {
                    text = (id == workspace.Document.ActiveObjectId ? "● " : "　") + displayName + " · " + (id.Length > 8 ? id.Substring(0, 8) : id),
                    name = "object-select-" + id
                };
                // Keep the compact label readable while ending the tooltip in
                // the complete stable identity. Existing automation and users
                // can still copy the exact object id from the hover text.
                button.tooltip = ObjectDisplayDetails(item) + "\n内部種別: " + kind + "\n" + id;
                button.SetEnabled(id != workspace.Document.ActiveObjectId);
                objectList.Add(button);
            }
            objectListKey = nextObjectListKey;
        }

        string fitSelectionOwnerObjectId = "";
        void ClearFitSelectionForObjectChange(string activeId)
        {
            if (fitSelectionOwnerObjectId == activeId) return;
            fitSelectionOwnerObjectId = activeId ?? "";
            selectedAvatarSurfaceTriangles.Clear();
            surfaceFitInspectionObjectId = "";
            surfaceFitInspectionTargetObjectId = "";
            surfaceFitInspectionStateHash = "";
            surfaceFitInspectionRevision = -1;
            surfaceFitInspectionTriangleIds = null;
            surfaceFitInspectionVertexIds = null;
            surfaceFitInspectionBehindSurfaceVertexIds = null;
            // These fields are object-domain specific. Keeping their numeric
            // IDs after switching from clothing A to B can silently apply the
            // same index to an unrelated mesh.
            accessorySurfaceTriangleIds?.SetValueWithoutNotify("");
            accessoryClothingVertexIds?.SetValueWithoutNotify("");
        }
    }
}
