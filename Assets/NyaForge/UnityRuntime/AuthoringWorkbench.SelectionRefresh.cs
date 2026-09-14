using System.Linq;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Refreshes only controls whose state changes when the vertex
        /// selection changes. Geometry/material/export panels are left alone;
        /// command commits still use the complete Refresh path.
        /// </summary>
        void RefreshSelectionPresentation()
        {
            if (workspace == null) return;
            bool faceSelectionActive = faceMode != null && faceMode.value && faceMode.enabledSelf;
            // Vertex selection drives the face editing controls too (split,
            // edge insertion and weld), so refresh that dependent group before
            // updating the smaller selection-only controls.
            RefreshFaceEditing();
            if (faceSelectionActive) return;

            if (selectionLabel != null)
                selectionLabel.text = selection.Count == 0
                    ? "点をクリックして選んでください"
                    : "選択: " + selection.Count + " 頂点  [" + string.Join(", ", selection.OrderBy(i => i).Take(12)) + "]";

            bool staticProfile = !workspace.Document.IsEmpty && workspace.Document.ActiveObject.IsStaticProfile;
            moveButton?.SetEnabled((staticProfile || activeEditContext != null) && selection.Count > 0);
            if (rigPanel != null) RefreshRig();

            if (accessoryUseSelectedVertices != null)
            {
                var editNode = IsGraph
                    ? workspace.Document.ActiveObject.Graph.Nodes.Values.FirstOrDefault(item => item.TypeId == NyaForge.Authoring.Graph.BuiltinNodes.EditMesh)
                    : null;
                accessoryUseSelectedVertices.SetEnabled(editNode != null && workspace.Preview.IsComplete && selection.Count > 0);
            }
            RefreshViewportHint();
        }
    }
}
