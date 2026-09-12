using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunMorphTests()
    {
        Test("sparse morph deforms rest mesh and roundtrips deterministically", () =>
        {
            var mesh = AuthoringFixtures.Panel(1); string id = GraphId();
            var target = MorphTarget.Create(mesh, id, "Smile", new[] { new MorphDelta(0, new Vec3(.2f, .1f, 0)), new MorphDelta(4, new Vec3(0, -.05f, .03f)) });
            var morphs = MorphSet.Create(mesh, new[] { target });
            var result = MorphDeformer.Apply(mesh, morphs, new Dictionary<string, float> { [id] = .5f });
            Near(mesh.Positions[0].X + .1f, result.Positions[0].X); Near(mesh.Positions[0].Y + .05f, result.Positions[0].Y);
            Near(mesh.Positions[4].Y - .025f, result.Positions[4].Y); Near(mesh.Positions[4].Z + .015f, result.Positions[4].Z);
            Equal(mesh.Normals[0], result.Normals[0]); Equal(mesh.Uv0[4], result.Uv0[4]);
            var bytes = MorphCodec.Write(morphs); var restored = MorphCodec.Read(bytes, mesh);
            True(bytes.SequenceEqual(MorphCodec.Write(restored))); Equal(morphs.ContentHash, restored.ContentHash); Equal(target.ContentHash, restored.ById[id].ContentHash);
        });

        Test("morph validation rejects unknown weights, invalid ranges and changed topology", () =>
        {
            var mesh = AuthoringFixtures.Panel(1); string id = GraphId();
            var morphs = MorphSet.Create(mesh, new[] { MorphTarget.Create(mesh, id, "Blink", new[] { new MorphDelta(1, new Vec3(0, 0, .1f)) }) });
            Expect("MORPH_NOT_FOUND", () => MorphDeformer.Apply(mesh, morphs, new Dictionary<string, float> { [GraphId()] = .5f }));
            Expect("INVALID_MORPH_WEIGHT", () => MorphDeformer.Apply(mesh, morphs, new Dictionary<string, float> { [id] = 1.1f }));
            var changed = new MeshData(mesh.Positions.ToArray(), mesh.Normals.ToArray(), mesh.Tangents.ToArray(), mesh.Uv0.ToArray(), new[] { new[] { 0, 3, 1, 0, 2, 3 }, new[] { 4, 5, 7, 4, 6, 7 } });
            Expect("MORPH_TOPOLOGY_CHANGED", () => MorphDeformer.Apply(changed, morphs, new Dictionary<string, float> { [id] = 1 }));
            Expect("INVALID_BLOB", () => MorphCodec.Read(MorphCodec.Write(morphs).Concat(new byte[] { 1 }).ToArray(), mesh));
        });
    }
}
