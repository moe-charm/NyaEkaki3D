using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static SourceNodeTransforms SkinTestNodes()
    {
        float h = (float)Math.Sqrt(.5);
        return new SourceNodeTransforms(new string('a', 64), new[] { -1, 0, 0 }, new[] {
            SourceAffine.FromTrs(new Vec3(3,4,5), new Vec4(0,0,h,h), new Vec3(2,3,4)),
            SourceAffine.FromTrs(new Vec3(1,2,3), new Vec4(0,0,0,1), new Vec3(-1,1,1)),
            SourceAffine.Identity }, null);
    }

    static void RunSourceSkinTests()
    {
        Test("Source skin preserves slot order and general bind cancellation", () =>
        {
            var nodes = SkinTestNodes(); var joints = new[] { 1, 0 };
            var matrices = new[] { nodes.World[1].Inverse(), nodes.World[0].Inverse(), SourceAffine.Identity };
            var skin = new SourceSkin(nodes, 7, joints, matrices, 0);
            joints[0] = 2; matrices[0] = SourceAffine.Identity;
            Equal(1, skin.Joints[0]); Equal(7, skin.SkinIndex); Equal(3, skin.InverseBindMatrices.Count);
            True(skin.HasExplicitInverseBindMatrices);
            var point = new Vec3(.2f,.3f,.4f);
            SpringPointNear(point, skin.JointMatrix(0, nodes.World[1]).TransformPoint(point));
            var movement = SourceAffine.FromTrs(new Vec3(4,5,6), new Vec4(0,0,0,1), new Vec3(1,1,1));
            SpringPointNear(point + new Vec3(4,5,6), skin.JointMatrix(0, movement.Compose(nodes.World[1])).TransformPoint(point));
        });
        Test("Source skin source-omitted bind is identity and not an inferred inverse rest", () =>
        {
            var nodes = SkinTestNodes(); var skin = new SourceSkin(nodes, 0, new[] { 1 }, null);
            True(!skin.HasExplicitInverseBindMatrices);
            var point = new Vec3(1,2,3);
            SpringPointNear(nodes.World[1].TransformPoint(point), skin.JointMatrix(0, nodes.World[1]).TransformPoint(point));
            var many = Enumerable.Range(0, 257).ToArray();
            var large = new SourceNodeTransforms(new string('b',64), Enumerable.Repeat(-1,257).ToArray(),
                Enumerable.Repeat(SourceAffine.Identity,257).ToArray(), null);
            Equal(257, new SourceSkin(large, 0, many, null).Joints.Count);
        });
        Test("Source skin rejects invalid slots roots and incomplete bind arrays", () =>
        {
            var nodes = SkinTestNodes();
            Expect("INVALID_IMPORT", () => new SourceSkin(nodes, -1, new[] { 0 }, null));
            Expect("INVALID_IMPORT", () => new SourceSkin(nodes, 0, new[] { 1,1 }, null));
            Expect("INVALID_IMPORT", () => new SourceSkin(nodes, 0, new[] { 3 }, null));
            Expect("INVALID_IMPORT", () => new SourceSkin(nodes, 0, new[] { 0,1 }, new[] { SourceAffine.Identity }));
            Expect("INVALID_IMPORT", () => new SourceSkin(nodes, 0, new[] { 1 }, new SourceAffine[] { null }));
            Expect("INVALID_IMPORT", () => new SourceSkin(nodes, 0, new[] { 1,2 }, null, 1));
            var skin = new SourceSkin(nodes, 0, new[] { 1 }, null);
            Expect("INVALID_IMPORT", () => skin.JointMatrix(1, SourceAffine.Identity));
        });
    }
}
