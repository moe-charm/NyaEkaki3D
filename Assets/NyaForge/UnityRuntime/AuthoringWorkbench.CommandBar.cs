using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button commandBarSave;
        Button commandBarUndo;
        Button commandBarRedo;
        Button commandBarModel;
        Button commandBarShape;
        Label commandBarContext;

        /// <summary>Stable primary actions that remain reachable above the long settings scroll view.</summary>
        void BuildCommandBar(VisualElement parent)
        {
            var bar = new VisualElement { name = "authoring-command-bar" };
            bar.AddToClassList("authoring-command-bar");
            commandBarContext = new Label("制作対象: 未選択") { name = "authoring-command-context" };
            commandBarContext.AddToClassList("authoring-command-context");
            bar.Add(commandBarContext);
            commandBarModel = Button("モデルを追加", OpenModelImportFromCommandBar, "command-add-model");
            commandBarShape = Button("基本形状を追加", FocusShapeCreationFromCommandBar, "command-add-shape");
            commandBarSave = Button("保存", SaveProject, "command-save");
            commandBarUndo = Button("元に戻す", () => Execute(AuthoringOperation.Undo()), "command-undo");
            commandBarRedo = Button("やり直す", () => Execute(AuthoringOperation.Redo()), "command-redo");
            bar.Add(commandBarModel); bar.Add(commandBarShape); bar.Add(commandBarSave); bar.Add(commandBarUndo); bar.Add(commandBarRedo);
            parent.Add(bar);
        }

        void OpenModelImportFromCommandBar()
        {
            ShowModelImportPanel();
            if (!modelPickerOpen) StartCoroutine(PickModel());
        }

        void FocusShapeCreationFromCommandBar()
        {
            if (shapeCreationPanel == null || controls == null) return;
            shapeCreationPanel.value = true;
            controls.ScrollTo(shapeCreationPanel);
        }

        void RefreshCommandBar()
        {
            if (commandBarContext == null || workspace == null) return;
            var document = workspace.Document;
            if (document.IsEmpty)
            {
                commandBarContext.text = "制作対象: 未選択（モデルまたは基本形状を追加）";
                commandBarContext.tooltip = "モデルまたは基本形状を追加すると、制作対象の名前と役割がここに表示されます。";
            }
            else
            {
                var active = document.ActiveObject;
                var kind = active.IsStaticProfile ? "モデル" : "制作物";
                commandBarContext.text = "制作対象: " + ObjectDisplayName(active) + " · " + kind + " · " + ShortId(document.ActiveObjectId) +
                    (workspace.IsDirty ? " · 未保存" : " · 保存済み");
                commandBarContext.tooltip = ObjectDisplayDetails(active) + "\n内部ID " + document.ActiveObjectId;
            }
            bool canEdit = !document.IsEmpty && !document.ActiveObject.IsStaticProfile;
            commandBarModel.SetEnabled(!modelPickerOpen && (document.IsEmpty || canEdit));
            commandBarShape.SetEnabled(document.IsEmpty || canEdit);
            commandBarSave.SetEnabled(!document.IsEmpty && !saveIncomplete);
            commandBarUndo.SetEnabled(workspace.CanUndo);
            commandBarRedo.SetEnabled(workspace.CanRedo);
        }

        static string ShortId(string id)
        {
            if (string.IsNullOrEmpty(id)) return "未選択";
            return id.Length <= 12 ? id : id.Substring(0, 12) + "…";
        }
    }
}
