using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static GraphEditContext ReadContext(JToken token)
        {
            var context=token as JObject;Shape(context,"graphId nodeId inputSnapshot domainId");
            return GraphEditContext.FromIdentity(Text(context,"graphId"),Text(context,"nodeId"),Text(context,"inputSnapshot"),Text(context,"domainId"));
        }
    }
}
