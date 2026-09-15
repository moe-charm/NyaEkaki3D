using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunGlbSourceSkinImporterTests()
    {
        Test("GLB source skin importer preserves all dense weight sets and produces a source deformer candidate", () =>
        {
            var bytes = BuildSkinnedGlb(); var result = GlbSourceSkinImporter.Read(bytes);
            Equal(ChecksHashForTest(bytes), result.SourceHash); Equal(result.MeshSource.Mesh.VertexCount, result.Binding.Weights.Count);
            Equal(2, result.Skin.Joints.Count); Equal(1, result.Binding.Weights[0].Count); Equal(0, result.Binding.Weights[0][0].JointSlot);
            var rest = result.Skin.Joints.Select((_,slot) => result.Skin.Nodes.World[result.Skin.Joints[slot]]).ToArray();
            var output = SourceSkinDeformer.Apply(result.MeshSource.Mesh,result.Skin,result.Binding,rest);
            for(int i=0;i<output.VertexCount;i++) SpringPointNear(result.MeshSource.Mesh.Positions[i],output.Positions[i]);
        });
        Test("GLB source skin importer decodes 8-bit and 16-bit JOINTS with identical slot values", () =>
        {
            var eight = GlbSourceSkinImporter.Read(BuildSkinnedGlb());
            var sixteen = GlbSourceSkinImporter.Read(BuildSkinnedGlb(true));
            var eightWeights = eight.Binding.Weights.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value).ToArray();
            var sixteenWeights = sixteen.Binding.Weights.OrderBy(pair => pair.Key).SelectMany(pair => pair.Value).ToArray();
            Equal(eightWeights.Length, sixteenWeights.Length);
            for (int i = 0; i < eightWeights.Length; i++)
            {
                Equal(eightWeights[i].VertexIndex, sixteenWeights[i].VertexIndex);
                Equal(eightWeights[i].JointSlot, sixteenWeights[i].JointSlot);
                Near(eightWeights[i].Weight, sixteenWeights[i].Weight);
            }
        });
        Test("GLB skin importers decode normalized integer weights", () =>
        {
            var bytes = BuildSkinnedGlb(false, true);
            var source = GlbSourceSkinImporter.Read(bytes);
            var skin = GlbSkinImporter.Read(bytes);
            Near(1f, source.Binding.Weights[0].Single().Weight);
            Near(.5f, source.Binding.Weights[1].Single(weight => weight.JointSlot == 0).Weight);
            Near(.5f, skin.Binding.Weights[1].First(weight => weight.BoneId != skin.Binding.Weights[0][0].BoneId).Weight);
        });
        Test("GLB skin importers apply sparse accessor overrides to base weights", () =>
        {
            var bytes = BuildSparseWeightGlb();
            var source = GlbSourceSkinImporter.Read(bytes); var skin = GlbSkinImporter.Read(bytes);
            Near(.75f, source.Binding.Weights[1].Single(weight => weight.JointSlot == 0).Weight);
            Near(.25f, source.Binding.Weights[1].Single(weight => weight.JointSlot == 1).Weight);
            Near(.75f, skin.Binding.Weights[1].Single(weight => weight.BoneId == skin.Skeleton.Bones[0].BoneId).Weight);
            Near(.25f, skin.Binding.Weights[1].Single(weight => weight.BoneId == skin.Skeleton.Bones[1].BoneId).Weight);
        });
        Test("GLB skin importers reject negative weight components before filtering", () =>
        {
            var bytes = BuildSkinnedGlb(false, false, true);
            Expect("INVALID_WEIGHT", () => GlbSourceSkinImporter.Read(bytes));
            Expect("INVALID_WEIGHT", () => GlbSkinImporter.Read(bytes));
        });
        Test("GLB source skin importer finds paired JOINTS_1 and WEIGHTS_1", () =>
        {
            var bytes = BuildSkinnedGlb(); var root=JObject.Parse(ReadJsonChunk(bytes));
            var attrs=(JObject)((JObject)((JArray)root["meshes"])[0])["primitives"][0]["attributes"];
            attrs["JOINTS_1"]=2; attrs["WEIGHTS_1"]=3;
            // Reusing the same accessors intentionally exposes duplicate source slots, which must be diagnosed.
            Expect("DUPLICATE_WEIGHT",()=>GlbSourceSkinImporter.Read(ReplaceJsonChunk(bytes,root.ToString())));
        });
        Test("GLB source skin importer selects a mesh while retaining the shared source skin", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildSkinnedGlb())); var meshes = (JArray)root["meshes"]!; meshes.Add(meshes[0]!.DeepClone());
            ((JArray)root["nodes"]!).Add(new JObject { ["name"] = "Accessory", ["mesh"] = 1, ["skin"] = 0 });
            var bytes = ReplaceJsonChunk(BuildSkinnedGlb(), root.ToString(Newtonsoft.Json.Formatting.None));
            Expect("UNSUPPORTED_FORMAT", () => GlbSourceSkinImporter.Read(bytes));
            var selected = GlbSourceSkinImporter.Read(bytes, 1, 0); Equal(1, selected.MeshIndex); Equal(0, selected.SkinIndex); Equal(3, selected.MeshSource.Mesh.VertexCount); Equal(3, selected.Binding.Weights.Count);
            Equal(ChecksHashForTest(bytes), selected.SourceHash); Equal(selected.SourceHash, selected.MeshSource.SourceHash); Equal(selected.SourceHash, selected.Skin.Nodes.SourceHash);
        });
        Test("GLB source skin importer rejects unpaired, noncontiguous and malformed attribute sets", () =>
        {
            var baseBytes=BuildSkinnedGlb();
            var cases=new (Action<JObject> edit,string code)[] {
                (root=>((JObject)((JArray)root["meshes"])[0])["primitives"][0]["attributes"]["WEIGHTS_1"]=3,"UNSUPPORTED_FORMAT"),
                (root=>((JObject)((JArray)root["meshes"])[0])["primitives"][0]["attributes"]["JOINTS_2"]=2,"UNSUPPORTED_FORMAT"),
                (root=>((JObject)((JArray)root["meshes"])[0])["primitives"][0]["attributes"]["JOINTS_0"]=999,"INVALID_IMPORT"),
                (root=>root["accessors"][2]["componentType"]=5126,"UNSUPPORTED_FORMAT"),
                (root=>root["accessors"][3]["type"]="VEC3","UNSUPPORTED_FORMAT"),
                (root=>root["accessors"][3]["sparse"]=new JObject(),"INVALID_IMPORT"),
                (root=>root["bufferViews"][2]["byteOffset"]=45,"INVALID_IMPORT"),
                (root=>root["buffers"][0]["byteLength"]=1,"INVALID_IMPORT")
            };
            foreach(var test in cases)
            {
                var root=JObject.Parse(ReadJsonChunk(baseBytes)); test.edit(root);
                var changed=ReplaceJsonChunk(baseBytes,root.ToString()); Expect(test.code,()=>GlbSourceSkinImporter.Read(changed));
            }
        });
    }

    static string ChecksHashForTest(byte[] bytes)
    {
        using(var sha=System.Security.Cryptography.SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-","").ToLowerInvariant();
    }

    static byte[] BuildSparseWeightGlb()
    {
        var source = BuildSkinnedGlb(); var root = JObject.Parse(ReadJsonChunk(source)); var bin = ReadBinChunk(source).ToList();
        while (bin.Count % 4 != 0) bin.Add(0);
        int indexOffset = bin.Count; bin.Add(1); while (bin.Count % 4 != 0) bin.Add(0);
        int valueOffset = bin.Count;
        using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
        {
            writer.Write(.75f); writer.Write(.25f); writer.Write(0f); writer.Write(0f); writer.Flush(); bin.AddRange(stream.ToArray());
        }
        var views = (JArray)root["bufferViews"]!; int indexView = views.Count; views.Add(new JObject { ["buffer"] = 0, ["byteOffset"] = indexOffset, ["byteLength"] = 1 }); int valueView = views.Count; views.Add(new JObject { ["buffer"] = 0, ["byteOffset"] = valueOffset, ["byteLength"] = 16 });
        var weightAccessor = (JObject)((JArray)root["accessors"]!)[3]!;
        weightAccessor["sparse"] = new JObject { ["count"] = 1, ["indices"] = new JObject { ["bufferView"] = indexView, ["componentType"] = 5121 }, ["values"] = new JObject { ["bufferView"] = valueView } };
        ((JObject)((JArray)root["buffers"]!)[0]!)["byteLength"] = bin.Count;
        return BuildGlbContainer(Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None)), bin.ToArray());
    }
}
