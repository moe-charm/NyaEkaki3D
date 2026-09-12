using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Rig;

namespace NyaForge.Authoring.Import
{
    internal static class ImportedNodeOriginsJson
    {
        internal static JToken Write(IReadOnlyDictionary<int, Vec3> origins)
            => origins == null ? (JToken)JValue.CreateNull() : new JArray(origins.OrderBy(p => p.Key).Select(p => new JObject { ["node"] = p.Key, ["position"] = new JArray(p.Value.X, p.Value.Y, p.Value.Z) }));

        internal static IDictionary<int, Vec3> Read(JToken token)
        {
            Checks.Require(token != null, "INVALID_IMPORT", "Source node origins field is missing.");
            if (token.Type == JTokenType.Null) return null;
            var values = token as JArray;
            Checks.Require(values != null && values.Count <= SkeletonDefinition.MaxBones, "INVALID_IMPORT", "Source node origins exceed capacity.");
            var result = new Dictionary<int, Vec3>();
            foreach (var item in values)
            {
                var value = item as JObject;
                Checks.Require(value != null && value.Count == 2 && value["node"]?.Type == JTokenType.Integer, "INVALID_IMPORT", "Source node origin fields are invalid.");
                long node = (long)value["node"];
                Checks.Require(node >= 0 && node < AuthoringLimits.MaxVertices, "INVALID_IMPORT", "Source origin node is out of range.");
                var vector = value["position"] as JArray;
                Checks.Require(vector != null && vector.Count == 3, "INVALID_IMPORT", "Source origin needs three numbers.");
                var position = new Vec3(Number(vector[0]), Number(vector[1]), Number(vector[2]));
                Checks.Require(result.TryAdd((int)node, position), "INVALID_IMPORT", "Source origin node repeats.");
            }
            return result;
        }

        static float Number(JToken token)
        {
            Checks.Require(token.Type == JTokenType.Float || token.Type == JTokenType.Integer, "INVALID_IMPORT", "Source origin component must be numeric.");
            float value = (float)token; Checks.Finite(value); return value;
        }
    }
}
