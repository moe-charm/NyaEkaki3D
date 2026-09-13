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
            }, 17);
            var bytes = ImportedGlbDiagnosticsCodec.Write(new[] { record }); var reopened = ImportedGlbDiagnosticsCodec.Read(bytes);
            Equal(1, reopened.Count); Equal(graphId, reopened[graphId].GraphId); Equal(3, reopened[graphId].MeshIndex); Equal(2, reopened[graphId].SkinIndex.Value); Equal(17, reopened[graphId].NodeIndex.Value); Equal(2, reopened[graphId].Diagnostics.Count);
            True(bytes.SequenceEqual(ImportedGlbDiagnosticsCodec.Write(reopened.Values)));

            var legacy = JObject.Parse(Encoding.UTF8.GetString(bytes)); legacy["version"] = 1; ((JObject)((JArray)legacy["records"]!)[0]!).Remove("nodeIndex");
            var legacyRecord = ImportedGlbDiagnosticsCodec.Read(Encoding.UTF8.GetBytes(legacy.ToString()))[graphId];
            True(!legacyRecord.NodeIndex.HasValue);
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
            var record = new ImportedGlbDiagnostics(graph.GraphId, new string('c', 64), 4, null, new[] { new GlbImportDiagnostic("ANIMATIONS_NOT_RETAINED", "animations", true, "animation is not retained") }, 6);
            workspace.SetAttachments(new ProjectAttachments(new System.Collections.Generic.Dictionary<string, byte[]> { [ProjectAttachments.ImportDiagnostics] = ImportedGlbDiagnosticsCodec.Write(new[] { record }) }));
            var directory = Dir("import-diagnostics-snapshot"); ProjectStore.Save(directory, workspace, 0); var reopened = ProjectStore.Open(directory);
            var inspected = AuthoringGraphReader.Read(reopened, reopened.InstanceId); Equal(1, inspected["graph"]["importDiagnostics"]["items"].Count()); Equal("ANIMATIONS_NOT_RETAINED", (string)inspected["graph"]["importDiagnostics"]["items"][0]["code"]); Equal(6, (int)inspected["graph"]["importDiagnostics"]["nodeIndex"]); False(reopened.IsDirty);

            var clean = new ImportedGlbDiagnostics(graph.GraphId, new string('f', 64), 7, 3, Array.Empty<GlbImportDiagnostic>(), 8);
            workspace.SetAttachments(new ProjectAttachments(new System.Collections.Generic.Dictionary<string, byte[]> { [ProjectAttachments.ImportDiagnostics] = ImportedGlbDiagnosticsCodec.Write(new[] { clean }) }));
            var cleanDirectory = Dir("clean-import-locator-snapshot"); ProjectStore.Save(cleanDirectory, workspace, 0); var cleanReopened = ProjectStore.Open(cleanDirectory);
            var cleanRecord = ImportedGlbDiagnosticsCodec.Read(cleanReopened.Attachments.Read(ProjectAttachments.ImportDiagnostics))[graph.GraphId];
            Equal(new string('f', 64), cleanRecord.SourceHash); Equal(7, cleanRecord.MeshIndex); Equal(3, cleanRecord.SkinIndex.Value); Equal(8, cleanRecord.NodeIndex.Value); Equal(0, cleanRecord.Diagnostics.Count); False(cleanReopened.IsDirty);
        });
    }
}
