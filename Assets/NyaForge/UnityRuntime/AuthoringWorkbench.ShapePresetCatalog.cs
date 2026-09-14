using System;
using System.Collections.Generic;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// User-facing primitive definitions. Keeping labels, defaults and
        /// creation callbacks together prevents the shape panel from growing
        /// another index-based condition whenever a primitive is added.
        /// </summary>
        sealed class ShapePresetDefinition
        {
            internal readonly string Id;
            internal readonly string DisplayName;
            internal readonly string SecondaryLabel;
            internal readonly string HelpText;
            internal readonly float PrimaryDefault;
            internal readonly float SecondaryDefault;
            internal readonly float ThicknessDefault;
            internal readonly int SegmentDefault;
            internal readonly bool HasThickness;
            internal readonly Action<AuthoringWorkbench, float, float, float, int> Create;

            internal ShapePresetDefinition(string id, string displayName, string secondaryLabel, string helpText,
                float primaryDefault, float secondaryDefault, float thicknessDefault, int segmentDefault,
                bool hasThickness, Action<AuthoringWorkbench, float, float, float, int> create)
            {
                Id = id; DisplayName = displayName; SecondaryLabel = secondaryLabel; HelpText = helpText;
                PrimaryDefault = primaryDefault; SecondaryDefault = secondaryDefault;
                ThicknessDefault = thicknessDefault; SegmentDefault = segmentDefault;
                HasThickness = hasThickness; Create = create;
            }
        }

        static readonly IReadOnlyList<ShapePresetDefinition> ShapePresets = new[]
        {
            new ShapePresetDefinition(
                "choker-ring", "リング（チョーカー）", "管の太さ (mm)",
                "リング: 半径 + 管の太さ。首周りへ合わせる位置・装着・ウェイトは追加後に調整します。",
                60, 8, 4, 24, false,
                (owner, primary, secondary, thickness, segments) => owner.CreateChokerGraph(primary, secondary, segments)),
            new ShapePresetDefinition(
                "wrist-cuff", "バンド（手首カフ）", "幅 (mm)",
                "バンド: 内半径 + 幅 + 厚み。手首周りへ合わせる位置・装着・ウェイトは追加後に調整します。",
                40, 35, 4, 32, true,
                (owner, primary, secondary, thickness, segments) => owner.CreateCuffGraph(primary, secondary, thickness, segments))
        };

        ShapePresetDefinition SelectedShapePreset()
        {
            int index = shapePresetChoice == null ? 0 : shapePresetChoice.index;
            return ShapePresets[Math.Max(0, Math.Min(index, ShapePresets.Count - 1))];
        }
    }
}
