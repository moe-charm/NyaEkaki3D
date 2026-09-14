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
            row.style.marginTop = 6;
            row.style.marginBottom = 6;
            parent.Add(row);
            return row;
        }

        static Button Button(string text, Action action, string name)
            => new Button(action) { text = text, name = name };

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
