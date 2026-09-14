using System;
using NyaForge.Authoring;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    /// <summary>Owns Unity event subscriptions and the preview scene lifetime.</summary>
    public sealed partial class AuthoringWorkbench
    {
        void Awake()
        {
            Application.wantsToQuit += WantsToQuit;
            selectionContext.Changed += OnSelectionContextChanged;
        }

        void OnSelectionContextChanged()
        {
            if (projection != null) projection.Select(selection);
        }

        void OnSessionStateChanged()
        {
            RefreshPersistencePanel();
            if (commandBarContext != null) RefreshCommandBar();
        }

        public void Open(VisualElement parent, Action closeAction, Action reopenAction)
        {
            close = closeAction;
            reopen = reopenAction;
            if (root == null)
            {
                BuildUi(parent);
                stage = new GameObject("NyaForge authoring preview");
                AuthoringPreviewLights.Create(stage.transform);
                camera = new GameObject("Authoring camera").AddComponent<Camera>();
                camera.transform.SetParent(stage.transform, false);
                camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = new Color(.055f, .08f, .11f);
                camera.cullingMask = 1 << OwnedMeshProjection.PreviewLayer;
                camera.nearClipPlane = .001f;
                camera.farClipPlane = 30;
                camera.fieldOfView = 35;
                camera.depth = 10;
                projection = new OwnedMeshProjection(stage.transform);
                objectProjection = new MultiObjectProjection(stage.transform);
                avatarSurfaceSelection = new AvatarSurfaceSelectionProjection(stage.transform);
                ReplaceWorkspace(AuthoringWorkspace.CreateEmpty(), null);
            }
            active = true;
            root.style.display = DisplayStyle.Flex;
            stage.SetActive(true);
            UpdateCamera();
        }

        bool WantsToQuit()
        {
            if (allowQuit || !HasUnsaved) return true;
            if (!active) reopen?.Invoke();
            confirmRow.Clear();
            confirmRow.style.display = DisplayStyle.Flex;
            confirmRow.Add(new Label("制作データに未保存の変更があります。終了方法を選んでください。"));
            confirmRow.Add(Button("保存して終了", () => { if (TrySaveForExit()) { allowQuit = true; Application.Quit(); } }, "authoring-save-quit"));
            confirmRow.Add(Button("変更を破棄して終了", () => { allowQuit = true; Application.Quit(); }, "authoring-discard-quit"));
            confirmRow.Add(Button("キャンセル", () => confirmRow.style.display = DisplayStyle.None, "authoring-cancel-quit"));
            controls.schedule.Execute(() => controls.ScrollTo(confirmRow));
            return false;
        }

        void OnDestroy()
        {
            StopMcp();
            if (session != null) session.StateChanged -= OnSessionStateChanged;
            selectionContext.Changed -= OnSelectionContextChanged;
            Application.wantsToQuit -= WantsToQuit;
            surfacePreparationWatch?.Pause();
            surfacePreparation.Dispose();
            CancelSurfaceStroke();
            projection?.Dispose();
            objectProjection?.Dispose();
            avatarSurfaceSelection?.Dispose();
            boundaryHighlight?.Dispose();
            bridgeHighlight?.Dispose();
            faceCreateOutline?.Dispose();
            edgeCutLine?.Dispose();
            cutPathLine?.Dispose();
            cutHoverEdge?.Dispose();
            paintCanvas?.Dispose();
            if (stage != null) Destroy(stage);
            if (previewTexture) { previewTexture.Release(); Destroy(previewTexture); }
        }
    }
}
