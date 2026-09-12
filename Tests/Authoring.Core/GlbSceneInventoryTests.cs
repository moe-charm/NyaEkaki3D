using System;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunGlbSceneInventoryTests()
    {
        Test("GLB scene inventory retains mesh instance and skin source indices", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb()));
            root["meshes"]!.Last!.AddAfterSelf(((JArray)root["meshes"]!)[0]!.DeepClone());
            root["nodes"] = new JArray(
                new JObject { ["name"] = "Root", ["translation"] = new JArray(1, 0, 0), ["children"] = new JArray(1, 2) },
                new JObject { ["name"] = "Body", ["mesh"] = 0, ["translation"] = new JArray(0, 2, 0), ["skin"] = 0 },
                new JObject { ["name"] = "Accessory", ["mesh"] = 1, ["translation"] = new JArray(0, 0, 3) });
            root["skins"] = new JArray(new JObject { ["joints"] = new JArray(0) });
            var result = GlbSceneInventoryReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)));
            Equal(2, result.Meshes.Count); Equal(2, result.Instances.Count); Equal(1, result.Skins.Count);
            Equal(0, result.Instances[0].MeshIndex); Equal(0, result.Instances[0].SkinIndex.Value); Equal(1, result.Instances[1].MeshIndex); True(!result.Instances[1].SkinIndex.HasValue);
            Near(1, result.Instances[0].WorldTransform.TransformPoint(new NyaForge.Authoring.Vec3()).X); Near(2, result.Instances[0].WorldTransform.TransformPoint(new NyaForge.Authoring.Vec3()).Y);
            Near(1, result.Instances[1].WorldTransform.TransformPoint(new NyaForge.Authoring.Vec3()).X); Near(3, result.Instances[1].WorldTransform.TransformPoint(new NyaForge.Authoring.Vec3()).Z);
            Equal(0, result.Skins[0].Joints[0]); Equal(ChecksHashForTest(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None))), result.SourceHash);
        });

        Test("GLB scene inventory keeps mesh resources without node instances", () =>
        {
            var result = GlbSceneInventoryReader.Read(BuildGlb());
            Equal(1, result.Meshes.Count); Equal(0, result.Instances.Count); True(result.NodeTransforms == null);
        });

        Test("GLB scene inventory rejects broken references and duplicate joints", () =>
        {
            var bytes = BuildGlb();
            var root = JObject.Parse(ReadJsonChunk(bytes));
            root["nodes"] = new JArray(new JObject { ["mesh"] = 0, ["skin"] = 0 });
            root["skins"] = new JArray(new JObject { ["joints"] = new JArray(0) });
            var valid = ReplaceJsonChunk(bytes, root.ToString(Newtonsoft.Json.Formatting.None));
            var badMesh = JObject.Parse(ReadJsonChunk(valid)); badMesh["nodes"]![0]!["mesh"] = 9;
            Expect("INVALID_IMPORT", () => GlbSceneInventoryReader.Read(ReplaceJsonChunk(valid, badMesh.ToString(Newtonsoft.Json.Formatting.None))));
            var badJoint = JObject.Parse(ReadJsonChunk(valid)); badJoint["skins"]![0]!["joints"] = new JArray(0, 0);
            Expect("INVALID_IMPORT", () => GlbSceneInventoryReader.Read(ReplaceJsonChunk(valid, badJoint.ToString(Newtonsoft.Json.Formatting.None))));
            var skinOnly = JObject.Parse(ReadJsonChunk(valid)); skinOnly["nodes"]![0]!["mesh"] = null;
            Expect("INVALID_IMPORT", () => GlbSceneInventoryReader.Read(ReplaceJsonChunk(valid, skinOnly.ToString(Newtonsoft.Json.Formatting.None))));
            var badNodes = JObject.Parse(ReadJsonChunk(valid)); badNodes["nodes"] = new JObject();
            Expect("INVALID_IMPORT", () => GlbSceneInventoryReader.Read(ReplaceJsonChunk(valid, badNodes.ToString(Newtonsoft.Json.Formatting.None))));
            var badSkins = JObject.Parse(ReadJsonChunk(valid)); badSkins["skins"] = new JObject();
            Expect("INVALID_IMPORT", () => GlbSceneInventoryReader.Read(ReplaceJsonChunk(valid, badSkins.ToString(Newtonsoft.Json.Formatting.None))));
        });
    }
}
