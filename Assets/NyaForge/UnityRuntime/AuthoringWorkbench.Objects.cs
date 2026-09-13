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
        Toggle showAllObjects;
        Toggle referenceProtectionToggle;
        readonly HashSet<string> referenceProtectedObjectIds = new HashSet<string>(StringComparer.Ordinal);

        void BuildObjectSelection(VisualElement parent)
        {
            objectSelectionPanel = new Foldout { text = "制作対象", value = true, name = "object-selection" };
            objectSelectionPanel.Add(new Label("対象を切り替えると、頂点編集・材質・リグの表示先も切り替わります。"));
            showAllObjects = new Toggle("他の制作対象も表示") { value = true, name = "object-show-all" };
            showAllObjects.RegisterValueChangedCallback(_ => { if (objectProjection != null) { objectProjection.Visible = showAllObjects.value; Refresh(); } });
            objectSelectionPanel.Add(showAllObjects);
            referenceProtectionToggle = new Toggle("選択中を参照として保護（編集不可）") { name = "object-reference-protection" };
            referenceProtectionToggle.tooltip = "avatarなどの基準objectを保護します。選択・表示・保存はできますが、頂点・材質・リグ・graphの変更と汎用納品出力は停止します。衣装は選択衣装skin packageで明示出力します。設定はnative projectへ保存されます。";
            referenceProtectionToggle.RegisterValueChangedCallback(e => Try(() => SetReferenceProtection(e.newValue)));
            objectSelectionPanel.Add(referenceProtectionToggle);
            parent.Add(objectSelectionPanel);
        }

        void RefreshObjectSelection()
        {
            if (objectSelectionPanel == null) return;
            if (workspace?.Document?.ActiveObject?.Graph != null)
            {
                var activeGraphId = workspace.Document.ActiveObject.Graph.GraphId;
                importedRigSession = importedRigSessions.TryGetValue(activeGraphId, out var active) ? active
                    : importedRigSession?.GraphId == activeGraphId ? importedRigSession : null;
            }
            RefreshImportedVrmSessionsForActiveGraph();
            SelectSecondaryMotionForActiveGraph();
            while (objectSelectionPanel.childCount > 3) objectSelectionPanel.RemoveAt(3);
            if (workspace == null || workspace.Document.IsEmpty)
            {
                referenceProtectionToggle.SetValueWithoutNotify(false); referenceProtectionToggle.SetEnabled(false);
                objectSelectionPanel.Add(new Label("制作対象はまだありません。"));
                return;
            }
            string activeId = workspace.Document.ActiveObjectId;
            referenceProtectionToggle.SetValueWithoutNotify(referenceProtectedObjectIds.Contains(activeId));
            referenceProtectionToggle.SetEnabled(!string.IsNullOrEmpty(activeId));
            foreach (var item in workspace.Document.Objects)
            {
                string id = item.ObjectId;
                string kind = item.IsStaticProfile ? "static" : "graph";
                var button = new Button(() => Execute(AuthoringOperation.SelectObject(id)))
                {
                    text = (id == workspace.Document.ActiveObjectId ? "● " : "　") + kind + " · " + (id.Length > 8 ? id.Substring(0, 8) : id),
                    name = "object-select-" + id
                };
                button.tooltip = kind + " · " + id;
                button.SetEnabled(id != workspace.Document.ActiveObjectId);
                objectSelectionPanel.Add(button);
            }
        }
    }
}
