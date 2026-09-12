using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void RefreshViewportHint()
        {
            var hint=root?.Q<Label>("selection-hint");if(hint==null) return;
            if(surfacePaintMode!=null && surfacePaintMode.value)
                hint.text="3D描画 · 左ドラッグで塗る · Alt+左で回転 · 右で移動 · Escapeで取消";
            else if(CutPathPicking)
                hint.text="辺をクリック: 切断位置を追加 · ドラッグ: 回転 · 右ドラッグ: 移動 · Escape: 指定終了";
            else
                hint.text=(faceMode!=null && faceMode.value && faceMode.enabledSelf ? "面" : "点")+"をクリック: 選択  ·  Shift: 追加／解除  ·  ドラッグ: 回転  ·  右ドラッグ: 移動  ·  ホイール: 拡大";
        }
    }
}
