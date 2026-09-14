using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// A small navigation strip for the long right-hand settings pane.
        /// It changes scroll/foldout state only; the underlying editing command
        /// and selection state remain shared with MCP and the existing panels.
        /// </summary>
        void BuildWorkModeNavigator(VisualElement parent)
        {
            var panel = new VisualElement { name = "work-mode-navigator" };
            panel.Add(new Label("作業モード（クリックで該当設定へ移動）"));
            var row = Row(panel);
            row.Add(Button("形状編集", () => FocusWorkMode("形状編集", shapeCreationPanel, graphDetailsPanel), "work-mode-shape"));
            row.Add(Button("UV・色", () => FocusWorkMode("UV・色", uvPanel, paintPanel, materialPanel), "work-mode-surface"));
            row.Add(Button("装着・骨", () => FocusWorkMode("装着・骨", attachmentPanel, rigPanel), "work-mode-rig"));
            row.Add(Button("確認・出力", () => FocusWorkMode("確認・出力", morphPanel, evidencePanel, validationPanel, projectOutputPanel), "work-mode-output"));
            parent.Add(panel);
        }

        void FocusWorkMode(string name, params Foldout[] panels)
        {
            // Keep one editing domain open at a time. The panels remain
            // available in the same order, but switching modes no longer
            // leaves every long section expanded in the controls column.
            var allPanels = new[]
            {
                shapeCreationPanel, graphDetailsPanel, uvPanel, paintPanel,
                materialPanel, attachmentPanel, rigPanel, evidencePanel,
                validationPanel, projectOutputPanel, morphPanel
            };
            foreach (var candidate in allPanels)
            {
                if (candidate == null) continue;
                bool keepOpen = false;
                foreach (var panel in panels ?? new Foldout[0])
                    if (ReferenceEquals(candidate, panel)) { keepOpen = true; break; }
                if (!keepOpen) candidate.value = false;
            }
            Foldout target = null;
            foreach (var panel in panels ?? new Foldout[0])
            {
                if (panel == null) continue;
                panel.value = true;
                if (target == null) target = panel;
            }
            if (target != null && controls != null) controls.ScrollTo(target);
            SetStatus(name + "の設定を表示しました。対象と編集段を確認して操作してください。");
        }
    }
}
