using System;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout objectSelectionPanel;

        void BuildObjectSelection(VisualElement parent)
        {
            objectSelectionPanel = new Foldout { text = "制作対象", value = true, name = "object-selection" };
            objectSelectionPanel.Add(new Label("対象を切り替えると、頂点編集・材質・リグの表示先も切り替わります。"));
            parent.Add(objectSelectionPanel);
        }

        void RefreshObjectSelection()
        {
            if (objectSelectionPanel == null) return;
            while (objectSelectionPanel.childCount > 1) objectSelectionPanel.RemoveAt(1);
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
                    text = (id == workspace.Document.ActiveObjectId ? "● " : "　") + kind + " · " + id,
                    name = "object-select-" + id
                };
                button.SetEnabled(id != workspace.Document.ActiveObjectId);
                objectSelectionPanel.Add(button);
            }
        }
    }
}
