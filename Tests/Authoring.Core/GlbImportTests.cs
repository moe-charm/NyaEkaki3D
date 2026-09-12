using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunGlbImportTests()
    {
        Test("GLB importer retains static geometry and POSITION morphs", () =>
        {
            var result = GlbImporter.Read(BuildGlb());
            Equal(4, result.Mesh.VertexCount); Equal(2, result.Mesh.TriangleCount); Equal(1, result.Mesh.Submeshes.Count);
            Equal(1, result.Morphs.Targets.Count); Equal("Smile", result.Morphs.Targets[0].Name); Equal(1, result.Morphs.Targets[0].Deltas.Count);
            var deformed = NyaForge.Authoring.Rig.MorphDeformer.Apply(result.Mesh, result.Morphs, result.Morphs.Targets.ToDictionary(t => t.TargetId, _ => .5f));
            Near(result.Mesh.Positions[0].X + .05f, deformed.Positions[0].X); Near(result.Mesh.Positions[1].X, deformed.Positions[1].X);
            True(result.Warnings.Any(w => w.Contains("static triangle", StringComparison.Ordinal)));
        });

        Test("GLB importer combines bounded primitives and rejects unsupported structure", () =>
        {
            var bytes = BuildGlb(); var root = JObject.Parse(ReadJsonChunk(bytes));
            ((JArray)root["meshes"]![0]!["primitives"]!).Add(((JArray)root["meshes"]![0]!["primitives"]!)[0]!.DeepClone());
            var changed = ReplaceJsonChunk(bytes, root.ToString(Newtonsoft.Json.Formatting.None));
            var combined = GlbImporter.Read(changed);
            Equal(8, combined.Mesh.VertexCount); Equal(2, combined.Mesh.Submeshes.Count); Equal(4, combined.Mesh.TriangleCount);
            Equal(1, combined.Morphs.Targets.Count); Equal(2, combined.Morphs.Targets[0].Deltas.Count);
            var skinRoot = JObject.Parse(ReadJsonChunk(bytes)); skinRoot["skins"] = new JArray(new JObject()); skinRoot["nodes"] = new JArray(new JObject { ["mesh"] = 0, ["skin"] = 0 });
            var skin = ReplaceJsonChunk(bytes, skinRoot.ToString(Newtonsoft.Json.Formatting.None)); Expect("UNSUPPORTED_FORMAT", () => GlbImporter.Read(skin));
            var bad = (byte[])bytes.Clone(); bad[0] = 0; Expect("INVALID_IMPORT", () => GlbImporter.Read(bad));
        });

        Test("GLB importer selects a mesh by source index without collapsing morph identity", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb()));
            var meshes = (JArray)root["meshes"]!; var second = (JObject)meshes[0]!.DeepClone(); ((JObject)second["extras"]!)["targetNames"] = new JArray("AccessorySmile"); meshes.Add(second);
            var bytes = ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None));
            Expect("UNSUPPORTED_FORMAT", () => GlbImporter.Read(bytes));
            var selected = GlbImporter.Read(bytes, 1); var first = GlbImporter.Read(bytes, 0); Equal(1, selected.MeshIndex); Equal("AccessorySmile", selected.Morphs.Targets[0].Name); True(first.Morphs.Targets[0].TargetId != selected.Morphs.Targets[0].TargetId); Equal(ChecksHashForTest(bytes), selected.SourceHash);
            Expect("INVALID_IMPORT", () => GlbImporter.Read(bytes, -1)); Expect("INVALID_IMPORT", () => GlbImporter.Read(bytes, 2));
        });
        Test("GLB importer applies a selected node affine frame to geometry and morphs", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb()));
            root["nodes"] = new JArray(new JObject { ["mesh"] = 0, ["translation"] = new JArray(1, 2, 3), ["rotation"] = new JArray(0, 0, Math.Sqrt(.5), Math.Sqrt(.5)), ["scale"] = new JArray(2, 3, 4) });
            var bytes = ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None));
            var instance = GlbSceneInventoryReader.Read(bytes).Instances.Single();
            var selected = GlbImporter.Read(bytes, 0, instance.WorldTransform);
            Near(1.15f, selected.Mesh.Positions[0].X); Near(1.8f, selected.Mesh.Positions[0].Y);
            Near(0f, selected.Morphs.Targets[0].Deltas.Single().Value.X); Near(.2f, selected.Morphs.Targets[0].Deltas.Single().Value.Y);
            True(selected.Warnings.Any(w => w.Contains("world transform", StringComparison.Ordinal)));
        });
        Test("GLB importer allows an unskinned accessory beside a skinned mesh", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb()));
            root["skins"] = new JArray(new JObject { ["joints"] = new JArray(0) });
            ((JArray)root["meshes"]!).Add(((JArray)root["meshes"]!)[0]!.DeepClone());
            root["nodes"] = new JArray(new JObject { ["mesh"] = 0, ["skin"] = 0 }, new JObject { ["mesh"] = 1 });
            var bytes = ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None));
            True(GlbImporter.Read(bytes, 1).Mesh.VertexCount == 4);
            Expect("UNSUPPORTED_FORMAT", () => GlbImporter.Read(bytes, 0));
        });
    }

    static byte[] BuildGlb()
    {
        using (var bin = new MemoryStream()) using (var b = new BinaryWriter(bin))
        {
            foreach (var p in new[] { new Vec3(-.1f, -.05f, 0), new Vec3(.1f, -.05f, 0), new Vec3(.1f, .05f, 0), new Vec3(-.1f, .05f, 0) }) { b.Write(p.X); b.Write(p.Y); b.Write(p.Z); }
            foreach (ushort i in new ushort[] { 0, 2, 1, 0, 3, 2 }) b.Write(i);
            foreach (var p in new[] { new Vec3(.1f, 0, 0), new Vec3(), new Vec3(), new Vec3() }) { b.Write(p.X); b.Write(p.Y); b.Write(p.Z); }
            while (bin.Length % 4 != 0) b.Write((byte)0);
            var json = new JObject
            {
                ["asset"] = new JObject { ["version"] = "2.0" },
                ["buffers"] = new JArray(new JObject { ["byteLength"] = (int)bin.Length }),
                ["bufferViews"] = new JArray(
                    new JObject { ["buffer"] = 0, ["byteOffset"] = 0, ["byteLength"] = 48 },
                    new JObject { ["buffer"] = 0, ["byteOffset"] = 48, ["byteLength"] = 12 },
                    new JObject { ["buffer"] = 0, ["byteOffset"] = 60, ["byteLength"] = 48 }),
                ["accessors"] = new JArray(
                    new JObject { ["bufferView"] = 0, ["componentType"] = 5126, ["count"] = 4, ["type"] = "VEC3" },
                    new JObject { ["bufferView"] = 1, ["componentType"] = 5123, ["count"] = 6, ["type"] = "SCALAR" },
                    new JObject { ["bufferView"] = 2, ["componentType"] = 5126, ["count"] = 4, ["type"] = "VEC3" }),
                ["meshes"] = new JArray(new JObject
                {
                    ["extras"] = new JObject { ["targetNames"] = new JArray("Smile") },
                    ["primitives"] = new JArray(new JObject { ["attributes"] = new JObject { ["POSITION"] = 0 }, ["indices"] = 1, ["targets"] = new JArray(new JObject { ["POSITION"] = 2 }) })
                })
            };
            return BuildGlbContainer(Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None)), bin.ToArray());
        }
    }

    static byte[] BuildGlbContainer(byte[] json, byte[] bin)
    {
        while (json.Length % 4 != 0) json = json.Concat(new byte[] { 0x20 }).ToArray();
        using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
        {
            writer.Write(0x46546c67); writer.Write(2); writer.Write(12 + 8 + json.Length + 8 + bin.Length);
            writer.Write(json.Length); writer.Write(0x4e4f534a); writer.Write(json);
            writer.Write(bin.Length); writer.Write(0x004e4942); writer.Write(bin); return stream.ToArray();
        }
    }

    static string ReadJsonChunk(byte[] bytes)
    {
        int length = BitConverter.ToInt32(bytes, 12); return Encoding.UTF8.GetString(bytes, 20, length).TrimEnd(' ', '\0', '\n', '\r', '\t');
    }

    static byte[] ReplaceJsonChunk(byte[] bytes, string json)
    {
        var bin = bytes.Skip(20 + BitConverter.ToInt32(bytes, 12) + 8).ToArray(); return BuildGlbContainer(Encoding.UTF8.GetBytes(json), bin);
    }
}
