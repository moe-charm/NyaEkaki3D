using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using UnityEngine;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout modelImportDiagnosticsPanel;
        Label modelImportDiagnosticsSummary;
        VisualElement modelImportDiagnosticsItems;

        void BuildModelImportDiagnostics(VisualElement parent)
        {
            modelImportDiagnosticsPanel = new Foldout
            {
                text = "取込診断（保存済み）",
                value = false,
                name = "model-import-diagnostics"
            };
            modelImportDiagnosticsSummary = new Label { name = "model-import-diagnostics-summary" };
            modelImportDiagnosticsSummary.style.whiteSpace = WhiteSpace.Normal;
            modelImportDiagnosticsPanel.Add(modelImportDiagnosticsSummary);
            modelImportDiagnosticsPanel.Add(Button("診断を更新", RefreshImportedGlbDiagnostics, "model-import-diagnostics-refresh"));
            modelImportDiagnosticsItems = new VisualElement { name = "model-import-diagnostics-items" };
            modelImportDiagnosticsItems.style.whiteSpace = WhiteSpace.Normal;
            modelImportDiagnosticsPanel.Add(modelImportDiagnosticsItems);
            parent.Add(modelImportDiagnosticsPanel);
        }

        void RefreshImportedGlbDiagnostics()
        {
            if (modelImportDiagnosticsPanel == null) return;
            modelImportDiagnosticsItems.Clear();
            byte[] bytes = null;
            try { bytes = workspace?.Attachments?.Read(ProjectAttachments.ImportDiagnostics); }
            catch (Exception error)
            {
                modelImportDiagnosticsSummary.text = "保存済み診断を読めません: " + error.Message;
                modelImportDiagnosticsItems.Add(new Label("作品を置き換えず、診断attachmentを確認してください。") { name = "model-import-diagnostics-error" });
                return;
            }
            if (bytes == null)
            {
                modelImportDiagnosticsSummary.text = "この作品には保存済みのGLB取込診断がありません。";
                return;
            }
            IReadOnlyDictionary<string, ImportedGlbDiagnostics> records;
            try { records = ImportedGlbDiagnosticsCodec.Read(bytes); }
            catch (Exception error)
            {
                modelImportDiagnosticsSummary.text = "診断attachmentが不正です。作品はそのまま保持しています。";
                modelImportDiagnosticsItems.Add(new Label(error.Message) { name = "model-import-diagnostics-error" });
                return;
            }
            if (records.Count == 0)
            {
                modelImportDiagnosticsSummary.text = "この作品には保存済みのGLB取込診断がありません。";
                return;
            }
            string activeGraphId = workspace?.Document?.ActiveObject?.Graph?.GraphId;
            var active = activeGraphId != null && records.TryGetValue(activeGraphId, out var activeRecord) ? activeRecord : null;
            int blocking = records.Values.Sum(record => record.Diagnostics.Count(item => item.IsBlocking));
            int partial = records.Values.Sum(record => record.Diagnostics.Count(item => !item.IsBlocking));
            modelImportDiagnosticsSummary.text = "保存済み " + records.Count + " object · 保持不可 " + blocking + " · 一部保持 " + partial +
                (active == null ? " · active objectの診断なし" : " · active objectを先頭表示");

            foreach (var record in records.Values.OrderBy(item => item.GraphId, StringComparer.Ordinal))
            {
                bool isActive = record.GraphId == activeGraphId;
                var header = new Label((isActive ? "● active  " : "") + "graph " + record.GraphId);
                header.name = "model-import-diagnostics-graph-" + record.GraphId;
                header.style.unityFontStyleAndWeight = FontStyle.Bold;
                header.style.whiteSpace = WhiteSpace.Normal;
                modelImportDiagnosticsItems.Add(header);
                string locator = "source " + record.SourceHash + " · mesh " + record.MeshIndex +
                    (record.SkinIndex.HasValue ? " · skin " + record.SkinIndex.Value : " · skinなし");
                var locatorLabel = new Label(locator) { name = "model-import-diagnostics-locator" };
                locatorLabel.style.whiteSpace = WhiteSpace.Normal;
                modelImportDiagnosticsItems.Add(locatorLabel);
                if (record.Diagnostics.Count == 0)
                {
                    modelImportDiagnosticsItems.Add(new Label("診断項目なし") { name = "model-import-diagnostics-empty" });
                    continue;
                }
                foreach (var diagnostic in record.Diagnostics)
                {
                    string severity = diagnostic.IsBlocking ? "保持不可" : "一部保持";
                    var detail = new Label(severity + " · " + diagnostic.Code + " · " + diagnostic.Path + "\n" + diagnostic.Message)
                    {
                        name = "model-import-diagnostic-" + diagnostic.Code
                    };
                    detail.style.whiteSpace = WhiteSpace.Normal;
                    detail.style.marginLeft = 10;
                    detail.style.marginBottom = 4;
                    modelImportDiagnosticsItems.Add(detail);
                }
            }
        }
    }
}
