using System;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout uvPanel;
        UvPreview uvPreview;
        Button uvProject;
        Label uvInfo;

        void BuildUv(VisualElement parent)
        {
            uvPanel = new Foldout { text = "UV投影と確認", value = false, name = "uv-panel" };
            uvPanel.RegisterValueChangedCallback(e=> { if(!e.newValue) uvPreview?.CancelIslandDrag(); });
            parent.Add(uvPanel);
            BuildUvDependencies(uvPanel);
            uvProject = Button("全ての面をUV投影", () => Try(() =>
            {
                if (activeEditContext == null) throw new InvalidOperationException("PolygonEditを選択してください。");
                Execute(AuthoringOperation.ProjectPolygonUv(activeEditContext));
            }), "project-uv");
            uvPanel.Add(uvProject);
            uvInfo = new Label(); uvInfo.style.whiteSpace = WhiteSpace.Normal; uvPanel.Add(uvInfo);
            uvPreview = new UvPreview(); uvPanel.Add(uvPreview);
            uvPanel.Add(Button("UV表示をリセット",uvPreview.ResetView,"reset-uv-view"));
            BuildUvEditing(uvPanel);
        }

        void RefreshUv()
        {
            RefreshUvDependencies();
            var polygon = DisplayedGraphValue()?.Polygon;
            uvProject.SetEnabled(activeEditContext != null && polygon?.Faces.Count > 0);
            uvPreview.Bind(polygon, selectedFaces);
            uvTransformButton.SetEnabled(activeEditContext != null && polygon?.Faces.Count > 0 && selectedFaces.Count > 0);
            uvInfo.text = polygon == null ? "PolygonのUVを表示します。" :
                "UV / " + polygon.Faces.Count + " 面。左ドラッグで島を移動、Escで取消。Shiftで選択追加／解除。ホイール拡大、右・中ドラッグは表示移動。格子0〜1、先頭2048面まで。";
        }
    }
}

