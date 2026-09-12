using System;
using System.Linq;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring
{
    public static partial class CommandWireReader
    {
        static MaterialParameters ReadMaterial(JObject p)
        {
            Shape(p,"baseColor metallic roughness emission alphaMode alphaCutoff");
            Checks.Require(p["baseColor"] is JArray && p["baseColor"].Count()==4,"INVALID_COMMAND_WIRE","Expected RGBA base color.");
            var color=p["baseColor"].Select(t=> { Checks.Require(t.Type==JTokenType.Float || t.Type==JTokenType.Integer,"INVALID_COMMAND_WIRE","Expected numeric RGBA.");return (float)t; }).ToArray();
            string mode=Text(p,"alphaMode");
            Checks.Require(mode=="Opaque" || mode=="Cutout" || mode=="Blend","INVALID_COMMAND_WIRE","Unknown alpha mode.");
            return new MaterialParameters(new Vec4(color[0],color[1],color[2],color[3]),Number(p,"metallic"),Number(p,"roughness"),Vector(p["emission"]),(MaterialAlphaMode)Enum.Parse(typeof(MaterialAlphaMode),mode),Number(p,"alphaCutoff"));
        }
    }
}
