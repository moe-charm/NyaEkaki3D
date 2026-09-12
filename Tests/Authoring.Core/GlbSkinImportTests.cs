using System;
using System.IO;
using System.Linq;
using System.Text;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunGlbSkinImportTests()
    {
        RunImportedBoneMapTests();
        Test("GLB skin importer maps joints and normalized weights into the rig core", () =>
        {
            var result = GlbSkinImporter.Read(BuildSkinnedGlb());
            Equal(3, result.Mesh.VertexCount); Equal(1, result.Mesh.Submeshes.Count); Equal(2, result.Skeleton.Bones.Count); Equal(3, result.Binding.Weights.Count);
            var child = result.Skeleton.Bones.Single(b => b.Name == "Child"); Equal("Root", result.Skeleton.Bones.Single(b => b.Name == "Root").Name); Equal(result.Skeleton.Bones.Single(b => b.Name == "Root").BoneId, child.ParentBoneId);
            Near(.5f, result.Binding.Weights[1].Single(w => w.BoneId == child.BoneId).Weight); Near(1f, result.Binding.Weights[2].Sum(w => w.Weight));
            True(result.Warnings.Any(w => w.Contains("translation-only", StringComparison.Ordinal)));
        });

        Test("GLB skin importer rejects rotations rather than losing rest transforms", () =>
        {
            var bytes = BuildSkinnedGlb(); var root = JObject.Parse(ReadJsonChunk(bytes)); ((JObject)root["nodes"]![0]!)!["rotation"] = new JArray(0, 0, .1, .995); Expect("UNSUPPORTED_FORMAT", () => GlbSkinImporter.Read(ReplaceJsonChunk(bytes, root.ToString(Newtonsoft.Json.Formatting.None))));
        });
        Test("GLB skin importer selects a mesh and skin by source index", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildSkinnedGlb())); var meshes = (JArray)root["meshes"]!; meshes.Add(meshes[0]!.DeepClone());
            ((JArray)root["nodes"]!).Add(new JObject { ["name"] = "Accessory", ["mesh"] = 1, ["skin"] = 0 });
            var bytes = ReplaceJsonChunk(BuildSkinnedGlb(), root.ToString(Newtonsoft.Json.Formatting.None));
            Expect("UNSUPPORTED_FORMAT", () => GlbSkinImporter.Read(bytes));
            var selected = GlbSkinImporter.Read(bytes, 1, 0); Equal(1, selected.MeshIndex); Equal(0, selected.SkinIndex); Equal(3, selected.Mesh.VertexCount); Equal(3, selected.Binding.Weights.Count);
            Expect("INVALID_IMPORT", () => GlbSkinImporter.Read(bytes, 2, 0)); Expect("INVALID_IMPORT", () => GlbSkinImporter.Read(bytes, 1, 1));
        });
    }

    static byte[] BuildSkinnedGlb(bool joints16 = false, bool normalizedWeights = false)
    {
        using (var bin = new MemoryStream()) using (var b = new BinaryWriter(bin))
        {
            foreach (var p in new[] { new Vec3(0, 0, 0), new Vec3(.1f, 0, 0), new Vec3(0, .1f, 0) }) { b.Write(p.X); b.Write(p.Y); b.Write(p.Z); }
            foreach (ushort i in new ushort[] { 0, 1, 2 }) b.Write(i);
            while (bin.Length % 4 != 0) b.Write((byte)0);
            foreach (byte[] row in new[] { new byte[] { 0, 0, 0, 0 }, new byte[] { 0, 1, 0, 0 }, new byte[] { 1, 0, 0, 0 } })
            {
                if (!joints16) b.Write(row);
                else foreach (byte value in row) b.Write((ushort)value);
            }
            if (normalizedWeights)
                foreach (var row in new[] { new byte[] { 255, 0, 0, 0 }, new byte[] { 128, 128, 0, 0 }, new byte[] { 0, 255, 0, 0 } }) b.Write(row);
            else
                foreach (var row in new[] { new[] { 1f, 0f, 0f, 0f }, new[] { .5f, .5f, 0f, 0f }, new[] { 0f, 1f, 0f, 0f } }) foreach (float value in row) b.Write(value);
            WriteIdentity(b); WriteTranslationInverse(b, .1f);
            int weightsOffset = joints16 ? 68 : 56, weightsLength = normalizedWeights ? 12 : 48, inverseOffset = weightsOffset + weightsLength;
            var json = new JObject
            {
                ["asset"] = new JObject { ["version"] = "2.0" }, ["buffers"] = new JArray(new JObject { ["byteLength"] = (int)bin.Length }),
                ["bufferViews"] = new JArray(new JObject { ["buffer"] = 0, ["byteOffset"] = 0, ["byteLength"] = 36 }, new JObject { ["buffer"] = 0, ["byteOffset"] = 36, ["byteLength"] = 6 }, new JObject { ["buffer"] = 0, ["byteOffset"] = 44, ["byteLength"] = joints16 ? 24 : 12 }, new JObject { ["buffer"] = 0, ["byteOffset"] = weightsOffset, ["byteLength"] = weightsLength }, new JObject { ["buffer"] = 0, ["byteOffset"] = inverseOffset, ["byteLength"] = 128 }),
                ["accessors"] = new JArray(new JObject { ["bufferView"] = 0, ["componentType"] = 5126, ["count"] = 3, ["type"] = "VEC3" }, new JObject { ["bufferView"] = 1, ["componentType"] = 5123, ["count"] = 3, ["type"] = "SCALAR" }, new JObject { ["bufferView"] = 2, ["componentType"] = joints16 ? 5123 : 5121, ["count"] = 3, ["type"] = "VEC4" }, new JObject { ["bufferView"] = 3, ["componentType"] = normalizedWeights ? 5121 : 5126, ["normalized"] = normalizedWeights, ["count"] = 3, ["type"] = "VEC4" }, new JObject { ["bufferView"] = 4, ["componentType"] = 5126, ["count"] = 2, ["type"] = "MAT4" }),
                ["meshes"] = new JArray(new JObject { ["primitives"] = new JArray(new JObject { ["attributes"] = new JObject { ["POSITION"] = 0, ["JOINTS_0"] = 2, ["WEIGHTS_0"] = 3 }, ["indices"] = 1 }) }),
                ["nodes"] = new JArray(new JObject { ["name"] = "Root", ["children"] = new JArray(1) }, new JObject { ["name"] = "Child", ["translation"] = new JArray(0, .1, 0) }),
                ["skins"] = new JArray(new JObject { ["joints"] = new JArray(0, 1), ["inverseBindMatrices"] = 4 })
            };
            return BuildGlbContainer(Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None)), bin.ToArray());
        }
    }

    static void WriteIdentity(BinaryWriter writer) { for (int i = 0; i < 16; i++) writer.Write(i == 0 || i == 5 || i == 10 || i == 15 ? 1f : 0f); }
    static void WriteTranslationInverse(BinaryWriter writer, float y) { for (int i = 0; i < 16; i++) writer.Write(i == 0 || i == 5 || i == 10 || i == 15 ? 1f : i == 13 ? -y : 0f); }
}
