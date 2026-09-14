using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>Verifies the four navigation buttons through the same UI hit path used by the pointer harness.</summary>
        void VerifyWorkModeNavigation(List<string> checks)
        {
            var buttons = new[]
            {
                root.Q<Button>("work-mode-shape"), root.Q<Button>("work-mode-surface"),
                root.Q<Button>("work-mode-rig"), root.Q<Button>("work-mode-output")
            };
            Check(Array.TrueForAll(buttons, button => button != null && button.enabledInHierarchy), "Work mode navigation buttons are missing");
            ClickWorkModeButton(buttons[0]);
            Check(shapeCreationPanel.value && graphDetailsPanel.value, "Shape mode did not open its panels");
            ClickWorkModeButton(buttons[1]);
            Check(uvPanel.value && paintPanel.value && materialPanel.value && !shapeCreationPanel.value && !graphDetailsPanel.value,
                "Surface mode did not open its panels or close the previous editing domain");
            ClickWorkModeButton(buttons[2]);
            Check(attachmentPanel.value && rigPanel.value && !uvPanel.value && !paintPanel.value && !materialPanel.value,
                "Rig mode did not open its panels or close the previous editing domain");
            ClickWorkModeButton(buttons[3]);
            Check(morphPanel.value && evidencePanel.value && validationPanel.value && projectOutputPanel.value && !attachmentPanel.value && !rigPanel.value,
                "Output mode did not open its panels or close the previous editing domain");
            // The remaining automated checks intentionally probe low-level
            // controls directly. Restore the expanded harness presentation
            // after verifying the user-facing collapse behavior.
            if (IsAutomatedUiVerification)
                foreach (var panel in new[] { shapeCreationPanel, graphDetailsPanel, uvPanel, paintPanel, materialPanel, attachmentPanel, rigPanel, evidencePanel, validationPanel, projectOutputPanel, morphPanel })
                    if (panel != null) panel.value = true;
            checks.Add("work-mode navigation: shape, UV/color, rig/attachment and review/output buttons open the existing panels through pointer hit testing");
        }

        void ClickWorkModeButton(Button button)
        {
            // The navigator lives inside the long controls ScrollView. Bring
            // each target into view before probing its real panel hit path;
            // forcing offset zero made this regression depend on content
            // height and fail whenever the button was below the fold.
            controls.ScrollTo(button);
            PointerProbe.Click(button);
        }

        void VerifyMorphTargetIdentity(List<string> checks)
        {
            var previousWorkspace = workspace;
            string previousPath = projectPath == null ? null : projectPath.value;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                graphCanvas.AddMorphSampleForVerification();
                Refresh();
                Check(morphTargetIds.Count == 1 && morphTargetChoice != null,
                    "Morph sample did not publish its target to the authoring panel");
                string targetId = morphTargetIds[0];
                Check(morphTargetChoice.tooltip.Contains(targetId, StringComparison.Ordinal),
                    "Morph target tooltip did not expose the complete target ID");
                string before = workspace.Document.StateHash;
                morphTargetChoice.value = morphTargetChoice.choices[0];
                Check(morphTargetChoice.tooltip.Contains(targetId, StringComparison.Ordinal) && workspace.Document.StateHash == before,
                    "Selecting a Morph target changed the document or lost its complete ID tooltip");
                checks.Add("Morph target selection exposes the complete stable ID in a tooltip without changing the document");
            }
            finally
            {
                ReplaceWorkspace(previousWorkspace, previousPath);
            }
        }
    }
}
