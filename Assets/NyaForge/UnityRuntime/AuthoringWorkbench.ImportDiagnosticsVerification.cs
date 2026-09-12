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
            }
            finally
            {
                workspace.SetAttachments(original);
                RefreshImportedGlbDiagnostics();
            }
        }
    }
}
