using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>One explicitly reported piece of GLB data that the selected importer does not retain.</summary>
    public sealed class GlbImportDiagnostic
    {
        public string Code { get; }
        public string Path { get; }
        public bool IsBlocking { get; }
        public string Message { get; }

        internal GlbImportDiagnostic(string code, string path, bool isBlocking, string message)
        {
            Checks.Name(code); Checks.Require(path != null && message != null, "INVALID_IMPORT", "GLB diagnostic fields are required.");
            Code = code; Path = path; IsBlocking = isBlocking; Message = message;
        }

        public override string ToString() => Code + " (" + Path + "): " + Message;
    }

    /// <summary>Reports lossy GLB features before the mesh is published to a graph object.</summary>
    internal static class GlbImportDiagnostics
    {
        public static IReadOnlyList<GlbImportDiagnostic> ForMesh(JObject root, JObject mesh)
        {
            Checks.Require(root != null && mesh != null, "INVALID_IMPORT", "GLB diagnostic source is required.");
            var result = new List<GlbImportDiagnostic>();
            var primitives = mesh["primitives"] as JArray;
            // Resources can be shared by unrelated meshes.  Only report a material loss
            // when a primitive of the selected mesh actually points at a material slot.
            bool materialReference = primitives != null && primitives.OfType<JObject>().Any(p => p["material"] != null);
            if (materialReference)
                result.Add(new GlbImportDiagnostic("MATERIALS_NOT_RETAINED", "materials", true, "Texture/image resources and unsupported material features are not retained; basic PBR factors are routed into native material nodes."));
            if (root["animations"] is JArray animations && animations.Count > 0)
                result.Add(new GlbImportDiagnostic("ANIMATIONS_NOT_RETAINED", "animations", true, "glTF animations are not imported and are not substituted with the current pose."));
            var required = Names(root["extensionsRequired"]);
            if (required.Count > 0)
                result.Add(new GlbImportDiagnostic("REQUIRED_EXTENSIONS_NOT_RETAINED", "extensionsRequired", true, "Required glTF extensions are present but no complete adapter is registered: " + string.Join(", ", required) + "."));
            var used = Names(root["extensionsUsed"]);
            if (used.Count > 0)
                result.Add(new GlbImportDiagnostic("EXTENSIONS_PARTIAL", "extensionsUsed", false, "glTF extensions are inventory-only at this boundary: " + string.Join(", ", used) + "."));
            return new ReadOnlyCollection<GlbImportDiagnostic>(result);
        }

        public static IReadOnlyList<GlbImportDiagnostic> Empty { get; } = Array.AsReadOnly(Array.Empty<GlbImportDiagnostic>());

        public static IEnumerable<string> WarningText(IEnumerable<GlbImportDiagnostic> diagnostics)
            => (diagnostics ?? Empty).Select(item => item.Code + ": " + item.Message);

        static IReadOnlyList<string> Names(JToken token)
        {
            var array = token as JArray;
            return array == null ? Array.Empty<string>() : array.Where(item => item.Type == JTokenType.String).Select(item => (string)item).Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.Ordinal).ToArray();
        }
    }
}
