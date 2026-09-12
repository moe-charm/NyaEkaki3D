using System;
using System.Collections.Generic;
using NyaForge.UnityRuntime;
using UnityEngine;
using UnityEngine.UIElements;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        AuthoringWorkbench authoringWorkbench;
        readonly List<VisualElement> hiddenViewerPanels = new List<VisualElement>();
        GameObject hiddenAuthoringAvatar;
        bool hiddenAvatarWasActive;

        void OpenAuthoring()
        {
            if (authoringWorkbench?.IsOpen == true) return;
            if (IsBusy || reloadLoop || updateChecking) { SetStatus("読み込み完了後に制作を開けます"); return; }
            Pause();
            if (authoringWorkbench == null) authoringWorkbench = gameObject.AddComponent<AuthoringWorkbench>();
            hiddenViewerPanels.Clear();
            foreach (var child in uiRoot.Children())
                if (child.resolvedStyle.display != DisplayStyle.None)
                {
                    hiddenViewerPanels.Add(child);
                    child.style.display = DisplayStyle.None;
                }
            hiddenAuthoringAvatar = Active?.Avatar?.Root;
            hiddenAvatarWasActive = hiddenAuthoringAvatar != null && hiddenAuthoringAvatar.activeSelf;
            if (hiddenAuthoringAvatar != null) hiddenAuthoringAvatar.SetActive(false);
            previewCamera.enabled = false;
            authoringWorkbench.Open(uiRoot, CloseAuthoring, OpenAuthoring);
            WakeRendering();
        }

        void CloseAuthoring()
        {
            foreach (var panel in hiddenViewerPanels) panel.style.display = DisplayStyle.Flex;
            hiddenViewerPanels.Clear();
            if (hiddenAuthoringAvatar != null) hiddenAuthoringAvatar.SetActive(hiddenAvatarWasActive);
            hiddenAuthoringAvatar = null;
            previewCamera.enabled = true;
            UpdateViewport();
            WakeRendering();
        }
    }
}
