using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Loss report for one imported graph object, persisted without source model bytes.</summary>
    public sealed class ImportedGlbDiagnostics
    {
        public string GraphId { get; }
        public string SourceHash { get; }
        public int MeshIndex { get; }
        public int? SkinIndex { get; }
        public int? NodeIndex { get; }
        public IReadOnlyList<GlbImportDiagnostic> Diagnostics { get; }

        public ImportedGlbDiagnostics(string graphId, string sourceHash, int meshIndex, int? skinIndex, IEnumerable<GlbImportDiagnostic> diagnostics, int? nodeIndex = null)
        {
            Checks.Id(graphId); Checks.HashText(sourceHash); Checks.Require(meshIndex >= 0 && (skinIndex == null || skinIndex.Value >= 0) && (nodeIndex == null || nodeIndex.Value >= 0), "INVALID_IMPORT", "GLB diagnostic locator is invalid.");
            var values = (diagnostics ?? Array.Empty<GlbImportDiagnostic>()).ToArray(); Checks.Require(values.Length <= 64 && values.All(item => item != null), "INVALID_IMPORT", "GLB diagnostics exceed capacity.");
            GraphId = graphId; SourceHash = sourceHash; MeshIndex = meshIndex; SkinIndex = skinIndex; NodeIndex = nodeIndex; Diagnostics = Array.AsReadOnly(values);
        }
    }

    /// <summary>Strict bounded JSON codec for persisted GLB import loss reports.</summary>
    public static class ImportedGlbDiagnosticsCodec
    {
        const int Version = 2;

        public static byte[] Write(IEnumerable<ImportedGlbDiagnostics> records)
        {
            var values = (records ?? Array.Empty<ImportedGlbDiagnostics>()).ToArray(); Checks.Require(values.Length <= 64 && values.All(item => item != null), "INVALID_IMPORT", "GLB diagnostic records exceed capacity.");
            var root = new JObject { ["version"] = Version, ["records"] = new JArray(values.OrderBy(item => item.GraphId, StringComparer.Ordinal).Select(item => new JObject
            {
                ["graphId"] = item.GraphId, ["sourceHash"] = item.SourceHash, ["meshIndex"] = item.MeshIndex,
                ["skinIndex"] = item.SkinIndex.HasValue ? (JToken)new JValue(item.SkinIndex.Value) : JValue.CreateNull(),
                ["nodeIndex"] = item.NodeIndex.HasValue ? (JToken)new JValue(item.NodeIndex.Value) : JValue.CreateNull(),
                ["diagnostics"] = new JArray(item.Diagnostics.Select(d => new JObject { ["code"] = d.Code, ["path"] = d.Path, ["isBlocking"] = d.IsBlocking, ["message"] = d.Message }))
            })) };
            var bytes = new UTF8Encoding(false).GetBytes(root.ToString(Formatting.Indented) + "\n"); Checks.Require(bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "GLB diagnostics exceed capacity."); return bytes;
        }

        public static IReadOnlyDictionary<string, ImportedGlbDiagnostics> Read(byte[] bytes)
        {
            Checks.Require(bytes != null && bytes.Length > 0 && bytes.Length <= AuthoringLimits.MaxBlobBytes, "BUDGET_EXCEEDED", "GLB diagnostics exceed capacity.");
            try
            {
                JObject root;
                using (var text = new StringReader(new UTF8Encoding(false, true).GetString(bytes))) using (var reader = new JsonTextReader(text) { MaxDepth = 12, DateParseHandling = DateParseHandling.None })
                {
                    root = JObject.Load(reader, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error }); Checks.Require(!reader.Read(), "INVALID_IMPORT", "Trailing GLB diagnostics data is not allowed.");
                }
                RequireFields(root, "version", "records"); int version = Int(root, "version"); Checks.Require(version == 1 || version == Version, "UNSUPPORTED_FORMAT", "GLB diagnostics version is unsupported.");
                var array = root["records"] as JArray; Checks.Require(array != null && array.Count <= 64, "INVALID_IMPORT", "GLB diagnostics records are invalid."); var result = new Dictionary<string, ImportedGlbDiagnostics>(StringComparer.Ordinal);
                foreach (var token in array)
                {
                    var item = token as JObject; Checks.Require(item != null, "INVALID_IMPORT", "GLB diagnostic record is invalid.");
                    if (version == 1) RequireFields(item, "graphId", "sourceHash", "meshIndex", "skinIndex", "diagnostics");
                    else RequireFields(item, "graphId", "sourceHash", "meshIndex", "skinIndex", "nodeIndex", "diagnostics");
                    string graphId = String(item, "graphId", 36); string sourceHash = String(item, "sourceHash", 64); int meshIndex = Int(item, "meshIndex"); int? skinIndex = item["skinIndex"]?.Type == JTokenType.Null ? (int?)null : Int(item, "skinIndex"); int? nodeIndex = version == 1 || item["nodeIndex"]?.Type == JTokenType.Null ? (int?)null : Int(item, "nodeIndex");
                    var diagnostics = item["diagnostics"] as JArray; Checks.Require(diagnostics != null && diagnostics.Count <= 64, "INVALID_IMPORT", "GLB diagnostic list is invalid."); var values = new List<GlbImportDiagnostic>();
                    foreach (var diagnosticToken in diagnostics)
                    {
                        var diagnostic = diagnosticToken as JObject; Checks.Require(diagnostic != null, "INVALID_IMPORT", "GLB diagnostic entry is invalid."); RequireFields(diagnostic, "code", "path", "isBlocking", "message");
                        values.Add(new GlbImportDiagnostic(String(diagnostic, "code", 128), String(diagnostic, "path", 256), Bool(diagnostic, "isBlocking"), String(diagnostic, "message", 2048, true)));
                    }
                    var record = new ImportedGlbDiagnostics(graphId, sourceHash, meshIndex, skinIndex, values, nodeIndex); Checks.Require(result.TryAdd(graphId, record), "INVALID_IMPORT", "GLB diagnostics graph identity repeats.");
                }
                return new ReadOnlyDictionary<string, ImportedGlbDiagnostics>(result);
            }
            catch (JsonException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
            catch (DecoderFallbackException error) { throw new AuthoringException("INVALID_IMPORT", error.Message); }
        }

        static void RequireFields(JObject owner, params string[] names)
        {
            var expected = new HashSet<string>(names, StringComparer.Ordinal); foreach (var name in names) Checks.Require(owner[name] != null, "INVALID_IMPORT", "GLB diagnostics field is missing: " + name); foreach (var property in owner.Properties()) Checks.Require(expected.Contains(property.Name), "INVALID_IMPORT", "GLB diagnostics field is unknown: " + property.Name);
        }
        static int Int(JObject owner, string name) { var token = owner[name]; Checks.Require(token != null && token.Type == JTokenType.Integer, "INVALID_IMPORT", "GLB diagnostics integer is invalid: " + name); int value = (int)token; Checks.Require(value >= 0, "INVALID_IMPORT", "GLB diagnostics integer is negative: " + name); return value; }
        static bool Bool(JObject owner, string name) { var token = owner[name]; Checks.Require(token != null && token.Type == JTokenType.Boolean, "INVALID_IMPORT", "GLB diagnostics boolean is invalid: " + name); return (bool)token; }
        static string String(JObject owner, string name, int maximum, bool optional = false) { var token = owner[name]; Checks.Require(token != null && token.Type == JTokenType.String && ((string)token).Length <= maximum && (optional || !string.IsNullOrWhiteSpace((string)token)), "INVALID_IMPORT", "GLB diagnostics text is invalid: " + name); return (string)token; }
    }
}
