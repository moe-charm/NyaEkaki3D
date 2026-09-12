using System;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout objectSelectionPanel;
        Toggle showAllObjects;

        void BuildObjectSelection(VisualElement parent)
        {
            objectSelectionPanel = new Foldout { text = "制作対象", value = true, name = "object-selection" };
            objectSelectionPanel.Add(new Label("対象を切り替えると、頂点編集・材質・リグの表示先も切り替わります。"));
            showAllObjects = new Toggle("他の制作対象も表示") { value = true, name = "object-show-all" };
            showAllObjects.RegisterValueChangedCallback(_ => { if (objectProjection != null) { objectProjection.Visible = showAllObjects.value; Refresh(); } });
            objectSelectionPanel.Add(showAllObjects);
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
            while (objectSelectionPanel.childCount > 2) objectSelectionPanel.RemoveAt(2);
            if (workspace == null || workspace.Document.IsEmpty)
            {
                objectSelectionPanel.Add(new Label("制作対象はまだありません。"));
                return;
            }
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
