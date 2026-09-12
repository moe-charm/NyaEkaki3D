using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunGlbNodeTransformTests()
    {
        Test("GLB node inventory combines matrix and TRS across reordered parents", () =>
        {
            var bytes = BuildMappedVrm(false); var json = JObject.Parse(ReadJsonChunk(bytes));
            double h = Math.Sqrt(.5);
            json["nodes"] = new JArray(
                new JObject { ["translation"] = new JArray(1, 2, 3) },
                new JObject { ["matrix"] = new JArray(new double[] { -1,0,0,0, 0,1,0,0, 0,0,1,0, 0,0,0,1 }) },
                new JObject { ["translation"] = new JArray(10,20,30), ["rotation"] = new JArray(0,0,h,h), ["scale"] = new JArray(2,3,4), ["children"] = new JArray(1,0) });
            bytes = ReplaceJsonChunk(bytes, json.ToString()); var frames = GlbNodeTransformReader.Read(bytes);
            Equal(Checks.Hash(bytes), frames.SourceHash); Equal(2, frames.Hierarchy.Parents[0]);
            Equal(1, frames.Hierarchy.Children[2][0]); Equal(0, frames.Hierarchy.Children[2][1]);
            SpringPointNear(new Vec3(4,22,42), frames.Hierarchy.Origins[0]);
            SpringPointNear(new Vec3(1,2,3), frames.Local[0].TransformPoint(new Vec3()));
            True(frames.World[1].IsMirrored);
            SpringPointNear(new Vec3(0,-2,0), frames.World[1].TransformVector(new Vec3(1,0,0)));
            SpringPointNear(new Vec3(1,2,3), frames.World[0].Inverse().TransformPoint(frames.World[0].TransformPoint(new Vec3(1,2,3))));
            json["nodes"][0]["translation"][0] = 99;
            SpringPointNear(new Vec3(1,2,3), frames.Local[0].TransformPoint(new Vec3()));
        });
        Test("GLB node inventory handles deep trees and translation-profile agreement", () =>
        {
            var nodes = new JArray(Enumerable.Range(0,4096).Select(i => {
                var n = new JObject { ["translation"] = new JArray(0,1,0) };
                if (i < 4095) n["children"] = new JArray(i+1); return n;
            }));
            var frames = GlbNodeTransformReader.Read(nodes, new string('a',64));
            SpringPointNear(new Vec3(0,4096,0),frames.Hierarchy.Origins[4095]);
            var bytes = BuildMappedVrm(false); var source = GlbSkinImporter.Read(bytes);
            frames = GlbNodeTransformReader.Read(bytes);
            foreach (var pair in source.SourceNodeOrigins) SpringPointNear(pair.Value,frames.Hierarchy.Origins[pair.Key]);
        });
        Test("GLB node inventory rejects malformed transforms and ambiguous hierarchy", () =>
        {
            string hash = new string('a',64);
            foreach (var invalid in new[] {
                new JObject { ["translation"] = JValue.CreateNull() },
                new JObject { ["translation"] = new JArray(1,2) },
                new JObject { ["translation"] = new JArray("1",2,3) },
                new JObject { ["translation"] = new JArray(double.PositiveInfinity,2,3) },
                new JObject { ["matrix"] = new JArray(SourceAffine.Identity.ToColumnMajor()), ["rotation"] = new JArray(0,0,0,1) },
                new JObject { ["children"] = "0" },
                new JObject { ["children"] = new JArray(1.5) },
                new JObject { ["children"] = new JArray(1000000000000L) },
                new JObject { ["children"] = new JArray(1,1) }
            }) Expect("INVALID_IMPORT",()=>GlbNodeTransformReader.Read(new JArray(invalid,new JObject()),hash));
            Expect("INVALID_IMPORT",()=>GlbNodeTransformReader.Read(new JArray(new JObject { ["children"] = new JArray(2) },new JObject { ["children"] = new JArray(2) },new JObject()),hash));
            Expect("BONE_CYCLE",()=>GlbNodeTransformReader.Read(new JArray(new JObject { ["children"] = new JArray(1) },new JObject { ["children"] = new JArray(0) }),hash));
            Expect("BUDGET_EXCEEDED",()=>GlbNodeTransformReader.Read(new JArray(Enumerable.Range(0,4097).Select(_=>new JObject())),hash));
            Expect("INVALID_AFFINE",()=>GlbNodeTransformReader.Read(new JArray(new JObject { ["scale"] = new JArray(0,1,1) }),hash));
        });
    }
}
