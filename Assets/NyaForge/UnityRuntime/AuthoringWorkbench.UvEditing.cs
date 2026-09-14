using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        FloatField uvMoveU, uvMoveV, uvRotation, uvScale;
        Button uvTransformButton;

        void BuildUvEditing(VisualElement parent)
        {
            uvPreview.BeginIslandDrag=BeginUvIslandDrag;
            uvMoveU = Number(parent, "U移動", 0, "uv-move-u");
            uvMoveV = Number(parent, "V移動", 0, "uv-move-v");
            uvRotation = Number(parent, "回転 (度)", 0, "uv-rotation");
            uvScale = Number(parent, "拡縮", 1, "uv-scale");
            uvTransformButton = Button("選択したUV島を変形", () => Try(() =>
            {
                var settings = new UvTransformSettings(new Vec2(uvMoveU.value, uvMoveV.value), uvRotation.value, uvScale.value);
                Execute(AuthoringOperation.TransformUvIslands(activeEditContext, selectedFaces.OrderBy(id => id).ToArray(), settings));
            }), "transform-uv");
            parent.Add(uvTransformButton);
            uvPreview.FacePicked += (face, add) => Try(() =>
            {
                if (activeEditContext == null) return;
                // Nested UI change events can run after this pointer event and clear the new selection.
                if (!faceMode.value)
                {
                    faceMode.SetValueWithoutNotify(true);
                    selection.Clear(); selectedFaces.Clear(); selectionContext.NotifyChanged();
                }
                if (!add) selectedFaces.Clear();
                if (face.HasValue)
                {
                    var island = UvIslands.Expand(DisplayedGraphValue().Polygon, new[] { face.Value });
                    if (add && island.All(selectedFaces.Contains)) selectedFaces.ExceptWith(island);
                    else selectedFaces.UnionWith(island);
                    selectionContext.NotifyChanged();
                }
                Refresh();
            });
        }
    }
}
