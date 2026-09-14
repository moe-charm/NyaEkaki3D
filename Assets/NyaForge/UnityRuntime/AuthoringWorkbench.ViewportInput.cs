using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using UnityEngine;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>Converts the shared vertex selection into a graph or static edit command.</summary>
        void MoveSelection()
        {
            var ids = selection.OrderBy(i => i).ToArray();
            var delta = new Vec3(moveX.value / 1000, moveY.value / 1000, moveZ.value / 1000);
            if (IsGraph)
            {
                if (activeEditContext == null) { SetStatus("編集可能なEditMeshを選択してください。"); return; }
                var value = DisplayedGraphValue();
                if (value?.Polygon != null)
                {
                    var stableIds = SelectedPolygonVertices();
                    Execute(AuthoringOperation.TranslatePolygonVertices(activeEditContext, stableIds, delta));
                }
                else Execute(AuthoringOperation.TranslateGraphVertices(activeEditContext, ids, delta));
            }
            else Execute(AuthoringOperation.TranslateVertices(ids, delta));
        }

        void Select(IEnumerable<int> indices)
        {
            selection.Clear(); foreach (int i in indices) selection.Add(i);
            selectionContext.NotifyChanged();
            projection.Select(selection); RefreshSelectionPresentation();
        }

        /// <summary>Resolves a click against world-space projection points after attachment transforms.</summary>
        void PickVertex(Vector2 panelPosition, bool add)
        {
            if (faceMode.value && faceMode.enabledSelf) { PickFace(panelPosition, add); return; }
            int closest = -1; float best = 18;
            var points = projection.WorldPoints;
            for (int i = 0; i < points.Length; i++)
            {
                var projected = camera.WorldToViewportPoint(points[i]);
                var d = Vector2.Distance(panelPosition, VertexPanelPoint(points[i]));
                if (projected.z > 0 && d < best) { closest = i; best = d; }
            }
            if (closest < 0) return;
            if (!add) selection.Clear();
            if (add && selection.Contains(closest)) selection.Remove(closest); else selection.Add(closest);
            selectionContext.NotifyChanged();
            projection.Select(selection); RefreshSelectionPresentation();
        }

        Vector2 VertexPanelPoint(Vector3 point)
        {
            var projected = camera.WorldToViewportPoint(point);
            var rect = view.worldBound;
            return new Vector2(rect.x + projected.x * rect.width, rect.y + (1 - projected.y) * rect.height);
        }
    }
}
