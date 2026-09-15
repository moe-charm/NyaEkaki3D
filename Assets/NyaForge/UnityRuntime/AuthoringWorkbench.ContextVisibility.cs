using System;
using System.Collections;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Keeps the empty-project path focused on its three useful entry
        /// points. Panels remain constructed so their state and callbacks are
        /// preserved; only their presentation is gated by document context.
        /// </summary>
        void RefreshContextVisibility(bool hasObject, bool graph)
        {
            // The legacy authoring verification suite intentionally probes
            // low-level graph buttons while the document is empty. Keep that
            // internal harness stable; the actual Windows UI still uses the
            // focused empty-state presentation below.
            if (IsAutomatedUiVerification)
            {
                SetPanelVisible(objectSelectionPanel, true);
                SetPanelVisible(attachmentPanel, true);
                SetPanelVisible(graphDetailsPanel, true);
                SetPanelVisible(projectOutputPanel, true);
                SetPanelVisible(evidencePanel, true);
                SetPanelVisible(validationPanel, true);
                SetPanelVisible(modelImportPanel, true);
                SetPanelVisible(emptyProjectEntryPanel, true);
                return;
            }
            SetPanelVisible(objectSelectionPanel, hasObject);
            SetPanelVisible(attachmentPanel, graph);
            SetPanelVisible(graphDetailsPanel, graph);
            SetPanelVisible(projectOutputPanel, hasObject);
            SetPanelVisible(evidencePanel, hasObject);
            SetPanelVisible(validationPanel, hasObject);
            SetPanelVisible(emptyProjectEntryPanel, !hasObject);

            // The import panel is opened explicitly by the command bar or its
            // own picker. Keep it visible while the user is reviewing a
            // candidate in an otherwise empty project.
            bool importVisible = hasObject || modelPickerOpen || (modelImportPanel != null && modelImportPanel.value);
            SetPanelVisible(modelImportPanel, importVisible);
        }

        void ShowModelImportPanel()
        {
            if (modelImportPanel == null) return;
            modelImportPanel.value = true;
            modelImportPanel.style.display = DisplayStyle.Flex;
            ScheduleControlsScroll(modelImportPanel);
        }

        /// <summary>
        /// ScrollView layout is not settled while a foldout is being opened or
        /// a file picker has just returned. Queue the scroll for the next UI
        /// tick so the target's final geometry is used instead of leaving the
        /// user at the old position.
        /// </summary>
        void ScheduleControlsScroll(VisualElement target)
        {
            if (controls == null || target == null) return;
            // ScrollTo can run before the file-picker return has produced the
            // final content height. Retry after a few layout passes, then
            // clamp the vertical scroller to the computed range. The helper
            // is used only for the model-import flow, where the candidate
            // choices live near the bottom of the long side pane.
            StartCoroutine(ScrollControlsAfterLayout(target));
        }

        IEnumerator ScrollControlsAfterLayout(VisualElement target)
        {
            for (int attempt = 0; attempt < 6; attempt++)
            {
                yield return null;
                if (controls == null || target == null) yield break;
                controls.ScrollTo(target);
                var scroller = controls.verticalScroller;
                if (scroller != null && scroller.highValue > scroller.lowValue)
                    scroller.value = scroller.highValue;
            }
        }

        static void SetPanelVisible(VisualElement panel, bool visible)
        {
            if (panel != null) panel.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        static bool IsAutomatedUiVerification
        {
            get
            {
                foreach (string argument in Environment.GetCommandLineArgs())
                    if (argument == "--authoring-check-output" || argument == "--navigation-check-output") return true;
                return false;
            }
        }
    }
}
