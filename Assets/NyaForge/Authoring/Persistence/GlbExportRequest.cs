using System;
using System.Linq;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    /// <summary>Revision-pinned request for the MCP standard GLB export.</summary>
    public sealed class GlbExportRequest
    {
        public string DocumentId { get; private set; }
        public long ExpectedRevision { get; private set; }
        public string Directory { get; private set; }
        public string ExportId { get; private set; }
        public GlbExportProfile Profile { get; private set; }

        public static GlbExportRequest Read(JObject value)
        {
            Checks.Require(value != null && value.Properties().Select(p => p.Name).OrderBy(n => n, StringComparer.Ordinal)
                .SequenceEqual(new[] { "directory", "documentId", "expectedRevision", "exportId", "profile" }),
                "INVALID_GLB_EXPORT_REQUEST", "Unexpected GLB export fields.");
            foreach (var field in new[] { "directory", "documentId", "exportId", "profile" })
                Checks.Require(value[field].Type == JTokenType.String, "INVALID_GLB_EXPORT_REQUEST", "Expected GLB export string: " + field);
            long revision = 0;
            Checks.Require(value["expectedRevision"].Type == JTokenType.Integer && long.TryParse(value["expectedRevision"].ToString(), out revision) && revision >= 0,
                "INVALID_GLB_EXPORT_REQUEST", "Expected nonnegative GLB export revision.");
            Checks.Id((string)value["documentId"]); Checks.Id((string)value["exportId"]);
            Checks.Require(Guid.TryParseExact((string)value["exportId"], "D", out _), "INVALID_GLB_EXPORT_REQUEST", "Export ID must be a canonical GUID.");
            GlbExportProfile profile;
            switch ((string)value["profile"])
            {
                case "static": profile = GlbExportProfile.StaticGeometry; break;
                case "skinned": profile = GlbExportProfile.SkinnedGeometry; break;
                case "skinned_extended": profile = GlbExportProfile.SkinnedGeometryExtended; break;
                default: throw new AuthoringException("INVALID_GLB_EXPORT_REQUEST", "Unknown GLB export profile.");
            }
            return new GlbExportRequest { DocumentId = (string)value["documentId"], ExpectedRevision = revision, Directory = (string)value["directory"], ExportId = (string)value["exportId"], Profile = profile };
        }
    }
}
