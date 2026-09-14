using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>Short user-facing workflow text kept outside the import implementation.</summary>
        void BuildModelImportGuidance(VisualElement parent)
        {
            var guidance = new Label(
                "取込の流れ\n" +
                "① ファイルを選ぶ → ②候補を確認 → ③取り込む\n" +
                "アバターはskin付き、髪・小物はskinなしの候補を選びます。\n" +
                "迷ったら「全meshをまとめて取り込む」で一度に確認できます。")
            {
                name = "model-import-guidance"
            };
            guidance.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(guidance);
        }
    }
}
