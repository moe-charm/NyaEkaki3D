using System;
using System.Collections.Generic;
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
            shapePresetChoice = new DropdownField("種類", new List<string> { "リング（チョーカー）", "バンド（手首カフ）" }, 0)
            {
                name = "shape-preset"
            };
            shapePresetChoice.tooltip = "チョーカーとカフは基本形状です。アバターへ自動装着する機能ではありません。";
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
            bool cuff = shapePresetChoice.index == 1;
            shapeSecondarySize.label = cuff ? "幅 (mm)" : "管の太さ (mm)";
            shapeThickness.style.display = cuff ? DisplayStyle.Flex : DisplayStyle.None;
            shapeSegments.SetValueWithoutNotify(cuff ? 32 : 24);
            shapeCreationHelp.text = cuff
                ? "バンド: 内半径 + 幅 + 厚み。手首周りへ合わせる位置・装着・ウェイトは追加後に調整します。"
                : "リング: 半径 + 管の太さ。首周りへ合わせる位置・装着・ウェイトは追加後に調整します。";
            bool available = workspace != null && (workspace.Document.IsEmpty || IsGraph);
            shapeCreateButton?.SetEnabled(available);
        }

        void CreateConfiguredShape()
        {
            if (shapePresetChoice.index == 1)
            {
                CreateCuffGraph(shapePrimarySize.value / 1000f, shapeSecondarySize.value / 1000f,
                    shapeThickness.value / 1000f, shapeSegments.value);
            }
            else
            {
                CreateChokerGraph(shapePrimarySize.value / 1000f, shapeSecondarySize.value / 1000f, shapeSegments.value);
            }
        }
    }
}
