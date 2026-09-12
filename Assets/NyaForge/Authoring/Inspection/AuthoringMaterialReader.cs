using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Inspection
{
    internal static class AuthoringMaterialReader
    {
        internal static JToken Read(GraphMaterialValue material)
        {
            if(material==null) return JValue.CreateNull();
            var p=material.Parameters;
            return new JObject
            {
                ["contentHash"]=p.ContentHash,
                ["baseColorLinear"]=new JArray(p.BaseColor.X,p.BaseColor.Y,p.BaseColor.Z,p.BaseColor.W),
                ["metallic"]=p.Metallic,["roughness"]=p.Roughness,
                ["emissionLinear"]=new JArray(p.Emission.X,p.Emission.Y,p.Emission.Z),
                ["alphaMode"]=p.AlphaMode.ToString(),["alphaCutoff"]=p.AlphaCutoff,
                ["baseColorImage"]=material.BaseColor==null ? JValue.CreateNull() : (JToken)new JObject
                {
                    ["imageHash"]=material.BaseColor.ImageHash,["uvHash"]=material.BaseColor.UvHash,["meshDomain"]=material.BaseColor.MeshDomain,
                    ["width"]=material.BaseColor.Image.Width,["height"]=material.BaseColor.Image.Height
                }
            };
        }
    }
}
