using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// The shape panel is refreshed after commands and selection changes.
        /// Verify that this presentation refresh does not discard dimensions
        /// already entered by the user, while an explicit preset change still
        /// loads that preset's documented defaults.
        /// </summary>
        void VerifyShapeCreationInputs(List<string> checks)
        {
            var previous = workspace;
            string previousPath = savedDirectory;
            try
            {
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
                Check(shapeCreationPanel != null && shapePresetChoice != null && shapeCreateButton != null,
                    "Shape creation panel controls are missing");

                shapePrimarySize.SetValueWithoutNotify(73);
                shapeSecondarySize.SetValueWithoutNotify(11);
                shapeThickness.SetValueWithoutNotify(5);
                shapeSegments.SetValueWithoutNotify(28);
                Refresh();
                Check(Math.Abs(shapePrimarySize.value - 73) < .001f &&
                    Math.Abs(shapeSecondarySize.value - 11) < .001f &&
                    Math.Abs(shapeThickness.value - 5) < .001f &&
                    shapeSegments.value == 28,
                    "Shape dimensions were reset by a presentation refresh");

                shapePresetChoice.value = "バンド（手首カフ）";
                Check(Math.Abs(shapePrimarySize.value - 40) < .001f &&
                    Math.Abs(shapeSecondarySize.value - 35) < .001f &&
                    Math.Abs(shapeThickness.value - 4) < .001f &&
                    shapeSegments.value == 32,
                    "Changing the shape preset did not load its documented defaults");

                shapePrimarySize.SetValueWithoutNotify(46);
                shapeSecondarySize.SetValueWithoutNotify(21);
                shapeThickness.SetValueWithoutNotify(3);
                shapeSegments.SetValueWithoutNotify(36);
                Refresh();
                Check(Math.Abs(shapePrimarySize.value - 46) < .001f &&
                    Math.Abs(shapeSecondarySize.value - 21) < .001f &&
                    Math.Abs(shapeThickness.value - 3) < .001f &&
                    shapeSegments.value == 36,
                    "Custom cuff dimensions were reset by a later refresh");
                checks.Add("shape creation inputs: custom dimensions survive refresh and preset changes restore only the selected preset defaults");
            }
            finally
            {
                ReplaceWorkspace(previous, previousPath);
            }
        }
    }
}
