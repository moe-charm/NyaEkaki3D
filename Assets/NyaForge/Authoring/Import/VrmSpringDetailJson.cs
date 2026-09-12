using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace NyaForge.Authoring.Import
{
    /// <summary>Vector/shape JSON boundaries shared by source import and session persistence.</summary>
    internal static class VrmSpringDetailJson
    {
        internal static Vec3 ModernVector(JObject owner, string name, Vec3 defaultValue)
            => owner[name] == null ? defaultValue : Vector(owner[name]);

        internal static Vec3? LegacyVector(JObject owner, string name)
        {
            if (owner[name] == null) return null;
            var value = owner[name] as JObject;
            Checks.Require(value != null, "INVALID_VRM", "Legacy Spring vector is invalid: " + name);
            // No legacy schema default for omitted components; do not silently discard a partial vector.
            return new Vec3(Number(value["x"]), Number(value["y"]), Number(value["z"]));
        }

        internal static Vec3 Vector(JToken token)
        {
            var values = token as JArray;
            Checks.Require(values != null && values.Count == 3, "INVALID_VRM", "Spring vector needs three numbers.");
            return new Vec3(Number(values[0]), Number(values[1]), Number(values[2]));
        }

        internal static float Number(JToken token)
        {
            Checks.Require(token != null && (token.Type == JTokenType.Integer || token.Type == JTokenType.Float), "INVALID_VRM", "Spring component must be numeric.");
            float value = (float)token; Checks.Finite(value); return value;
        }

        internal static JToken WriteVector(Vec3? value)
            => value.HasValue ? (JToken)new JArray(value.Value.X, value.Value.Y, value.Value.Z) : JValue.CreateNull();
        internal static Vec3? ReadOptionalVector(JToken token)
        {
            Checks.Require(token != null, "INVALID_VRM", "Spring vector field is missing.");
            return token.Type == JTokenType.Null ? (Vec3?)null : Vector(token);
        }

        internal static VrmSpringColliderShape ModernShape(JObject shape)
        {
            Checks.Require(shape != null, "INVALID_VRM", "Spring collider shape is required.");
            bool sphere = shape["sphere"] != null, capsule = shape["capsule"] != null;
            Checks.Require(sphere ^ capsule, "INVALID_VRM", "Spring collider needs exactly one shape.");
            string kind = sphere ? "sphere" : "capsule";
            var value = shape[kind] as JObject;
            Checks.Require(value != null, "INVALID_VRM", "Spring collider shape is invalid.");
            return new VrmSpringColliderShape(kind, ModernVector(value, "offset", new Vec3()),
                value["radius"] == null ? 0 : Number(value["radius"]), capsule ? (Vec3?)ModernVector(value, "tail", new Vec3()) : null);
        }

        internal static JToken WriteShapes(IReadOnlyList<VrmSpringColliderShape> shapes)
        {
            if (shapes == null) return JValue.CreateNull();
            var values = new JArray();
            foreach (var shape in shapes) values.Add(new JObject { ["kind"] = shape.Kind, ["offset"] = WriteVector(shape.Offset), ["radius"] = shape.Radius, ["tail"] = WriteVector(shape.Tail) });
            return values;
        }

        internal static IReadOnlyList<VrmSpringColliderShape> ReadShapes(JToken token, int count)
        {
            Checks.Require(token != null, "INVALID_VRM", "Spring shapes field is missing.");
            if (token.Type == JTokenType.Null) return null;
            var values = token as JArray;
            Checks.Require(values != null && values.Count == count, "INVALID_VRM", "Spring shape count differs from collider nodes.");
            var result = new List<VrmSpringColliderShape>();
            foreach (var item in values)
            {
                var value = item as JObject;
                Checks.Require(value != null && value.Count == 4 && value["kind"]?.Type == JTokenType.String, "INVALID_VRM", "Spring shape fields are invalid.");
                result.Add(new VrmSpringColliderShape((string)value["kind"], ReadOptionalVector(value["offset"]), Number(value["radius"]), ReadOptionalVector(value["tail"])));
            }
            return result.AsReadOnly();
        }
    }
}
