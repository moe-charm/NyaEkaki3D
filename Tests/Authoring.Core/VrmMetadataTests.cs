using System;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunVrmMetadataTests()
    {
        Test("VRM 1.0 metadata reader keeps source identity and humanoid node mapping", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["nodes"] = new JArray(new JObject { ["name"] = "Hips" }); root["extensions"] = new JObject { ["VRMC_vrm"] = new JObject { ["specVersion"] = "1.0", ["meta"] = new JObject { ["name"] = "Sample", ["authors"] = "Nya" }, ["humanoid"] = new JObject { ["humanBones"] = new JObject { ["hips"] = new JObject { ["node"] = 0 } } }, ["expressions"] = new JObject { ["preset"] = new JObject { ["happy"] = new JObject { ["morphTargetBinds"] = new JArray(new JObject { ["node"] = 0 }) } }, ["custom"] = new JObject { ["smile"] = new JObject { ["morphTargetBinds"] = new JArray(new JObject { ["node"] = 0 }, new JObject { ["node"] = 0 }), ["materialColorBinds"] = new JArray(new JObject { ["material"] = 0 }) } } } } };
            var bytes = ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)); var profile = VrmMetadataReader.Read(bytes);
            Equal("vrm1", profile.Format); Equal("1.0", profile.SpecVersion); Equal("Sample", profile.Title); Equal(0, profile.HumanoidNodes["hips"]); Equal(2, profile.Expressions.Count); Equal("happy", profile.Expressions[0].Name); Equal(1, profile.Expressions[0].MorphTargetBindCount); Equal(0, profile.Expressions[0].MaterialBindCount); Equal("smile", profile.Expressions[1].Name); True(profile.Expressions[1].IsCustom); Equal(2, profile.Expressions[1].MorphTargetBindCount); Equal(1, profile.Expressions[1].MaterialBindCount); True(VrmMetadataReader.ContainsVrm(bytes));
        });

        Test("VRM 0.x metadata reader accepts legacy humanBones array", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["nodes"] = new JArray(new JObject { ["name"] = "Hips" }); root["extensions"] = new JObject { ["VRM"] = new JObject { ["specVersion"] = "0.0", ["meta"] = new JObject { ["title"] = "Legacy", ["author"] = "Nya" }, ["humanoid"] = new JObject { ["humanBones"] = new JArray(new JObject { ["bone"] = "hips", ["node"] = 0 }) }, ["blendShapeMaster"] = new JObject { ["blendShapeGroups"] = new JArray(new JObject { ["name"] = "Joy", ["presetName"] = "happy", ["binds"] = new JArray(new JObject { ["mesh"] = 0, ["index"] = 0, ["weight"] = 1.0 }), ["materialValues"] = new JArray(new JObject { ["materialName"] = "body" }) }) } } };
            var profile = VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None))); Equal("vrm0", profile.Format); Equal("Legacy", profile.Title); Equal(0, profile.HumanoidNodes["hips"]); Equal(1, profile.Expressions.Count); Equal("Joy", profile.Expressions[0].Name); Equal("happy", profile.Expressions[0].Preset); Equal(1, profile.Expressions[0].MorphTargetBindCount); Equal(1, profile.Expressions[0].MaterialBindCount);
        });

        Test("VRM metadata reader refuses absent and unsupported extensions", () =>
        {
            Expect("UNSUPPORTED_FORMAT", () => VrmMetadataReader.Read(BuildGlb())); var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["extensions"] = new JObject { ["VRMC_vrm"] = new JObject { ["specVersion"] = "2.0", ["meta"] = new JObject(), ["humanoid"] = new JObject() } }; Expect("UNSUPPORTED_FORMAT", () => VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None))));
        });
    }
}
