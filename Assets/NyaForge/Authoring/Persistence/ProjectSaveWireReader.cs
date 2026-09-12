using System;
using System.Linq;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring
{
    public static class ProjectSaveWireReader
    {
        public static ProjectSaveRequest Read(JObject o,string instance)
        {
            Checks.Require(o!=null && o.Properties().Select(p=>p.Name).OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(new[]{"directory","documentId","expectedRevision","expectedSaveVersion"}),"INVALID_SAVE_REQUEST","Unexpected save fields.");
            foreach(var field in new[]{"directory","documentId"}) Checks.Require(o[field].Type==JTokenType.String,"INVALID_SAVE_REQUEST","Expected save string.");
            foreach(var field in new[]{"expectedRevision","expectedSaveVersion"}) Checks.Require(o[field].Type==JTokenType.Integer && long.TryParse(o[field].ToString(),out var v) && v>=0,"INVALID_SAVE_REQUEST","Expected nonnegative save number.");
            return new ProjectSaveRequest(instance,(string)o["documentId"],(long)o["expectedRevision"],(string)o["directory"],(long)o["expectedSaveVersion"]);
        }
    }
}
