using System.Linq;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        /// <summary>
        /// Applies the live workspace to every visible projection and panel.
        /// Selection-only changes use SelectionRefresh instead; command
        /// commits and document replacement come through this full boundary.
        /// </summary>
        void Refresh()
        {
            if (workspace == null) return;
            if (springPlayback != null && (workspace != springWorkspace || workspace.Document.StateHash != springDocumentHash || workspace.Attachments.ContentHash != springMetadataHash)) ClearSpringPlayback(true);
            RefreshGraphEditing();
            RefreshObjectSelection();
            objectProjection?.Refresh(workspace.Document, ResolveAttachmentPoseForObject);
            RefreshAvatarSurfaceSelection();
            RefreshAttachmentControls();
            RefreshSourceSkinDisplayProjection();
            RefreshAttachmentProjection();
            graphCanvas.Bind(workspace, operations => Execute(operations), node => Try(() => SelectEditStage(editStageIds.IndexOf(node))));
            var displayed = DisplayedGraphValue();
            var data = displayed?.Mesh;
            bool staticProfile = !workspace.Document.IsEmpty && workspace.Document.ActiveObject.IsStaticProfile;
            RefreshContextVisibility(!workspace.Document.IsEmpty, !workspace.Document.IsEmpty && !staticProfile);
            emptyHint.style.display = data == null ? DisplayStyle.Flex : DisplayStyle.None;
            emptyHint.text = displayed?.Polygon?.Faces.Count == 0 ? "点を置いて、最初の面を作れます" : workspace.Document.IsEmpty ? "空の制作プロジェクト\n上部の「モデルを追加」または「基本形状を追加」から始めます" : "グラフの評価が未完了です\n接続とノードを確認してください";
            metrics.text = displayed?.Polygon?.Faces.Count == 0 ? displayed.Polygon.Vertices.Count + " 編集点 / 面なし" : data == null ? (workspace.Document.IsEmpty ? "空のプロジェクト — 形を追加して始める" : "評価未完了 — 表示できる結果がありません") : data.VertexCount + " 頂点 / " + data.TriangleCount + " △\n単位: m  ·  元scale: " + displayed.Transform.Scale;
            if (!workspace.Preview.IsComplete) metrics.text += "\n最終評価未完了 / 文書 rev " + workspace.Document.DocumentRevision + (projection.PreviewNodeId == "" && workspace.Preview.IsStale ? " / 表示 rev " + workspace.Preview.OutputRevision : "");
            root.Q<Button>("fixture-1").SetEnabled(workspace.Document.IsEmpty);
            root.Q<Button>("fixture-100").SetEnabled(workspace.Document.IsEmpty);
            layer.SetEnabled(staticProfile);
            selectionLabel.text = selection.Count == 0 ? "点をクリックして選んでください" : "選択: " + selection.Count + " 頂点  [" + string.Join(", ", selection.OrderBy(i => i).Take(12)) + "]";
            moveButton.SetEnabled((staticProfile || activeEditContext != null) && selection.Count > 0);
            undoButton.SetEnabled(workspace.CanUndo);
            redoButton.SetEnabled(workspace.CanRedo);
            layer.SetValueWithoutNotify(staticProfile && workspace.Document.LayerEnabled);
            RefreshPersistencePanel();
            RefreshEditingPanels();
            RefreshSimulationAndOutputPanels();
        }
    }
}
