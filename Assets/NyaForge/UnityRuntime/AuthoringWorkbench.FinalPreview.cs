using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Toggle finalPreview;

        void BuildFinalPreview(VisualElement parent)
        {
            finalPreview = new Toggle("最終結果も表示（灰色・確認用）") { value = true, name = "show-final-result" };
            finalPreview.style.whiteSpace = WhiteSpace.Normal;
            parent.Add(finalPreview);
            finalPreview.RegisterValueChangedCallback(e => Try(() =>
            {
                bool previous = projection.ShowFinalResult;
                projection.ShowFinalResult = e.newValue;
                try { using (var prepared = projection.PrepareGraph(workspace.Document, workspace.Preview)) prepared.Commit(); }
                catch
                {
                    projection.ShowFinalResult = previous;
                    finalPreview.SetValueWithoutNotify(previous);
                    throw;
                }
                Refresh();
            }));
        }
    }
}
