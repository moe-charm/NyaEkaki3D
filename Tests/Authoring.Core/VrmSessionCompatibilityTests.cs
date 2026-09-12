using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunVrmSessionCompatibilityTests()
    {
        foreach (bool spring in new[] { false, true })
        {
            Test("VRM session migration and project snapshot preserve authors: " + spring, () =>
            {
                var root = new JObject { ["version"] = 1, ["sourceHash"] = new string('a', 64), ["format"] = "vrm1", ["title"] = "Example", ["author"] = "Nya, Charm" };
                if (spring) { root["springBones"] = new JArray(); root["colliderGroups"] = new JArray(); }
                else root["expressions"] = new JArray();
                Func<byte[], byte[]> roundtrip = bytes => spring ? VrmSpringSessionCodec.Write(VrmSpringSessionCodec.Read(bytes)) : VrmExpressionSessionCodec.Write(VrmExpressionSessionCodec.Read(bytes));
                var migrated = JObject.Parse(Encoding.UTF8.GetString(roundtrip(Encoding.UTF8.GetBytes(root.ToString()))));
                Equal(2, (int)migrated["version"]); Equal(1, ((JArray)migrated["authors"]).Count); Equal("Nya, Charm", (string)migrated["authors"][0]); True(migrated["author"] == null);
                migrated["authors"] = new JArray("Nya, Charm", new string('b', 256));
                var payload = roundtrip(Encoding.UTF8.GetBytes(migrated.ToString()));
                string name = spring ? ProjectAttachments.Springs : ProjectAttachments.Expressions;
                var w = AuthoringWorkspace.CreateEmpty(); w.SetAttachments(new ProjectAttachments(new Dictionary<string, byte[]> { [name] = payload }));
                string directory = Dir("vrm-authors-" + spring); ProjectStore.Save(directory, w, 0);
                var loaded = ProjectStore.Open(directory); True(payload.SequenceEqual(roundtrip(loaded.Attachments.Read(name))));
                migrated["authors"] = new JArray(12); Expect("INVALID_VRM", () => roundtrip(Encoding.UTF8.GetBytes(migrated.ToString())));
            });
        }
        Test("VRM1 author arrays reject absent empty and non-text identities", () =>
        {
            var root = JObject.Parse(ReadJsonChunk(BuildGlb())); root["nodes"] = new JArray(new JObject());
            var meta = new JObject(); root["extensions"] = new JObject { ["VRMC_vrm"] = new JObject { ["specVersion"] = "1.0", ["meta"] = meta, ["humanoid"] = new JObject { ["humanBones"] = new JObject { ["hips"] = new JObject { ["node"] = 0 } } } } };
            foreach (JToken authors in new JToken[] { JValue.CreateNull(), new JValue("Nya"), new JArray(), new JArray(""), new JArray(1) })
            {
                meta["authors"] = authors; Expect("INVALID_VRM", () => VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString())));
            }
            meta.Remove("authors"); Expect("INVALID_VRM", () => VrmMetadataReader.Read(ReplaceJsonChunk(BuildGlb(), root.ToString())));
        });
    }
}
