using System.Linq;
using NyaForge.Authoring;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Button mergeFacesButton;
        void BuildFaceMerge(VisualElement parent)
        {
            mergeFacesButton=Button("選択した面を1面にまとめる",()=>Try(()=>
            {
                Execute(AuthoringOperation.DissolvePolygonFaces(activeEditContext,selectedFaces.OrderBy(id=>id).ToArray()));
            }),"merge-faces");
            mergeFacesButton.tooltip="同一平面・同材質で接続した2面以上をまとめます。内部のUV・法線の継ぎ目や穴がある場合は拒否します。内部の不要点も除去。Undoで戻せます。";
            parent.Add(mergeFacesButton);
        }
    }
}

