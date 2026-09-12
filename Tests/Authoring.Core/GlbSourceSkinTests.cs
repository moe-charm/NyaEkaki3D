using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static byte[] SourceSkinGlb(Action<JObject> edit = null)
    {
        var nodes = SkinTestNodes();
        using (var bin = new MemoryStream())
        using (var writer = new BinaryWriter(bin))
        {
            writer.Write(0); writer.Write(0); // independent view and accessor offsets
            foreach (var matrix in new[] { nodes.World[1].Inverse(), nodes.World[0].Inverse(), SourceAffine.Identity })
                foreach (double component in matrix.ToColumnMajor()) writer.Write((float)component);
            var root = new JObject {
                ["asset"] = new JObject { ["version"] = "2.0" },
                ["nodes"] = new JArray(nodes.Local.Select((frame,i) => {
                    var node = new JObject { ["matrix"] = new JArray(frame.ToColumnMajor()) };
                    if(i == 0) node["children"] = new JArray(1,2); return node;
                })),
                ["skins"] = new JArray(new JObject { ["joints"] = new JArray(1,0), ["skeleton"] = 0, ["inverseBindMatrices"] = 0 }),
                ["buffers"] = new JArray(new JObject { ["byteLength"] = bin.Length }),
                ["bufferViews"] = new JArray(new JObject { ["buffer"] = 0, ["byteOffset"] = 4, ["byteLength"] = bin.Length - 4 }),
                ["accessors"] = new JArray(new JObject { ["bufferView"] = 0, ["byteOffset"] = 4, ["componentType"] = 5126, ["count"] = 3, ["type"] = "MAT4" })
            };
            edit?.Invoke(root);
            return BuildGlbContainer(Encoding.UTF8.GetBytes(root.ToString()), bin.ToArray());
        }
    }

    static void RunGlbSourceSkinTests()
    {
        Test("GLB source skin reads general bind matrices with offsets and preserved extras", () =>
        {
            var bytes = SourceSkinGlb(); var skin = GlbSourceSkinReader.Read(bytes);
            Equal(Checks.Hash(bytes), skin.Nodes.SourceHash); Equal(1, skin.Joints[0]);
            Equal(3, skin.InverseBindMatrices.Count); True(skin.HasExplicitInverseBindMatrices);
            SpringPointNear(new Vec3(.2f,.3f,.4f), skin.JointMatrix(0,skin.Nodes.World[1]).TransformPoint(new Vec3(.2f,.3f,.4f)));
            var multiple = SourceSkinGlb(root => ((JArray)root["skins"]).Add(new JObject { ["joints"] = new JArray(2) }));
            var second = GlbSourceSkinReader.Read(multiple,1);
            Equal(1,second.SkinIndex); Equal(2,second.Joints[0]); True(!second.HasExplicitInverseBindMatrices);
        });
        Test("GLB source skin rejects malformed matrix layouts and references", () =>
        {
            Action<JObject>[] edits = {
                root => root["accessors"][0]["count"] = 4,
                root => root["accessors"][0]["count"] = 1,
                root => root["accessors"][0]["byteOffset"] = 2,
                root => root["accessors"][0]["componentType"] = 5123,
                root => root["accessors"][0]["type"] = new JObject(),
                root => root["accessors"][0]["normalized"] = true,
                root => root["bufferViews"][0]["byteLength"] = 200,
                root => root["bufferViews"][0]["buffer"] = 1,
                root => root["bufferViews"][0]["byteStride"] = 60,
                root => root["buffers"][0]["byteLength"] = 100,
                root => root["skins"][0]["inverseBindMatrices"] = JValue.CreateNull(),
                root => root["skins"][0]["joints"] = new JArray(1000000000000L),
                root => root["skins"][0]["joints"] = new JArray(1,1)
            };
            foreach (var edit in edits) Expect("INVALID_IMPORT", () => GlbSourceSkinReader.Read(SourceSkinGlb(edit)));
            Expect("UNSUPPORTED_FORMAT", () => GlbSourceSkinReader.Read(SourceSkinGlb(root => root["accessors"][0]["sparse"] = new JObject())));
            Expect("UNSUPPORTED_FORMAT", () => GlbSourceSkinReader.Read(SourceSkinGlb(root => root["buffers"][0]["uri"] = "external.bin")));
            Expect("INVALID_IMPORT", () => GlbSourceSkinReader.Read(SourceSkinGlb(),3));
        });
    }
}
