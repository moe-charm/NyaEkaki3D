using System;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunSpringDetailTests()
    {
        RunVrmColliderAdapterTests();
        RunVrmSpringChainTests();
        Test("Spring old session geometry remains unknown after version 3 migration", () =>
        {
            foreach (int version in new[] { 1, 2 })
            {
                var root = new JObject { ["version"] = version, ["sourceHash"] = new string('a', 64), ["format"] = "vrm1", ["title"] = "old",
                    [version == 1 ? "author" : "authors"] = version == 1 ? (JToken)new JValue("fixture") : new JArray("fixture"),
                    ["springBones"] = new JArray(new JObject { ["name"] = "tail", ["centerNode"] = -1, ["rootBoneNodes"] = new JArray(), ["colliderGroupIndices"] = new JArray(0), ["joints"] = new JArray(new JObject { ["node"] = 1, ["hitRadius"] = 0, ["stiffness"] = 1, ["gravityPower"] = 1, ["dragForce"] = .5 }) }),
                    ["colliderGroups"] = new JArray(new JObject { ["node"] = 0, ["colliderCount"] = 2, ["nodes"] = new JArray(0, 0) }) };
                var session = VrmSpringSessionCodec.Read(Encoding.UTF8.GetBytes(root.ToString()));
                False(session.HasCompleteDetails); True(session.ColliderGroups[0].Shapes == null); False(session.SpringBones[0].Joints[0].GravityDirection.HasValue);
                var bytes = VrmSpringSessionCodec.Write(session); var reopened = VrmSpringSessionCodec.Read(bytes);
                Equal(3, (int)JObject.Parse(Encoding.UTF8.GetString(bytes))["version"]); False(reopened.HasCompleteDetails); True(reopened.ColliderGroups[0].Shapes == null);
            }
        });
        Test("Spring shape JSON handles defaults and rejects malformed or partial vectors", () =>
        {
            var shape = VrmSpringDetailJson.ModernShape(new JObject { ["capsule"] = new JObject() });
            Near(0, shape.Radius); SpringPointNear(new Vec3(), shape.Offset.Value); SpringPointNear(new Vec3(), shape.Tail.Value);
            Expect("INVALID_VRM", () => VrmSpringDetailJson.ModernShape(new JObject { ["sphere"] = new JObject { ["offset"] = new JArray(1, 2) } }));
            Expect("INVALID_VRM", () => VrmSpringDetailJson.ModernShape(new JObject { ["sphere"] = new JObject { ["radius"] = -1 } }));
            Expect("INVALID_VRM", () => VrmSpringDetailJson.LegacyVector(new JObject { ["offset"] = new JObject { ["x"] = 1 } }, "offset"));
            True(!VrmSpringDetailJson.LegacyVector(new JObject(), "offset").HasValue);
            Expect("INVALID_VRM", () => VrmSpringDetailJson.ReadShapes(new JArray(), 1));
            Expect("INVALID_VRM", () => VrmSpringDetailJson.ReadShapes(new JArray(new JObject { ["kind"] = "sphere", ["radius"] = 1, ["offset"] = new JArray(0, 0, 0), ["tail"] = new JArray(1, 0, 0) }), 1));
        });
    }
}
