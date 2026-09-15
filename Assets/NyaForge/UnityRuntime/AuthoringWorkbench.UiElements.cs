using System;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Shared UI construction primitives used by every authoring panel.
        /// Keeping sizing and wrapping here prevents individual panels from
        /// drifting apart on compact or DPI-scaled Windows windows.
        /// </summary>
        static VisualElement Row(VisualElement parent)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.alignItems = Align.Center;
            row.style.width = Length.Percent(100);
            row.style.minWidth = 0;
            row.style.marginTop = 6;
            row.style.marginBottom = 6;
            parent.Add(row);
            return row;
        }

        static Button Button(string text, Action action, string name)
        {
            var button = new Button(action) { text = text, name = name };
            // UI Toolkit buttons otherwise keep an intrinsic minimum width
            // based on their Japanese caption. On a compact or DPI-scaled
            // window that pushes the caption outside the controls pane. Let
            // the button shrink and wrap; Row() can then move sibling buttons
            // to the next line when there is no room.
            button.style.minWidth = 0;
            button.style.flexShrink = 1;
            button.style.whiteSpace = WhiteSpace.Normal;
            button.style.height = StyleKeyword.Auto;
            button.style.minHeight = 36;
            button.style.paddingLeft = 8;
            button.style.paddingRight = 8;
            return button;
        }

        /// <summary>
        /// Applies the shared compact-window layout to a DropdownField whose
        /// label or selected value may be long. All panels use this helper so
        /// names remain readable at narrow widths and high DPI.
        /// </summary>
        static void ConfigureWrappedChoice(DropdownField choice, string tooltip)
        {
            if (choice == null) return;
            choice.tooltip = tooltip;
            choice.AddToClassList("wrapped-choice");
            choice.style.flexDirection = FlexDirection.Column;
            choice.style.width = Length.Percent(100);
            choice.style.minWidth = 0;
            choice.style.flexShrink = 1;
            choice.style.flexGrow = 1;
            choice.style.marginTop = 5;
            choice.style.marginBottom = 5;
            var label = choice.Q<Label>(className: "unity-base-field__label");
            if (label != null)
            {
                label.style.width = Length.Percent(100);
                label.style.minWidth = 0;
                label.style.whiteSpace = WhiteSpace.Normal;
                label.style.marginRight = 0;
            }
            var input = choice.Q<VisualElement>(className: "unity-base-popup-field__input");
            if (input != null)
            {
                input.style.width = Length.Percent(100);
                input.style.minWidth = 0;
                input.style.flexShrink = 1;
            }
            var selected = choice.Q<Label>(className: "unity-base-popup-field__text");
            if (selected != null)
            {
                selected.style.whiteSpace = WhiteSpace.Normal;
                selected.style.overflow = Overflow.Visible;
                selected.style.textOverflow = TextOverflow.Clip;
                selected.style.height = StyleKeyword.Auto;
                selected.style.minHeight = 34;
            }
        }

        static FloatField Number(VisualElement parent, string label, float value, string name)
        {
            var field = new FloatField(label) { value = value, name = name };
            field.style.minHeight = 36;
            field.style.marginTop = 4;
            parent.Add(field);
            return field;
        }
    }
}
