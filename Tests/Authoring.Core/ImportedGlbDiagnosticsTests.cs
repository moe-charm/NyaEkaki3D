using System;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Inspection;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunImportedGlbDiagnosticsTests()
    {
        Test("GLB import diagnostics roundtrip with source locator and severity", () =>
        {
            var sourceHash = new string('a', 64); var graphId = GraphId();
            var record = new ImportedGlbDiagnostics(graphId, sourceHash, 3, 2, new[]
            {
                new GlbImportDiagnostic("MATERIALS_NOT_RETAINED", "materials", true, "material is not retained"),
                new GlbImportDiagnostic("EXTENSIONS_PARTIAL", "extensionsUsed", false, "extension is inventory-only")
            });
            var bytes = ImportedGlbDiagnosticsCodec.Write(new[] { record }); var reopened = ImportedGlbDiagnosticsCodec.Read(bytes);
            Equal(1, reopened.Count); Equal(graphId, reopened[graphId].GraphId); Equal(3, reopened[graphId].MeshIndex); Equal(2, reopened[graphId].SkinIndex.Value); Equal(2, reopened[graphId].Diagnostics.Count);
            True(bytes.SequenceEqual(ImportedGlbDiagnosticsCodec.Write(reopened.Values)));
        });
        Test("GLB import diagnostics reject unknown fields, versions and trailing bytes", () =>
        {
            var record = new ImportedGlbDiagnostics(GraphId(), new string('b', 64), 0, null, Array.Empty<GlbImportDiagnostic>());
            var bytes = ImportedGlbDiagnosticsCodec.Write(new[] { record }); var json = JObject.Parse(Encoding.UTF8.GetString(bytes));
            ((JObject)((JArray)json["records"]!)[0]!).Add("extra", true); Expect("INVALID_IMPORT", () => ImportedGlbDiagnosticsCodec.Read(Encoding.UTF8.GetBytes(json.ToString())));
            json = JObject.Parse(Encoding.UTF8.GetString(bytes)); json["version"] = 99; Expect("UNSUPPORTED_FORMAT", () => ImportedGlbDiagnosticsCodec.Read(Encoding.UTF8.GetBytes(json.ToString())));
            Expect("INVALID_IMPORT", () => ImportedGlbDiagnosticsCodec.Read(bytes.Concat(new byte[] { 0x01 }).ToArray()));
        });
        Test("GLB import diagnostics survive project snapshot and graph inspection", () =>
        {
            var workspace = AuthoringWorkspace.CreateEmpty("diagnostic snapshot"); string source, edit; var graph = PlaneGraph(out source, out edit);
            Ok(Execute(workspace, AuthoringOperation.AddGraph(graph)));
            var record = new ImportedGlbDiagnostics(graph.GraphId, new string('c', 64), 4, null, new[] { new GlbImportDiagnostic("ANIMATIONS_NOT_RETAINED", "animations", true, "animation is not retained") });
            workspace.SetAttachments(new ProjectAttachments(new System.Collections.Generic.Dictionary<string, byte[]> { [ProjectAttachments.ImportDiagnostics] = ImportedGlbDiagnosticsCodec.Write(new[] { record }) }));
            var directory = Dir("import-diagnostics-snapshot"); ProjectStore.Save(directory, workspace, 0); var reopened = ProjectStore.Open(directory);
            var inspected = AuthoringGraphReader.Read(reopened, reopened.InstanceId); Equal(1, inspected["graph"]["importDiagnostics"]["items"].Count()); Equal("ANIMATIONS_NOT_RETAINED", (string)inspected["graph"]["importDiagnostics"]["items"][0]["code"]); False(reopened.IsDirty);
        });
    }
}
