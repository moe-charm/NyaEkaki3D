using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        VisualElement paintRebindPanel;
        Label paintRebindInfo;
        Button paintRebindButton;
        PaintRebindContext reviewedPaintBinding;

        void BuildPaintRebind(VisualElement parent)
        {
            paintRebindPanel = new VisualElement(); parent.Add(paintRebindPanel);
            paintRebindInfo = new Label(); paintRebindInfo.style.whiteSpace = WhiteSpace.Normal;
            paintRebindPanel.Add(paintRebindInfo);
            paintRebindButton = Button("旧画像を現在のUVに割り当てる", () => Try(() =>
            {
                paintCanvas.CancelStroke();
                Execute(AuthoringOperation.RebindPaintImage(reviewedPaintBinding));
            }), "paint-rebind");
            paintRebindPanel.Add(paintRebindButton);
        }

        void RefreshPaintRebind(GraphNode node, GraphImageValue image)
        {
            reviewedPaintBinding = null;
            bool unresolved = node?.PaintImage != null && image == null;
            paintRebindPanel.style.display = unresolved ? DisplayStyle.Flex : DisplayStyle.None;
            if (unresolved)
            {
                try { reviewedPaintBinding = PaintRebinding.Context(workspace.Document.Objects[0].Graph, node.NodeId); }
                catch (AuthoringException) { /* Missing/unsupported input must stay unresolved. */ }
                paintRebindInfo.text = reviewedPaintBinding == null ?
                    "旧画像は保存されています。先に上流のメッシュとUVの問題を解決してください。" :
                    "画像の画素を変えずに、現在のUVへ割り当て直します。模様の位置や形は変わります。再投影ではありません。元の対応へ戻すにはUndoを使ってください。";
            }
            paintRebindButton.SetEnabled(reviewedPaintBinding != null);
        }
    }
}
