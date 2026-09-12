using System;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunVrmMetadataTests()
    {
        Test("VRM 1.0 metadata reader keeps source identity and humanoid node mapping", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["nodes"] = new JArray(new JObject { ["name"] = "Hips" }); root["extensions"] = new JObject { ["VRMC_vrm"] = new JObject { ["specVersion"] = "1.0", ["meta"] = new JObject { ["name"] = "Sample", ["authors"] = "Nya" }, ["humanoid"] = new JObject { ["humanBones"] = new JObject { ["hips"] = new JObject { ["node"] = 0 } } } } };
            var bytes = ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)); var profile = VrmMetadataReader.Read(bytes);
            Equal("vrm1", profile.Format); Equal("1.0", profile.SpecVersion); Equal("Sample", profile.Title); Equal(0, profile.HumanoidNodes["hips"]); True(VrmMetadataReader.ContainsVrm(bytes));
        });

        Test("VRM 0.x metadata reader accepts legacy humanBones array", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["nodes"] = new JArray(new JObject { ["name"] = "Hips" }); root["extensions"] = new JObject { ["VRM"] = new JObject { ["specVersion"] = "0.0", ["meta"] = new JObject { ["title"] = "Legacy", ["author"] = "Nya" }, ["humanoid"] = new JObject { ["humanBones"] = new JArray(new JObject { ["bone"] = "hips", ["node"] = 0 }) } } };
            var profile = VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None))); Equal("vrm0", profile.Format); Equal("Legacy", profile.Title); Equal(0, profile.HumanoidNodes["hips"]);
        });

        Test("VRM metadata reader refuses absent and unsupported extensions", () =>
        {
            Expect("UNSUPPORTED_FORMAT", () => VrmMetadataReader.Read(BuildGlb())); var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["extensions"] = new JObject { ["VRMC_vrm"] = new JObject { ["specVersion"] = "2.0", ["meta"] = new JObject(), ["humanoid"] = new JObject() } }; Expect("UNSUPPORTED_FORMAT", () => VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None))));
        });
    }
}
