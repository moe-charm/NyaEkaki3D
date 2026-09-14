using System;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout shapeCreationPanel;
        DropdownField shapePresetChoice;
        FloatField shapePrimarySize;
        FloatField shapeSecondarySize;
        FloatField shapeThickness;
        IntegerField shapeSegments;
        Label shapeCreationHelp;
        Button shapeCreateButton;

        /// <summary>Human-facing entry point for the reusable primitive generators.</summary>
        void BuildShapeCreationPanel(VisualElement parent)
        {
            shapeCreationPanel = new Foldout { text = "基本形状を追加", value = true, name = "shape-creation" };
            shapeCreationPanel.Add(new Label("寸法を指定して、編集可能な形状を1つ追加します。追加後は形状編集へ進みます。"));
            var choices = new System.Collections.Generic.List<string>();
            foreach (var preset in ShapePresets) choices.Add(preset.DisplayName);
            shapePresetChoice = new DropdownField("種類", choices, 0)
            {
                name = "shape-preset"
            };
            shapePresetChoice.tooltip = "基本形状を選びます。アバターへ自動装着する機能ではありません。";
            shapePresetChoice.RegisterValueChangedCallback(_ => RefreshShapeCreationPanel());
            shapeCreationPanel.Add(shapePresetChoice);

            shapePrimarySize = ShapeSize(shapeCreationPanel, "半径 (mm)", 60, "shape-primary-size");
            shapeSecondarySize = ShapeSize(shapeCreationPanel, "管の太さ (mm)", 8, "shape-secondary-size");
            shapeThickness = ShapeSize(shapeCreationPanel, "厚み (mm)", 4, "shape-thickness");
            shapeSegments = new IntegerField("分割数") { value = 24, name = "shape-segments" };
            shapeSegments.style.minHeight = 36;
            shapeSegments.tooltip = "形状の周方向の分割数。増やすほど滑らかになりますが、頂点数も増えます。";
            shapeCreationPanel.Add(shapeSegments);

            shapeCreationHelp = new Label { name = "shape-creation-help" };
            shapeCreationHelp.style.whiteSpace = WhiteSpace.Normal;
            shapeCreationPanel.Add(shapeCreationHelp);
            shapeCreateButton = Button("この寸法で形状を追加", () => Try(CreateConfiguredShape), "shape-create");
            shapeCreationPanel.Add(shapeCreateButton);
            parent.Add(shapeCreationPanel);
            RefreshShapeCreationPanel();
        }

        static FloatField ShapeSize(VisualElement parent, string label, float value, string name)
        {
            var field = new FloatField(label) { value = value, name = name };
            field.style.minHeight = 36;
            field.tooltip = "ミリメートルで入力します。内部ではメートルへ変換して保存します。";
            parent.Add(field);
            return field;
        }

        void RefreshShapeCreationPanel()
        {
            if (shapePresetChoice == null) return;
            var preset = SelectedShapePreset();
            shapePrimarySize.SetValueWithoutNotify(preset.PrimaryDefault);
            shapeSecondarySize.SetValueWithoutNotify(preset.SecondaryDefault);
            shapeThickness.SetValueWithoutNotify(preset.ThicknessDefault);
            shapeSecondarySize.label = preset.SecondaryLabel;
            shapeThickness.style.display = preset.HasThickness ? DisplayStyle.Flex : DisplayStyle.None;
            shapeSegments.SetValueWithoutNotify(preset.SegmentDefault);
            shapeCreationHelp.text = preset.HelpText;
            bool available = workspace != null && (workspace.Document.IsEmpty || IsGraph);
            shapeCreateButton?.SetEnabled(available);
        }

        void CreateConfiguredShape()
        {
            var preset = SelectedShapePreset();
            preset.Create(this, shapePrimarySize.value / 1000f, shapeSecondarySize.value / 1000f,
                shapeThickness.value / 1000f, shapeSegments.value);
        }
    }
}
