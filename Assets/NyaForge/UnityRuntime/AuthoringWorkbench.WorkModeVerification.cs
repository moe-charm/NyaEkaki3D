using System;
using System.Collections.Generic;
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
            Check(uvPanel.value && paintPanel.value && materialPanel.value, "Surface mode did not open its panels");
            ClickWorkModeButton(buttons[2]);
            Check(attachmentPanel.value && rigPanel.value, "Rig mode did not open its panels");
            ClickWorkModeButton(buttons[3]);
            Check(evidencePanel.value && validationPanel.value && projectOutputPanel.value, "Output mode did not open its panels");
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
    }
}
