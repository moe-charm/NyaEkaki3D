using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Paint;

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
        Test("GLB importer rejects an unretained TEXCOORD_1 set", () =>
        {
            var source = BuildGlb(); var root = JObject.Parse(ReadJsonChunk(source)); var bin = ReadBinChunk(source);
            int offset = bin.Length;
            using (var extra = new MemoryStream()) using (var writer = new BinaryWriter(extra))
            {
                for (int i = 0; i < 4; i++) { writer.Write(i == 1 || i == 2 ? 1f : 0f); writer.Write(i >= 2 ? 1f : 0f); }
                var combined = bin.Concat(extra.ToArray()).ToArray();
                ((JObject)root["buffers"]![0]!)!["byteLength"] = combined.Length;
                ((JArray)root["bufferViews"]!).Add(new JObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = extra.Length });
                ((JArray)root["accessors"]!).Add(new JObject { ["bufferView"] = ((JArray)root["bufferViews"]!).Count - 1, ["componentType"] = 5126, ["count"] = 4, ["type"] = "VEC2" });
                var primitive = (JObject)((JArray)((JObject)((JArray)root["meshes"]!)[0]!) ["primitives"]!)[0]!;
                ((JObject)primitive["attributes"]!)["TEXCOORD_1"] = ((JArray)root["accessors"]!).Count - 1;
                var bytes = BuildGlbContainer(Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None)), combined);
                Expect("UNSUPPORTED_UV_SET", () => GlbImporter.Read(bytes));
                // The same contract applies when the selected mesh is skinned:
                // the skin importer routes geometry through this static adapter.
                root["skins"] = new JArray(new JObject { ["joints"] = new JArray(0) });
                root["nodes"] = new JArray(new JObject { ["mesh"] = 0, ["skin"] = 0 });
                var skinned = BuildGlbContainer(Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None)), combined);
                Expect("UNSUPPORTED_UV_SET", () => GlbSkinImporter.Read(skinned, 0, 0));
            }
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
        Test("GLB importer reports normal and tangent morph loss explicitly", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb()));
            var primitive = (JObject)((JArray)root["meshes"]![0]!["primitives"]!)[0]!;
            var target = (JObject)((JArray)primitive["targets"]!)[0]!;
            target["NORMAL"] = 2; target["TANGENT"] = 2;
            var bytes = ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None));
            var imported = GlbImporter.Read(bytes);
            True(imported.Morphs != null && imported.Warnings.Any(w => w.Contains("normal/tangent morph", StringComparison.Ordinal)));
        });
        Test("GLB importer reports material, animation and extension loss explicitly", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb()));
            root["materials"] = new JArray(new JObject()); root["animations"] = new JArray(new JObject());
            root["extensionsUsed"] = new JArray("EXT_test"); root["extensionsRequired"] = new JArray("EXT_required");
            var primitive = (JObject)((JArray)((JObject)((JArray)root["meshes"]!)[0]!)["primitives"]!)[0]!;
            primitive["material"] = 0;
            var bytes = ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None));
            Expect("UNSUPPORTED_EXTENSION", () => GlbImporter.Read(bytes));
            root["extensionsRequired"] = new JArray();
            var imported = GlbImporter.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)));
            True(imported.Diagnostics.Any(item => item.Code == "MATERIALS_NOT_RETAINED" && item.IsBlocking));
            True(imported.Diagnostics.Any(item => item.Code == "ANIMATIONS_NOT_RETAINED" && item.IsBlocking));
            True(imported.Diagnostics.Any(item => item.Code == "EXTENSIONS_PARTIAL" && !item.IsBlocking));
            True(imported.Warnings.Any(item => item.Contains("MATERIALS_NOT_RETAINED", StringComparison.Ordinal)));
        });
        Test("GLB importer retains bounded PBR material factors for native graph routing", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb()));
            root["materials"] = new JArray(new JObject
            {
                ["name"] = "Red lacquer",
                ["pbrMetallicRoughness"] = new JObject
                {
                    ["baseColorFactor"] = new JArray(.5, .25, 1.0, .75),
                    ["metallicFactor"] = .7,
                    ["roughnessFactor"] = .2,
                    ["baseColorTexture"] = new JObject { ["index"] = 0 }
                },
                ["emissiveFactor"] = new JArray(.1, .2, .3),
                ["alphaMode"] = "BLEND",
                ["alphaCutoff"] = .3
            });
            var primitive = (JObject)((JArray)((JObject)((JArray)root["meshes"]!)[0]!) ["primitives"]!)[0]!;
            primitive["material"] = 0;
            var imported = GlbImporter.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)));
            Equal(1, imported.Materials.Count);
            var material = imported.Materials[0];
            Equal(0, material.SubmeshIndex); Equal(0, material.SourceMaterialIndex); Equal("Red lacquer", material.Name); True(material.HasTextureReferences);
            Near(.5f, material.Parameters.BaseColor.X); Near(.25f, material.Parameters.BaseColor.Y); Near(1f, material.Parameters.BaseColor.Z); Near(.75f, material.Parameters.BaseColor.W);
            True(material.Parameters.BaseColor.X >= 0f && material.Parameters.BaseColor.X <= 1f && material.Parameters.BaseColor.Y <= 1f && material.Parameters.BaseColor.Z <= 1f && material.Parameters.BaseColor.W <= 1f);
            Near(.7f, material.Parameters.Metallic); Near(.2f, material.Parameters.Roughness); Near(.1f, material.Parameters.Emission.X); Near(.3f, material.Parameters.Emission.Z);
            Equal(NyaForge.Authoring.Graph.MaterialAlphaMode.Blend, material.Parameters.AlphaMode); Near(.3f, material.Parameters.AlphaCutoff);
            var defaults = JObject.Parse(ReadJsonChunk(BuildGlb()));
            defaults["materials"] = new JArray(new JObject { ["pbrMetallicRoughness"] = new JObject() });
            ((JObject)((JArray)((JObject)((JArray)defaults["meshes"]!)[0]!) ["primitives"]!)[0]!) ["material"] = 0;
            var defaultMaterial = GlbImporter.Read(ReplaceJsonChunk(BuildGlb(), defaults.ToString(Newtonsoft.Json.Formatting.None))).Materials[0].Parameters;
            Near(1f, defaultMaterial.Metallic); Near(1f, defaultMaterial.Roughness);
            var missingMaterials = JObject.Parse(ReadJsonChunk(BuildGlb()));
            ((JObject)((JArray)((JObject)((JArray)missingMaterials["meshes"]!)[0]!) ["primitives"]!)[0]!) ["material"] = 0;
            Expect("INVALID_IMPORT", () => GlbImporter.Read(ReplaceJsonChunk(BuildGlb(), missingMaterials.ToString(Newtonsoft.Json.Formatting.None))));
        });
        Test("GLB importer exposes an embedded base color image without fetching external resources", () =>
        {
            var source = BuildGlb(); var root = JObject.Parse(ReadJsonChunk(source)); var bin = ReadBinChunk(source);
            var image = PaintPng.Encode(new PaintImage(2, 1, new Rgba32(220, 30, 60, 255))); int offset = bin.Length;
            var combinedBin = bin.Concat(image).ToArray();
            ((JObject)((JArray)root["buffers"]!)[0]!) ["byteLength"] = combinedBin.Length;
            ((JArray)root["bufferViews"]!).Add(new JObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = image.Length });
            root["images"] = new JArray(new JObject { ["bufferView"] = 3, ["mimeType"] = "image/png", ["name"] = "red" });
            root["textures"] = new JArray(new JObject { ["source"] = 0 });
            root["materials"] = new JArray(new JObject { ["pbrMetallicRoughness"] = new JObject { ["baseColorTexture"] = new JObject { ["index"] = 0 } } });
            var primitive = (JObject)((JArray)((JObject)((JArray)root["meshes"]!)[0]!) ["primitives"]!)[0]!; primitive["material"] = 0;
            var imported = GlbImporter.Read(BuildGlbContainer(Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None)), combinedBin));
            Equal(1, imported.Materials.Count); var material = imported.Materials[0]; True(material.HasTextureReferences); True(material.HasEmbeddedBaseColorImage); Equal(0, material.BaseColorImageIndex); Equal("image/png", material.BaseColorImageMimeType); True(image.SequenceEqual(material.CopyBaseColorImageBytes()));
        });
        Test("GLB material image cache shares immutable payload across mesh resources", () =>
        {
            var source = BuildGlb(); var root = JObject.Parse(ReadJsonChunk(source)); var bin = ReadBinChunk(source);
            var image = PaintPng.Encode(new PaintImage(2, 1, new Rgba32(40, 120, 220, 255))); int offset = bin.Length;
            var combinedBin = bin.Concat(image).ToArray();
            ((JObject)((JArray)root["buffers"]!)[0]!)!["byteLength"] = combinedBin.Length;
            ((JArray)root["bufferViews"]!).Add(new JObject { ["buffer"] = 0, ["byteOffset"] = offset, ["byteLength"] = image.Length });
            root["images"] = new JArray(new JObject { ["bufferView"] = 3, ["mimeType"] = "image/png" });
            root["textures"] = new JArray(new JObject { ["source"] = 0 });
            root["materials"] = new JArray(new JObject { ["pbrMetallicRoughness"] = new JObject { ["baseColorTexture"] = new JObject { ["index"] = 0 } } });
            var meshes = (JArray)root["meshes"]!; var second = (JObject)meshes[0]!.DeepClone();
            ((JObject)((JArray)second["primitives"]!)[0]!)!["material"] = 0; meshes.Add(second);
            ((JObject)((JArray)((JObject)meshes[0]!)!["primitives"]!)[0]!)!["material"] = 0;
            root["nodes"] = new JArray(new JObject { ["mesh"] = 0 }, new JObject { ["mesh"] = 1 });
            var bytes = BuildGlbContainer(Encoding.UTF8.GetBytes(root.ToString(Newtonsoft.Json.Formatting.None)), combinedBin);
            var document = GlbDocumentReader.Read(bytes); var cache = new GlbImportImageCache();
            var first = GlbImporter.ReadDocument(document, 0, null, null, cache);
            var secondImported = GlbImporter.ReadDocument(document, 1, null, null, cache);
            True(first.Materials.Count == 1 && secondImported.Materials.Count == 1);
            True(ReferenceEquals(first.Materials[0].BorrowBaseColorImageBytes(), secondImported.Materials[0].BorrowBaseColorImageBytes()));
            True(image.SequenceEqual(secondImported.Materials[0].CopyBaseColorImageBytes()));
        });
        Test("GLB importer resolves a safe local external base color image", () =>
        {
            string directory = Dir("external-image"); string imagePath = Path.Combine(directory, "textures", "red.png"); Directory.CreateDirectory(Path.GetDirectoryName(imagePath));
            var image = PaintPng.Encode(new PaintImage(2, 1, new Rgba32(220, 30, 60, 255))); File.WriteAllBytes(imagePath, image);
            var root = JObject.Parse(ReadJsonChunk(BuildGlb()));
            root["images"] = new JArray(new JObject { ["uri"] = "textures/red.png", ["mimeType"] = "image/png" });
            root["textures"] = new JArray(new JObject { ["source"] = 0 });
            root["materials"] = new JArray(new JObject { ["pbrMetallicRoughness"] = new JObject { ["baseColorTexture"] = new JObject { ["index"] = 0 } } });
            ((JObject)((JArray)((JObject)((JArray)root["meshes"]!)[0]!) ["primitives"]!)[0]!) ["material"] = 0;
            var imported = GlbImporter.ReadFromDirectory(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)), 0, directory);
            var material = imported.Materials.Single(); True(material.HasEmbeddedBaseColorImage); Equal("image/png", material.BaseColorImageMimeType); True(image.SequenceEqual(material.CopyBaseColorImageBytes()));
            root["images"]![0]!["uri"] = "textures/red%2Epng";
            var encoded = GlbImporter.ReadFromDirectory(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)), 0, directory);
            True(encoded.Materials.Single().CopyBaseColorImageBytes().SequenceEqual(image));
            Expect("EXTERNAL_RESOURCE_UNAVAILABLE", () => GlbImporter.Read(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None))));
            root["images"]![0]!["uri"] = "../outside.png";
            Expect("UNSUPPORTED_FORMAT", () => GlbImporter.ReadFromDirectory(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)), 0, directory));
            root["images"]![0]!["uri"] = "%2E%2E/outside.png";
            Expect("UNSUPPORTED_FORMAT", () => GlbImporter.ReadFromDirectory(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)), 0, directory));
            File.WriteAllBytes(Path.Combine(directory, "textures", "red.webp"), image); root["images"]![0]!["uri"] = "textures/red.webp"; root["images"]![0]!["mimeType"] = "image/webp";
            Expect("UNSUPPORTED_FORMAT", () => GlbImporter.ReadFromDirectory(ReplaceJsonChunk(BuildGlb(), root.ToString(Newtonsoft.Json.Formatting.None)), 0, directory));
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

    static byte[] ReadBinChunk(byte[] bytes)
    {
        int jsonLength = BitConverter.ToInt32(bytes, 12); int binHeader = 20 + jsonLength; int binLength = BitConverter.ToInt32(bytes, binHeader);
        return bytes.Skip(binHeader + 8).Take(binLength).ToArray();
    }

    static byte[] ReplaceJsonChunk(byte[] bytes, string json)
    {
        var bin = bytes.Skip(20 + BitConverter.ToInt32(bytes, 12) + 8).ToArray(); return BuildGlbContainer(Encoding.UTF8.GetBytes(json), bin);
    }
}
