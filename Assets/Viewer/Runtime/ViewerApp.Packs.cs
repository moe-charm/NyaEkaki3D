using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine.UIElements;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        [Serializable]
        sealed class RecentPackPreferences
        {
            public int schemaVersion = 1;
            public string[] paths = Array.Empty<string>();
        }

        DropdownField packPicker;
        readonly Dictionary<string, string> packChoices = new Dictionary<string, string>(StringComparer.Ordinal);
        readonly List<string> recentPackPaths = new List<string>();
        string recentPacksPath;

        void InitializePackHistory()
        {
            recentPacksPath = Path.Combine(settingsRoot, "recent-packs.json");
            try
            {
                if (!File.Exists(recentPacksPath)) return;
                var value = JsonFiles.Read<RecentPackPreferences>(recentPacksPath);
                if (value.schemaVersion != 1 || value.paths == null) throw new InvalidDataException("recent-packs schema");
                foreach (var path in value.paths.Where(p => !string.IsNullOrWhiteSpace(p)).Select(Path.GetFullPath).Distinct(StringComparer.OrdinalIgnoreCase).Take(8))
                    recentPackPaths.Add(path);
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("最近のパック履歴を読み込めません: " + e.Message); }
        }

        void BuildPackPicker(VisualElement toolbar)
        {
            packPicker = new DropdownField("最近のパック") { name = "recent-pack-picker" };
            packPicker.style.width = 360; packPicker.style.flexShrink = 1;
            toolbar.Add(packPicker);
            toolbar.Add(MakeButton("開く", OpenSelectedPack, "open-recent-pack"));
            RefreshPackPicker();
        }

        void RefreshPackPicker()
        {
            if (packPicker == null) return;
            packChoices.Clear();
            var labels = new List<string>();
            foreach (var path in recentPackPaths)
            {
                var label = PackLabel(path);
                if (packChoices.ContainsKey(label)) label += "  (" + labels.Count + ")";
                packChoices[label] = path; labels.Add(label);
            }
            if (labels.Count == 0) labels.Add("履歴なし — パックを開くから選択");
            packPicker.choices = labels;
            packPicker.SetValueWithoutNotify(labels[0]);
            packPicker.tooltip = packChoices.TryGetValue(labels[0], out var selected) ? selected : "";
        }

        static string PackLabel(string path)
        {
            var cursor = new DirectoryInfo(Path.GetDirectoryName(path) ?? path);
            for (int i = 0; cursor != null && i < 6; i++, cursor = cursor.Parent)
            {
                if (string.Equals(cursor.Name, "avatar-raddollv3-local", StringComparison.OrdinalIgnoreCase)) return "RadDollV3（最近）";
                if (string.Equals(cursor.Name, "NyaForgeFixture", StringComparison.OrdinalIgnoreCase)) return "NyaForge試作（最近）";
            }
            var name = Path.GetFileName(Path.GetDirectoryName(path) ?? path);
            return name + "（最近）";
        }

        void OpenSelectedPack()
        {
            if (IsBusy || reloadLoop || updateChecking) { SetStatus("読み込み完了後にパックを開けます"); return; }
            if (packPicker == null || !packChoices.TryGetValue(packPicker.value, out var path))
            {
                BrowsePack();
                return;
            }
            AcceptPickedPath(path);
        }

        void RememberPackPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return;
            try { path = Path.GetFullPath(path); } catch { return; }
            recentPackPaths.RemoveAll(p => string.Equals(p, path, StringComparison.OrdinalIgnoreCase));
            recentPackPaths.Insert(0, path);
            if (recentPackPaths.Count > 8) recentPackPaths.RemoveRange(8, recentPackPaths.Count - 8);
            try
            {
                JsonFiles.AtomicWrite(recentPacksPath, new RecentPackPreferences { paths = recentPackPaths.ToArray() }, allowReplace: true);
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("最近のパック履歴を保存できません: " + e.Message); }
            RefreshPackPicker();
        }
    }
}
