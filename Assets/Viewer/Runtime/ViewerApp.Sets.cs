using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Viewer.Contracts;

namespace Viewer.Runtime
{
    public sealed partial class ViewerApp
    {
        [Serializable] public sealed class ViewerPreferences
        {
            public int schemaVersion = 1;
            public string startupMode = "pack";
            public string startupSetPath = "";
        }
        const string DefaultSet = "パックの初期状態";
        string settingsRoot, setsDirectory, preferencesPath, preferencesHash, previousPath, previousHash;
        ViewerPreferences preferences = new ViewerPreferences();
        DropdownField setPicker, startupModePicker, startupSetPicker;
        TextField setNameField;
        Label currentSetLabel;
        VisualElement setNameRow, discardRow;
        Action pendingSetAction;
        readonly Dictionary<string, string> setPaths = new Dictionary<string, string>();

        void InitializeSets()
        {
            settingsRoot = Path.GetFullPath(Arg("--settings-root") ?? Application.persistentDataPath);
            setsDirectory = Path.Combine(settingsRoot, "Sessions", "Sets");
            preferencesPath = Path.Combine(settingsRoot, "viewer-preferences.json");
            previousPath = Path.Combine(settingsRoot, "Sessions", "previous.viewer.json");
            sessionPath = "";
            try
            {
                if (File.Exists(preferencesPath))
                {
                    var value = JsonFiles.Read<ViewerPreferences>(preferencesPath, out preferencesHash);
                    if (value.schemaVersion != 1 || !new[] { "pack", "set", "previous" }.Contains(value.startupMode))
                        throw new InvalidDataException("起動設定の形式が不正です。パックの初期状態で起動します。");
                    preferences = value;
                }
                if (File.Exists(previousPath)) previousHash = JsonFiles.Sha256(previousPath);
            }
            catch (Exception e) { Debug.LogWarning(e.Message); }
        }
        string StartupSession()
        {
            return preferences.startupMode == "set" ? preferences.startupSetPath : preferences.startupMode == "previous" ? previousPath : "";
        }
        void OpenStartupState()
        {
            string path = StartupSession();
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) { OpenPath(path); return; }
            if (!string.IsNullOrEmpty(path)) Debug.LogWarning("起動セットがありません。パックの初期状態を開きます: " + path);
            if (File.Exists(Path.Combine(LibraryPath, "current.StandaloneWindows64.json"))) CheckUpdate();
        }
        void BuildSetsUi()
        {
            var row = new VisualElement { name = "sets-toolbar" }; row.AddToClassList("file-row"); uiRoot.Add(row);
            setPicker = new DropdownField("確認セット") { name = "set-picker" }; setPicker.style.width = 460; setPicker.style.flexShrink = 0; row.Add(setPicker);
            setPicker.RegisterValueChangedCallback(e => { if (setPaths.TryGetValue(e.newValue, out var path)) ConfirmSetChange(() => OpenSet(path)); });
            currentSetLabel = new Label { name = "current-set-status" }; currentSetLabel.style.flexGrow = 1; row.Add(currentSetLabel);
            row.Add(MakeButton("上書き保存", SaveCurrentSet, "set-save"));
            row.Add(MakeButton("名前を付けて保存", () => { setNameRow.style.display = DisplayStyle.Flex; setNameField.Focus(); }, "set-save-as"));
            row.Add(MakeButton("保存時に戻す", () => ConfirmSetChange(() => OpenSet(sessionPath)), "set-revert"));
            setNameRow = new VisualElement { name = "set-name-row" }; setNameRow.AddToClassList("file-row"); uiRoot.Add(setNameRow);
            setNameField = new TextField("セット名") { name = "set-name" }; setNameField.style.flexGrow = 1; setNameRow.Add(setNameField);
            setNameRow.Add(MakeButton("新規保存", () => SaveNamedSet(setNameField.value), "set-create"));
            setNameRow.Add(MakeButton("キャンセル", () => setNameRow.style.display = DisplayStyle.None, "set-name-cancel"));
            setNameRow.style.display = DisplayStyle.None;
            discardRow = new VisualElement { name = "set-discard-confirm" }; discardRow.AddToClassList("file-row"); uiRoot.Add(discardRow);
            discardRow.Add(new Label("未保存の変更があります。変更を破棄して切り替えますか？"));
            discardRow.Add(MakeButton("破棄して切替", () => { var action = pendingSetAction; pendingSetAction = null; discardRow.style.display = DisplayStyle.None; action?.Invoke(); }, "set-discard"));
            discardRow.Add(MakeButton("キャンセル", () => { pendingSetAction = null; discardRow.style.display = DisplayStyle.None; }, "set-discard-cancel"));
            discardRow.style.display = DisplayStyle.None;
            var startup = new Foldout { text = "起動時の表示", value = false, name = "startup-settings" }; uiRoot.Add(startup);
            var startupRow = new VisualElement(); startupRow.AddToClassList("file-row"); startup.Add(startupRow);
            startupModePicker = new DropdownField("起動時", new List<string> { "pack", "set", "previous" }, 0) { name = "startup-mode" };
            string ModeName(string v) => v == "set" ? "指定した確認セット" : v == "previous" ? "前回終了時の状態" : DefaultSet;
            startupModePicker.formatListItemCallback = ModeName; startupModePicker.formatSelectedValueCallback = ModeName;
            startupModePicker.SetValueWithoutNotify(preferences.startupMode); startupRow.Add(startupModePicker);
            startupSetPicker = new DropdownField("指定セット") { name = "startup-set" }; startupSetPicker.style.flexGrow = 1; startupRow.Add(startupSetPicker);
            startupRow.Add(MakeButton("起動設定を保存", SaveStartupPreferences, "startup-save"));
            startup.Add(new Label("確認セットの保存では起動設定は変わりません。前回の状態は終了時に別ファイルへ保存します。"));
            RefreshSetChoices();
        }
        void RefreshSetChoices()
        {
            if (setPicker == null) return;
            try
            {
                setPaths.Clear(); setPaths.Add(DefaultSet, "");
                void AddChoice(string path)
                {
                    string label = Path.GetFileName(path);
                    if (label.EndsWith(".viewer.json", StringComparison.OrdinalIgnoreCase)) label = label.Substring(0, label.Length - ".viewer.json".Length);
                    if (string.Equals(path, previousPath, StringComparison.OrdinalIgnoreCase)) label = "前回終了時の状態";
                    string unique = label; int suffix = 2;
                    while (setPaths.ContainsKey(unique)) unique = label + " (" + suffix++ + ")";
                    setPaths.Add(unique, path);
                }
                if (Directory.Exists(setsDirectory))
                    foreach (var path in Directory.GetFiles(setsDirectory, "*.viewer.json").OrderBy(x => x))
                        AddChoice(path);
                if (!string.IsNullOrEmpty(sessionPath) && !setPaths.Values.Contains(sessionPath, StringComparer.OrdinalIgnoreCase))
                    AddChoice(sessionPath);
                setPicker.choices = setPaths.Keys.ToList();
                string selected = startupSetPicker.value;
                startupSetPicker.choices = setPaths.Where(x => x.Value != "").Select(x => x.Key).ToList();
                startupSetPicker.SetValueWithoutNotify(startupSetPicker.choices.Contains(selected ?? "") ? selected :
                    setPaths.FirstOrDefault(x => string.Equals(x.Value, preferences.startupSetPath, StringComparison.OrdinalIgnoreCase) && x.Value != "").Key ?? startupSetPicker.choices.FirstOrDefault() ?? "");
            }
            catch (Exception e) { Error(e); }
        }
        void ReflectSetsUi()
        {
            if (setPicker == null) return;
            bool ready = !IsBusy && !reloadLoop && Document != null && Active?.Avatar != null;
            string name = string.IsNullOrEmpty(sessionPath) ? DefaultSet : Path.GetFileName(sessionPath).Replace(".viewer.json", "");
            currentSetLabel.text = Dirty ? "● 未保存" : string.IsNullOrEmpty(sessionPath) ? "初期状態" : "保存済み";
            currentSetLabel.tooltip = name;
            var choice = setPaths.FirstOrDefault(x => string.Equals(x.Value, sessionPath, StringComparison.OrdinalIgnoreCase)).Key;
            if (choice == null) { RefreshSetChoices(); choice = setPaths.FirstOrDefault(x => string.Equals(x.Value, sessionPath, StringComparison.OrdinalIgnoreCase)).Key; }
            setPicker.SetValueWithoutNotify(choice ?? DefaultSet); setPicker.SetEnabled(ready);
            setPicker.tooltip = string.IsNullOrEmpty(sessionPath) ? DefaultSet : sessionPath;
            foreach (string id in new[] { "save", "set-save", "set-save-as", "set-create", "set-revert", "set-discard" }) uiRoot.Q<Button>(id)?.SetEnabled(ready);
            startupSetPicker.SetEnabled(startupModePicker.value == "set");
        }
        void ConfirmSetChange(Action action)
        {
            if (IsBusy || reloadLoop) return;
            Pause();
            if (!Dirty) { action(); return; }
            pendingSetAction = action; discardRow.style.display = DisplayStyle.Flex;
        }
        void OpenSet(string path)
        {
            if (IsBusy || reloadLoop) return;
            if (!string.IsNullOrEmpty(path)) { OpenPath(path); return; }
            if (Active == null) return;
            RequestReload(Active.Verified.Path, PackStore.Defaults(Active.Verified), Active.Verified.Hash, "", null);
        }
        void SaveCurrentSet()
        {
            if (string.IsNullOrEmpty(sessionPath) || string.Equals(sessionPath, previousPath, StringComparison.OrdinalIgnoreCase))
            { setNameRow.style.display = DisplayStyle.Flex; setNameField.Focus(); return; }
            Pause(); Save(sessionPath);
        }
        void SaveNamedSet(string name)
        {
            try
            {
                name = (name ?? "").Trim();
                if (name.Length == 0 || name.Length > 80 || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.EndsWith(".") ||
                    System.Text.RegularExpressions.Regex.IsMatch(name, @"^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])($|\.)", System.Text.RegularExpressions.RegexOptions.IgnoreCase))
                    throw new InvalidDataException("セット名を1～80文字で入力してください。ファイル名に使えない文字・予約名は使えません。");
                var path = Path.Combine(setsDirectory, name + ".viewer.json");
                if (File.Exists(path)) throw new IOException("同名のセットがあります。別の名前にするか、そのセットを開いて上書き保存してください。");
                Pause(); Save(path);
                if (string.Equals(sessionPath, path, StringComparison.OrdinalIgnoreCase) && !Dirty) setNameRow.style.display = DisplayStyle.None;
            }
            catch (Exception e) { Error(e); }
        }
        void SaveStartupPreferences()
        {
            try
            {
                var value = new ViewerPreferences { startupMode = startupModePicker.value };
                if (value.startupMode == "set")
                {
                    if (!setPaths.TryGetValue(startupSetPicker.value ?? "", out value.startupSetPath) || !File.Exists(value.startupSetPath))
                        throw new InvalidDataException("保存済みの確認セットを指定してください。");
                    Validation.Session(JsonFiles.Read<SessionDocument>(value.startupSetPath));
                }
                preferencesHash = JsonFiles.AtomicWrite(preferencesPath, value, preferencesHash);
                preferences = value; SetStatus("起動設定を保存しました");
            }
            catch (Exception e) { Error(e); }
        }
        void SetBodyVisibility(bool bodyOnly)
        {
            Edit(s => s.visibilityOverrides = bodyOnly ? Active.Verified.Manifest.renderers.Select(r => new VisibilityOverride { rendererId = r.rendererId, visible = r.category == "body" }).ToArray() : Array.Empty<VisibilityOverride>());
            if (Active?.Avatar != null && !IsBusy) RebuildControls();
        }
        void SavePreviousState()
        {
            if (Document == null || IsBusy || reloadLoop) return;
            var snapshot = Snapshot();
            snapshot.pack.manifestPath = Path.GetRelativePath(Path.GetDirectoryName(previousPath), Active?.Verified.Path ?? Document.pack.manifestPath).Replace('\\', '/');
            Validation.Session(snapshot);
            previousHash = JsonFiles.AtomicWrite(previousPath, snapshot, previousHash);
        }
        void OnApplicationQuit()
        {
            // Acceptance runners must never alter the user's startup snapshot.
            if (Environment.GetCommandLineArgs().Any(a => a.StartsWith("--") && a.EndsWith("-output"))) return;
            try { SavePreviousState(); } catch (Exception e) { Debug.LogWarning("前回状態を保存できませんでした: " + e.Message); }
        }
    }
}
