using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSkinDeformerTests()
    {
        Test("skin deformation applies rest-relative rotation and preserves mesh attributes", () =>
        {
            string root = GraphId(), child = GraphId();
            var skeleton = new SkeletonDefinition(new[]
            {
                new BoneDefinition(root, "Root", "", new Vec3(0, 0, 0), new Vec3(0, .1f, 0)),
                new BoneDefinition(child, "Child", root, new Vec3(.1f, .1f, 0), new Vec3(.2f, .1f, 0))
            });
            var mesh = new MeshData(new[] { new Vec3(.2f, .1f, 0), new Vec3(0, 0, 0), new Vec3(0, .1f, 0) },
                new[] { new Vec3(0, 0, 1), new Vec3(0, 0, 1), new Vec3(0, 0, 1) },
                new[] { new Vec4(1, 0, 0, 1), new Vec4(1, 0, 0, 1), new Vec4(1, 0, 0, 1) },
                new[] { new Vec2(0, 0), new Vec2(1, 0), new Vec2(0, 1) }, new[] { new[] { 0, 1, 2 } });
            var raw = new[] { new SkinBinding.VertexWeightInput(0, child, 1), new SkinBinding.VertexWeightInput(1, root, 1), new SkinBinding.VertexWeightInput(2, root, 1) };
            var binding = SkinBinding.Create(mesh, skeleton, raw);
            var posed = SkinDeformer.Apply(mesh, skeleton, binding, new[] { new BonePose(root, PoseTransform.FromTranslation(new Vec3())), new BonePose(child, PoseTransform.RotationZ(90, new Vec3(.1f, .1f, 0))) });
            Near(.1f, posed.Positions[0].X); Near(.2f, posed.Positions[0].Y); Near(1f, posed.Normals[0].Z); Near(1f, posed.Tangents[0].W); Equal(mesh.Uv0[0].X, posed.Uv0[0].X);
        });

        Test("identity rest pose reproduces positions and deformer rejects stale inputs", () =>
        {
            string root = GraphId(); var skeleton = new SkeletonDefinition(new[] { new BoneDefinition(root, "Root", "", new Vec3(), new Vec3(0, .1f, 0)) }); var mesh = AuthoringFixtures.Panel(1);
            var binding = SkinBinding.Create(mesh, skeleton, Enumerable.Range(0, mesh.VertexCount).Select(i => new SkinBinding.VertexWeightInput(i, root, 1)));
            var rest = SkinDeformer.Apply(mesh, skeleton, binding, new[] { new BonePose(root, PoseTransform.FromTranslation(new Vec3())) });
            Equal(mesh.ContentHash, rest.ContentHash);
            var changed = new MeshData(mesh.Positions.ToArray(), mesh.Normals.ToArray(), mesh.Tangents.ToArray(), mesh.Uv0.ToArray(), new[] { new[] { 0, 3, 2, 0, 2, 1 }, new[] { 4, 6, 5, 4, 7, 6 } });
            Expect("SKIN_TOPOLOGY_CHANGED", () => SkinDeformer.Apply(changed, skeleton, binding, new[] { new BonePose(root, PoseTransform.Identity) }));
            Expect("POSE_BONE_MISSING", () => SkinDeformer.Apply(mesh, skeleton, binding, Array.Empty<BonePose>()));
        });
    }
}
