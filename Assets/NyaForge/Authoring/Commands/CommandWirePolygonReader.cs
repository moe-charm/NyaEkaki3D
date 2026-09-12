using System.Globalization;
using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static GraphNode ReadPolygon(string id,JObject parameters)
        {
            Shape(parameters,"domainId vertices faces scale translation");
            var vertices=parameters["vertices"] as JArray;var faces=parameters["faces"] as JArray;
            Checks.Require(vertices!=null && vertices.Count<=4096 && faces!=null && faces.Count<=1024,"INVALID_COMMAND_WIRE","Polygon wire collections exceed limits.");
            var mesh=new PolygonMesh(Text(parameters,"domainId"),vertices.Select(t=>
            {
                var v=t as JObject;Shape(v,"id position");return new CageVertex(ElementId(v["id"]),Vector(v["position"]));
            }),faces.Select(t=>
            {
                var f=t as JObject;Shape(f,"id materialSlot corners");
                Checks.Require(f["materialSlot"].Type==JTokenType.Integer && int.TryParse(f["materialSlot"].ToString(),out _),"INVALID_COMMAND_WIRE","Invalid material slot.");
                var corners=f["corners"] as JArray;
                Checks.Require(corners!=null && corners.Count>=3 && corners.Count<=256,"INVALID_COMMAND_WIRE","Face requires 3..256 corners.");
                return new CageFace(ElementId(f["id"]),(int)f["materialSlot"],corners.Select(c=>
                {
                    var corner=c as JObject;Shape(corner,"id vertexId");
                    return new CageCorner(ElementId(corner["id"]),ElementId(corner["vertexId"]));
                }));
            }));
            return GraphNode.Polygon(id,mesh,new RestTransform(Number(parameters,"scale"),Vector(parameters["translation"])));
        }

        static ulong ElementId(JToken token)
        {
            Checks.Require(token?.Type==JTokenType.String,"INVALID_COMMAND_WIRE","Element IDs must be decimal strings.");
            string text=(string)token;
            Checks.Require(ulong.TryParse(text,NumberStyles.None,CultureInfo.InvariantCulture,out var id) && id!=0 && id.ToString(CultureInfo.InvariantCulture)==text,"INVALID_COMMAND_WIRE","Invalid canonical element ID.");
            return id;
        }
    }
}
