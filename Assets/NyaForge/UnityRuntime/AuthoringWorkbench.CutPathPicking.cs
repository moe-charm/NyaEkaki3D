using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Toggle cutPathPickMode;
        bool CutPathPicking=>cutPathPickMode!=null && cutPathPickMode.value && cutPathPanel.value && activeEditContext!=null;
        void BuildCutPathPicking(VisualElement parent)
        {
            cutPathPickMode=new Toggle("画面の辺をクリックして追加") { name="cut-path-pick-mode" };parent.Add(cutPathPickMode);
            var help=new Label("クリック位置を経路へ追加。左ドラッグで回転、右で移動。Escapeで指定モード終了。");help.style.whiteSpace=WhiteSpace.Normal;parent.Add(help);
            BuildCutVisibility(parent);BuildCutPathHover(parent);
            cutPathPickMode.RegisterValueChangedCallback(e=>
            {
                ClearCutPathHover();
                if(e.newValue && surfacePaintMode!=null) surfacePaintMode.value=false;
                RefreshViewportHint();
                SetStatus(e.newValue ? "切断位置指定：辺をクリック。確定ボタンで反映、Escapeで指定モード終了。" : "切断位置指定を終了。経路は確定まで保持します。");
            });
            view.RegisterCallback<KeyDownEvent>(e=>
            {
                if(e.keyCode!=KeyCode.Escape || !CutPathPicking) return;
                cutPathPickMode.value=false;e.StopPropagation();
            });
        }
        void PickCutPathPoint(Vector2 panelPosition)=>Try(()=>
        {
            var value=DisplayedGraphValue();if(value?.Polygon==null) return;
            var hit=FindCutPathHit(panelPosition);
            if(hit==null) { SetStatus("辺の近くをクリックしてください。");return; }
            var path=ReadCutPath();
            if(path.Length>=256 || path.Any(p=>p.Edge.Equals(hit.Location.Edge))) throw new InvalidOperationException("辺は重複なしで256個まで登録できます。");
            WriteCutPath(path.Concat(new[]{hit.Location}).ToArray());
        });
    }
}



