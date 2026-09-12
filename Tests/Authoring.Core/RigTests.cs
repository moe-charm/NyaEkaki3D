using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunRigTests()
    {
        Test("skeleton validates stable rest hierarchy and deterministic identity", () =>
        {
            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[]
            {
                new BoneDefinition(root, "Root", "", new Vec3(0, 0, 0), new Vec3(0, .1f, 0)),
                new BoneDefinition(child, "Chest", root, new Vec3(0, .1f, 0), new Vec3(0, .2f, 0))
            });
            True(skeleton.ById.ContainsKey(child)); Equal(root, skeleton.ById[child].ParentBoneId); Equal(2, skeleton.Bones.Count);
            var reordered = new SkeletonDefinition(skeleton.Bones.Reverse()); Equal(skeleton.ContentHash, reordered.ContentHash);
        });

        Test("skeleton rejects missing parents, duplicate IDs and cycles", () =>
        {
            string root = GraphId(), missing = GraphId();
            Expect("BONE_PARENT_MISSING", () => new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", missing, new Vec3(), new Vec3(0, .1f, 0)) }));
            Expect("DUPLICATE_BONE", () => new SkeletonDefinition(new[] { new BoneDefinition(root, "A", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(root, "B", "", new Vec3(), new Vec3(0, .2f, 0)) }));
            string a = GraphId(), b = GraphId();
            Expect("BONE_CYCLE", () => new SkeletonDefinition(new[] { new BoneDefinition(a, "A", b, new Vec3(), new Vec3(.1f, 0, 0)), new BoneDefinition(b, "B", a, new Vec3(.1f, 0, 0), new Vec3(.2f, 0, 0)) }));
        });

        Test("skin binding normalizes and orders up to four influences", () =>
        {
            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(child, "Child", root, new Vec3(0, .1f, 0), new Vec3(0, .2f, 0)) });
            var mesh = AuthoringFixtures.Panel(1);
            var raw = Enumerable.Range(0, mesh.VertexCount).SelectMany(i => new[] { new SkinBinding.VertexWeightInput(i, root, .25f), new SkinBinding.VertexWeightInput(i, child, .75f) });
            var binding = SkinBinding.Create(mesh, skeleton, raw);
            Equal(mesh.TopologyHash, binding.MeshTopologyHash); Equal(skeleton.ContentHash, binding.SkeletonHash);
            foreach (var weights in binding.Weights.Values) { Near(.75f, weights[0].Weight); Near(.25f, weights[1].Weight); Near(1f, weights.Sum(w => w.Weight)); }
        });

        Test("skin binding rejects duplicate, unknown, unweighted and excessive influences", () =>
        {
            string root = GraphId(); var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) }); var mesh = AuthoringFixtures.Panel(1);
            var complete = Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, root, 1f)).ToList();
            complete.Add(new SkinBinding.VertexWeightInput(0, root, .1f)); Expect("DUPLICATE_WEIGHT", () => SkinBinding.Create(mesh, skeleton, complete));
            complete = Enumerable.Range(1, mesh.VertexCount - 1).Select(i => new SkinBinding.VertexWeightInput(i, root, 1f)).ToList(); Expect("UNWEIGHTED_VERTEX", () => SkinBinding.Create(mesh, skeleton, complete));
            Expect("BONE_NOT_FOUND", () => SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, GraphId(), 1f))));
            string[] extra = Enumerable.Range(0, 4).Select(_ => GraphId()).ToArray();
            var manyBones = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) }.Concat(extra.Select((id, index) => new BoneDefinition(id, "B" + index, root, new Vec3(0, .1f, 0), new Vec3(.1f + index * .01f, .1f, 0)))));
            var tooMany = Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, root, 1f)).ToList();
            tooMany.AddRange(extra.Select(id => new SkinBinding.VertexWeightInput(0, id, .1f))); Expect("INFLUENCE_LIMIT", () => SkinBinding.Create(mesh, manyBones, tooMany));
        });
    }
}
