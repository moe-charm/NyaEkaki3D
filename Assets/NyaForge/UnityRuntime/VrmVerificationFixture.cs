using System;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    // Self-authored bounded test data; no third-party avatar bytes.
    internal static class VrmVerificationFixture
    {
    internal static byte[] Create(bool legacy, bool invalidSkin = false, bool playback = false)
    {
        using (var bin = new MemoryStream()) using (var b = new BinaryWriter(bin))
        {
            foreach (var p in new[] { new Vec3(0, 0, 0), new Vec3(.1f, 0, 0), new Vec3(0, .1f, 0) }) { b.Write(p.X); b.Write(p.Y); b.Write(p.Z); }
            foreach (ushort i in new ushort[] { 0, 1, 2 }) b.Write(i);
            while (bin.Length % 4 != 0) b.Write((byte)0);
            foreach (byte[] row in new[] { new byte[] { 0, 0, 0, 0 }, new byte[] { 0, 1, 0, 0 }, new byte[] { 1, 0, 0, 0 } }) b.Write(row);
            foreach (var row in new[] { new[] { 1f, 0f, 0f, 0f }, new[] { .5f, .5f, 0f, 0f }, new[] { 0f, 1f, 0f, 0f } }) foreach (float value in row) b.Write(value);
            WriteIdentity(b); WriteTranslationInverse(b, .1f);
            var json = new JObject
            {
                ["asset"] = new JObject { ["version"] = "2.0" }, ["buffers"] = new JArray(new JObject { ["byteLength"] = (int)bin.Length }),
                ["bufferViews"] = new JArray(new JObject { ["buffer"] = 0, ["byteOffset"] = 0, ["byteLength"] = 36 }, new JObject { ["buffer"] = 0, ["byteOffset"] = 36, ["byteLength"] = 6 }, new JObject { ["buffer"] = 0, ["byteOffset"] = 44, ["byteLength"] = 12 }, new JObject { ["buffer"] = 0, ["byteOffset"] = 56, ["byteLength"] = 48 }, new JObject { ["buffer"] = 0, ["byteOffset"] = 104, ["byteLength"] = 128 }),
                ["accessors"] = new JArray(new JObject { ["bufferView"] = 0, ["componentType"] = 5126, ["count"] = 3, ["type"] = "VEC3" }, new JObject { ["bufferView"] = 1, ["componentType"] = 5123, ["count"] = 3, ["type"] = "SCALAR" }, new JObject { ["bufferView"] = 2, ["componentType"] = 5121, ["count"] = 3, ["type"] = "VEC4" }, new JObject { ["bufferView"] = 3, ["componentType"] = 5126, ["count"] = 3, ["type"] = "VEC4" }, new JObject { ["bufferView"] = 4, ["componentType"] = 5126, ["count"] = 2, ["type"] = "MAT4" }),
                ["meshes"] = new JArray(new JObject { ["primitives"] = new JArray(new JObject { ["attributes"] = new JObject { ["POSITION"] = 0, ["JOINTS_0"] = 2, ["WEIGHTS_0"] = 3 }, ["indices"] = 1 }) }),
                ["nodes"] = new JArray(new JObject { ["name"] = "Root", ["children"] = new JArray(1) }, new JObject { ["name"] = "Child", ["translation"] = new JArray(0, .1, 0) }),
                ["skins"] = new JArray(new JObject { ["joints"] = new JArray(0, 1), ["inverseBindMatrices"] = 4 })
            };
            AddVrm(json, legacy);
            if (playback && !legacy) json["extensions"]["VRMC_springBone"] = new JObject {
                ["specVersion"] = "1.0", ["springs"] = new JArray(new JObject { ["center"] = 0, ["joints"] = new JArray(
                    new JObject { ["node"] = 0, ["stiffness"] = 1, ["gravityPower"] = .2, ["gravityDir"] = new JArray(1, 0, 0) }, new JObject { ["node"] = 1 }) }) };

            if (playback && legacy) json["extensions"]["VRM"]["secondaryAnimation"] = new JObject {
                ["colliderGroups"] = new JArray(), ["boneGroups"] = new JArray(new JObject {
                    ["bones"] = new JArray(0), ["center"] = 0, ["stiffiness"] = 1, ["gravityPower"] = .2,
                    ["gravityDir"] = new JObject { ["x"] = 1, ["y"] = 0, ["z"] = 0 } }) };

            if (invalidSkin) json["nodes"][1]["scale"] = new JArray(2, 1, 1);
            return BuildGlbContainer(Encoding.UTF8.GetBytes(json.ToString(Newtonsoft.Json.Formatting.None)), bin.ToArray());
        }
    }

    static void WriteIdentity(BinaryWriter writer) { for (int i = 0; i < 16; i++) writer.Write(i == 0 || i == 5 || i == 10 || i == 15 ? 1f : 0f); }
    static void WriteTranslationInverse(BinaryWriter writer, float y) { for (int i = 0; i < 16; i++) writer.Write(i == 0 || i == 5 || i == 10 || i == 15 ? 1f : i == 13 ? -y : 0f); }

        static void AddVrm(JObject root, bool legacy)
        {
            // Source origin intentionally differs from the inverse-bind head (0.1).
            root["nodes"][1]["translation"] = new JArray(0, .3, 0);
            ((JArray)root["nodes"]).Add(new JObject { ["mesh"] = 0, ["skin"] = 0 });
            root["meshes"][0]["primitives"][0]["targets"] = new JArray(new JObject { ["POSITION"] = 0 });
            var vrm = new JObject { ["specVersion"] = legacy ? "0.0" : "1.0" };
            root["extensions"] = new JObject { [legacy ? "VRM" : "VRMC_vrm"] = vrm };
            if (legacy)
            {
                vrm["meta"] = new JObject { ["title"] = "Legacy fixture", ["author"] = "Nya, Charm" };
                vrm["humanoid"] = new JObject { ["humanBones"] = new JArray(new JObject { ["bone"] = "hips", ["node"] = 0 }) };
                vrm["blendShapeMaster"] = new JObject { ["blendShapeGroups"] = new JArray(new JObject { ["name"] = "happy", ["presetName"] = "happy", ["binds"] = new JArray(new JObject { ["mesh"] = 0, ["index"] = 0, ["weight"] = 50 }) }) };
                vrm["secondaryAnimation"] = new JObject {
                    ["colliderGroups"] = new JArray(new JObject { ["node"] = 0, ["colliders"] = new JArray(new JObject { ["radius"] = .1, ["offset"] = new JObject { ["x"] = 0, ["y"] = 0, ["z"] = 0 } }, new JObject { ["radius"] = .2, ["offset"] = new JObject { ["x"] = 0, ["y"] = .1, ["z"] = 0 } }) }),
                    ["boneGroups"] = new JArray(new JObject { ["comment"] = "tail", ["bones"] = new JArray(1), ["colliderGroups"] = new JArray(0), ["stiffiness"] = 1, ["dragForce"] = .5, ["gravityDir"] = new JObject { ["x"] = 1, ["y"] = 0, ["z"] = -1 } }) };
            }
            else
            {
                vrm["meta"] = new JObject { ["name"] = "Modern fixture", ["authors"] = new JArray("Nya", "Moe, Charm"), ["licenseUrl"] = "https://example.invalid/license" };
                vrm["humanoid"] = new JObject { ["humanBones"] = new JObject { ["hips"] = new JObject { ["node"] = 0 } } };
                vrm["expressions"] = new JObject { ["preset"] = new JObject { ["happy"] = new JObject { ["morphTargetBinds"] = new JArray(new JObject { ["node"] = 2, ["index"] = 0, ["weight"] = .5 }) } } };
                root["extensions"]["VRMC_springBone"] = new JObject { ["specVersion"] = "1.0",
                    ["colliders"] = new JArray(new JObject { ["node"] = 0, ["shape"] = new JObject { ["sphere"] = new JObject { ["radius"] = .1, ["offset"] = new JArray(0, 0, 0) } } }, new JObject { ["node"] = 0, ["shape"] = new JObject { ["sphere"] = new JObject { ["radius"] = .2, ["offset"] = new JArray(0, .1, 0) } } }, new JObject { ["node"] = 1, ["shape"] = new JObject { ["capsule"] = new JObject { ["radius"] = .1, ["offset"] = new JArray(0, 0, 0), ["tail"] = new JArray(0, .1, 0) } } }),
                    ["colliderGroups"] = new JArray(new JObject { ["colliders"] = new JArray(0, 1, 2) }),
                    ["springs"] = new JArray(new JObject { ["name"] = "tail", ["joints"] = new JArray(new JObject { ["node"] = 0, ["gravityDir"] = new JArray(2, -3, 4) }, new JObject { ["node"] = 1, ["stiffness"] = 0, ["dragForce"] = 0 }), ["colliderGroups"] = new JArray(0) }) };
            }
        }

        static byte[] BuildGlbContainer(byte[] json, byte[] bin)
        {
            int jsonSize = (json.Length + 3) & ~3;
            int binSize = (bin.Length + 3) & ~3;
            using (var stream = new MemoryStream()) using (var writer = new BinaryWriter(stream))
            {
                writer.Write(0x46546C67); writer.Write(2); writer.Write(12 + 8 + jsonSize + 8 + binSize);
                writer.Write(jsonSize); writer.Write(0x4E4F534A); writer.Write(json);
                for (int i = json.Length; i < jsonSize; i++) writer.Write((byte)32);
                writer.Write(binSize); writer.Write(0x004E4942); writer.Write(bin);
                for (int i = bin.Length; i < binSize; i++) writer.Write((byte)0);
                return stream.ToArray();
            }
        }
    }
}
