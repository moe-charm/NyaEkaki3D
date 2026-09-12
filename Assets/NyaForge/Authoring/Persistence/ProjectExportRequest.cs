using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring
{
    public sealed class ProjectExportRequest
    {
        public string DocumentId { get; private set; }
        public long ExpectedRevision { get; private set; }
        public string Directory { get; private set; }
        public string ExportId { get; private set; }
        public static ProjectExportRequest Read(JObject o)
        {
            Checks.Require(o!=null && o.Properties().Select(p=>p.Name).OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(new[]{"directory","documentId","expectedRevision","exportId"}),"INVALID_EXPORT_REQUEST","Unexpected export fields.");
            foreach(var field in new[]{"directory","documentId","exportId"}) Checks.Require(o[field].Type==JTokenType.String,"INVALID_EXPORT_REQUEST","Expected export string.");
            Checks.Require(o["expectedRevision"].Type==JTokenType.Integer && long.TryParse(o["expectedRevision"].ToString(),out var v) && v>=0,"INVALID_EXPORT_REQUEST","Expected export revision.");
            string id=(string)o["exportId"];Checks.Id((string)o["documentId"]);Checks.Id(id);
            Checks.Require(Guid.TryParseExact(id,"D",out _),"INVALID_EXPORT_REQUEST","Export ID must be a canonical GUID.");
            return new ProjectExportRequest { DocumentId=(string)o["documentId"],ExpectedRevision=(long)o["expectedRevision"],Directory=(string)o["directory"],ExportId=id };
        }
    }
}
