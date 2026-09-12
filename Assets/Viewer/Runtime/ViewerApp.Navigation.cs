using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        VisualElement recentPanel, setsPanel, settingsPanel;
        Label activePackLabel;
        bool filePickerOpen;

        VisualElement MakeNavigationPanel(string name)
        {
            var panel = new VisualElement { name = name };
            panel.AddToClassList("navigation-panel"); panel.style.display = DisplayStyle.None;
            uiRoot.Add(panel); return panel;
        }

        void ToggleNavigationPanel(VisualElement target)
        {
            bool show = target.resolvedStyle.display == DisplayStyle.None;
            foreach (var panel in new[] { recentPanel, setsPanel, settingsPanel })
                panel.style.display = panel == target && show ? DisplayStyle.Flex : DisplayStyle.None;
            WakeRendering();
        }

        void CloseNavigationPanels()
        {
            foreach (var panel in new[] { recentPanel, setsPanel, settingsPanel })
                if (panel != null) panel.style.display = DisplayStyle.None;
        }

        void BrowsePack()
        {
            if (filePickerOpen || IsBusy || reloadLoop || updateChecking) return;
            StartCoroutine(BrowsePackRoutine());
        }

        IEnumerator BrowsePackRoutine()
        {
            if (Application.platform != RuntimePlatform.WindowsPlayer && Application.platform != RuntimePlatform.WindowsEditor)
            { SetStatus("ファイル選択はWindows版で利用できます。設定からパスを指定してください。"); yield break; }
            var selectedPath = recentPackPaths.Count > 0 ? recentPackPaths[0] : LibraryPath;
            string directory = Directory.Exists(selectedPath) ? selectedPath : Path.GetDirectoryName(selectedPath);
            var picker = WindowsFilePicker.Open(WindowsFilePicker.GetActiveWindow(), directory);
            filePickerOpen = true;
            uiRoot.SetEnabled(false);
            while (!picker.IsCompleted) yield return null;
            filePickerOpen = false;
            uiRoot.SetEnabled(true);
            if (picker.IsFaulted) { Error(picker.Exception); yield break; }
            AcceptPickedPath(picker.Result);
        }

        void AcceptPickedPath(string path)
        {
            // Cancel is a no-op: keep the current mesh, set identity, and recent files.
            if (string.IsNullOrWhiteSpace(path)) return;
            ConfirmSetChange(() => { CloseNavigationPanels(); OpenPath(path, true); });
        }

        void ReflectNavigation()
        {
            bool idle = !filePickerOpen && !IsBusy && !reloadLoop && !updateChecking;
            uiRoot?.Q<Button>("browse-pack")?.SetEnabled(idle);
            uiRoot?.Q<Button>("open-recent-pack")?.SetEnabled(idle && packChoices.Count > 0);
            uiRoot?.Q<Button>("open-authoring")?.SetEnabled(idle);
            if (activePackLabel != null)
            {
                activePackLabel.text = Active?.Verified.Manifest.displayName ?? "パックを開いて始める";
                activePackLabel.tooltip = Active?.Verified.Path ?? "パックを開くからJSONファイルを選んでください";
            }
        }
    }
}
