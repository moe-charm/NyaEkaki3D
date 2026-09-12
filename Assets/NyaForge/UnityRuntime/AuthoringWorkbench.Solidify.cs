using System;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        FloatField shellThickness;
        Button solidifyButton;
        void BuildSolidify(VisualElement parent)
        {
            shellThickness = Number(parent, "全体の厚み (mm)", 5, "shell-thickness");
            shellThickness.tooltip = "表示・編集段の全体に内向きの厚みを追加。平均頂点法線方式。選択面だけの操作ではありません。";
            solidifyButton = Button("全体に厚みを付ける", () => Try(() =>
            {
                if (activeEditContext == null) throw new InvalidOperationException("PolygonEditを選択してください。");
                Execute(AuthoringOperation.SolidifyPolygon(activeEditContext, shellThickness.value / 1000));
                selection.Clear(); selectedFaces.Clear(); Refresh();
            }), "solidify-polygon");
            parent.Add(solidifyButton);
        }
        void RefreshSolidify()
        {
            bool editable = activeEditContext != null && DisplayedGraphValue()?.Polygon?.Faces.Count > 0;
            shellThickness.SetEnabled(editable); solidifyButton.SetEnabled(editable);
        }
    }
}

