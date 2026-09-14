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

        Test("skin binding normalizes and orders up to 32 influences", () =>
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
            string[] extra = Enumerable.Range(0, 32).Select(_ => GraphId()).ToArray();
            var manyBones = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) }.Concat(extra.Select((id, index) => new BoneDefinition(id, "B" + index, root, new Vec3(0, .1f, 0), new Vec3(.1f + index * .01f, .1f, 0)))));
            var tooMany = Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, root, 1f)).ToList();
            tooMany.AddRange(extra.Select(id => new SkinBinding.VertexWeightInput(0, id, .1f))); Expect("INFLUENCE_LIMIT", () => SkinBinding.Create(mesh, manyBones, tooMany));
        });

        Test("clothing skeleton subset keeps weighted bones and their ancestors", () =>
        {
            string root = GraphId(), mid = GraphId(), leaf = GraphId(), unused = GraphId();
            var skeleton = new SkeletonDefinition(new[] {
                new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)),
                new BoneDefinition(mid, "Mid", root, new Vec3(0, .1f, 0), new Vec3(0, .2f, 0)),
                new BoneDefinition(leaf, "Leaf", mid, new Vec3(0, .2f, 0), new Vec3(0, .3f, 0)),
                new BoneDefinition(unused, "Unused", "", new Vec3(1, 0, 0), new Vec3(1, .1f, 0))
            });
            var mesh = AuthoringFixtures.Panel(1);
            var binding = SkinBinding.Create(mesh, skeleton,
                Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, leaf, 1f)));
            var subset = SkeletonBindingSubset.ForBinding(skeleton, binding);
            Equal(3, subset.Bones.Count); True(subset.ById.ContainsKey(root)); True(subset.ById.ContainsKey(mid)); True(subset.ById.ContainsKey(leaf));
            var rebound = binding.RebindToSkeleton(mesh, subset);
            Equal(subset.ContentHash, rebound.SkeletonHash); Equal(leaf, rebound.Weights[0][0].BoneId);
        });

        Test("rig and morph capacity retain 257 bones, 18 influences and 262 targets", () =>
        {
            var boneIds = Enumerable.Range(0, 257).Select(_ => GraphId()).ToArray();
            var bones = boneIds.Select((id, i) => new BoneDefinition(id, "B" + i, i == 0 ? "" : boneIds[i - 1], new Vec3(0, i * .01f, 0), new Vec3(0, i * .01f + .01f, 0))).ToArray();
            var skeleton = new SkeletonDefinition(bones); Equal(257, skeleton.Bones.Count);
            var mesh = AuthoringFixtures.Panel(1);
            var weights = Enumerable.Range(0, mesh.VertexCount).SelectMany(v => Enumerable.Range(0, 18).Select(i => new SkinBinding.VertexWeightInput(v, boneIds[i], 1f))).ToArray();
            var binding = SkinBinding.Create(mesh, skeleton, weights); Equal(18, binding.Weights[0].Count); Near(1f, binding.Weights[0].Sum(item => item.Weight));
            var targets = Enumerable.Range(0, 262).Select(i => MorphTarget.Create(mesh, GraphId(), "Target" + i, new[] { new MorphDelta(i % mesh.VertexCount, new Vec3(.001f, 0, 0)) })).ToArray();
            var morphs = MorphSet.Create(mesh, targets); Equal(262, morphs.Targets.Count); var restored = MorphCodec.Read(MorphCodec.Write(morphs), mesh); Equal(262, restored.Targets.Count);
        });
    }
}
