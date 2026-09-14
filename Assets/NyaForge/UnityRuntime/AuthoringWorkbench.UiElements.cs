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
