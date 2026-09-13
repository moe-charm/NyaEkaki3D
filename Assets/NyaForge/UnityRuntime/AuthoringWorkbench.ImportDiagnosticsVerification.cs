using System;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void VerifyImportDiagnosticsPanel(List<string> checks)
        {
            var original = workspace.Attachments;
            try
            {
                string graphId = workspace.Document.ActiveObject.Graph.GraphId;
                var record = new ImportedGlbDiagnostics(graphId, new string('d', 64), 0, null, new[]
                {
                    new GlbImportDiagnostic("MATERIALS_NOT_RETAINED", "materials", true, "material is inventory-only")
                });
                workspace.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]>
                {
                    [ProjectAttachments.ImportDiagnostics] = ImportedGlbDiagnosticsCodec.Write(new[] { record })
                }));
                RefreshImportedGlbDiagnostics();
                Check(modelImportDiagnosticsPanel != null && modelImportDiagnosticsSummary != null && modelImportDiagnosticsItems != null, "GLB diagnostics panel was not built");
                Check(modelImportDiagnosticsSummary.text.Contains("保存済み 1 object") && modelImportDiagnosticsSummary.text.Contains("保持不可 1"), "GLB diagnostics summary is incomplete");
                var detail = modelImportDiagnosticsItems.Q<Label>("model-import-diagnostic-MATERIALS_NOT_RETAINED");
                Check(detail != null && detail.text.Contains("materials") && detail.text.Contains("material is inventory-only"), "GLB diagnostics detail is not visible");
                checks.Add("GLB import diagnostics GUI: saved attachment summary, active graph locator, severity/code/path/message detail");

                // Clean imports still carry the source locator. The diagnostics
                // list is empty, but the serialized record must still expose
                // enough identity for a shared mesh to be traced back.
                var clean = new ImportedGlbDiagnostics(graphId, new string('e', 64), 7, 3, Array.Empty<GlbImportDiagnostic>());
                workspace.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]>
                {
                    [ProjectAttachments.ImportDiagnostics] = ImportedGlbDiagnosticsCodec.Write(new[] { clean })
                }));
                RefreshImportedGlbDiagnostics();
                Check(modelImportDiagnosticsSummary.text.Contains("保存済み 1 object") && modelImportDiagnosticsSummary.text.Contains("保持不可 0") && modelImportDiagnosticsSummary.text.Contains("一部保持 0"), "Clean GLB locator summary is incomplete");
                var locator = modelImportDiagnosticsItems.Q<Label>("model-import-diagnostics-locator");
                Check(locator != null && locator.text.Contains(new string('e', 64)) && locator.text.Contains("mesh 7") && locator.text.Contains("skin 3"), "Clean GLB source locator is not visible");
                Check(modelImportDiagnosticsItems.Q<Label>("model-import-diagnostics-empty") != null, "Clean GLB diagnostics should show an empty diagnostic state");
                checks.Add("clean GLB import locator: source hash, mesh/skin selection and empty diagnostics survive attachment refresh");
            }
            finally
            {
                workspace.SetAttachments(original);
                RefreshImportedGlbDiagnostics();
            }
        }
    }
}
