using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunRigCodecTests()
    {
        Test("rig codec roundtrips skeleton, normalized binding and exact identity", () =>
        {
            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)), new BoneDefinition(child, "Child", root, new Vec3(.1f, .1f, 0), new Vec3(.2f, .1f, 0)) });
            var mesh = AuthoringFixtures.Panel(1);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).SelectMany(i => new[] { new SkinBinding.VertexWeightInput(i, root, .2f), new SkinBinding.VertexWeightInput(i, child, .8f) }));
            var bytes = RigCodec.Write(skeleton, binding); var restored = RigCodec.Read(bytes, mesh);
            Equal(skeleton.ContentHash, restored.Skeleton.ContentHash); Equal(binding.MeshTopologyHash, restored.Binding.MeshTopologyHash); Equal(binding.SkeletonHash, restored.Binding.SkeletonHash);
            for (int i = 0; i < mesh.VertexCount; i++) { Near(.8f, restored.Binding.Weights[i][0].Weight); Near(.2f, restored.Binding.Weights[i][1].Weight); }
            True(bytes.SequenceEqual(RigCodec.Write(restored.Skeleton, restored.Binding)));
        });

        Test("rig codec refuses trailing bytes and another mesh topology", () =>
        {
            string root = GraphId(); var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) }); var mesh = AuthoringFixtures.Panel(1);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, root, 1)));
            var bytes = RigCodec.Write(skeleton, binding).Concat(new byte[] { 1 }).ToArray(); Expect("INVALID_BLOB", () => RigCodec.Read(bytes, mesh));
            var changed = new MeshData(mesh.Positions.ToArray(), mesh.Normals.ToArray(), mesh.Tangents.ToArray(), mesh.Uv0.ToArray(), new[] { new[] { 0, 3, 1, 0, 2, 3 }, new[] { 4, 5, 7, 4, 6, 7 } });
            Expect("SKIN_TOPOLOGY_CHANGED", () => RigCodec.Read(RigCodec.Write(skeleton, binding), changed));
        });
    }
}
