using System.Linq;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Label uvPaintDependencies;
        void BuildUvDependencies(VisualElement parent)
        {
            uvPaintDependencies = new Label { name = "uv-paint-dependencies" };
            uvPaintDependencies.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(uvPaintDependencies);
        }
        void RefreshUvDependencies()
        {
            var affected = activeEditContext == null ? new string[0] :
                PaintDependencies.DownstreamImages(workspace.Document.ActiveObject.Graph,activeEditContext.NodeId).ToArray();
            uvPaintDependencies.style.display = affected.Length == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            uvPaintDependencies.text = affected.Length == 0 ? "" :
                "UV変更の影響候補: " + affected.Length + "個の保存済みPaint（" +
                string.Join("、", affected.Select(id => id.Substring(0,8))) +
                "）。下流のUV対応が変わると描画は未解決になります。旧画像は保持されます。Undoで戻すか、色塗り欄で明示的に割り当て直してください。";
        }
    }
}
