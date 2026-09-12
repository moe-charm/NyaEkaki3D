using System.Linq;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Paint;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static AuthoringOperation ReadPaintStroke(JObject value)
        {
            Shape(value,"kind paintContext points radius color");
            var context=ReadPaintContext(value["paintContext"]);
            return AuthoringOperation.PaintImageStroke(context,ReadStrokePoints(value["points"]),Number(value,"radius"),ReadStrokeColor(value["color"]));
        }
        static Vec2[] ReadStrokePoints(JToken token)
        {
            var points=token as JArray;
            Checks.Require(points!=null && points.Count>0 && points.Count<=PaintStroke.MaxPoints,"INVALID_COMMAND_WIRE","Invalid stroke point count.");
            return points.Select(p=>
            {
                var pair=p as JArray;Checks.Require(pair!=null && pair.Count==2,"INVALID_COMMAND_WIRE","Expected UV pair.");
                foreach(var item in pair) Checks.Require(item.Type==JTokenType.Integer || item.Type==JTokenType.Float,"INVALID_COMMAND_WIRE","Expected numeric UV.");
                return new Vec2((float)pair[0],(float)pair[1]);
            }).ToArray();
        }
        static Rgba32 ReadStrokeColor(JToken token)
        {
            var color=token as JArray;
            Checks.Require(color!=null && color.Count==4,"INVALID_COMMAND_WIRE","Expected RGBA bytes.");
            var rgba=color.Select(ReadByte).ToArray();
            return new Rgba32(rgba[0],rgba[1],rgba[2],rgba[3]);
        }
        static byte ReadByte(JToken token)
        {
            Checks.Require(token?.Type==JTokenType.Integer && int.TryParse(token.ToString(),out var n) && n>=0 && n<=255,"INVALID_COMMAND_WIRE","Expected byte 0..255.");return (byte)token;
        }
        static int ImageDimension(JObject value,string field)
        {
            Checks.Require(value[field]?.Type==JTokenType.Integer && int.TryParse(value[field].ToString(),out _),"INVALID_COMMAND_WIRE","Expected integer image dimension.");
            return (int)value[field];
        }
        static PaintEditContext ReadPaintContext(JToken token)
        {
            var c=token as JObject;Shape(c,"graphId nodeId imageHash uvHash meshDomain");
            return PaintEditContext.FromIdentity(Text(c,"graphId"),Text(c,"nodeId"),Text(c,"imageHash"),Text(c,"uvHash"),Text(c,"meshDomain"));
        }
    }
}
