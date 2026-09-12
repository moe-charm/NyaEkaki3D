using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static void RunSourceSkinCodecTests()
    {
        Test("Source skin codec preserves complete frames binds slots and original child order", () =>
        {
            var input = GlbSourceSkinReader.Read(SourceSkinGlb());
            var nodes = new SourceNodeTransforms(input.Nodes.SourceHash, input.Nodes.Hierarchy.Parents,
                input.Nodes.Local, new System.Collections.Generic.IReadOnlyList<int>[] { new[] { 2,1 }, Array.Empty<int>(), Array.Empty<int>() });
            var source = new SourceSkin(nodes, 9, input.Joints, input.InverseBindMatrices,0);
            byte[] bytes = SourceSkinCodec.Write(source);
            var restored = SourceSkinCodec.Read(bytes);
            Equal(9,restored.SkinIndex); Equal(2,restored.Nodes.Hierarchy.Children[0][0]); Equal(1,restored.Joints[0]);
            Equal(source.Nodes.SourceHash,restored.Nodes.SourceHash); True(restored.HasExplicitInverseBindMatrices);
            Equal(3,restored.InverseBindMatrices.Count);
            for(int i=0;i<nodes.Local.Count;i++)
            {
                True(nodes.Local[i].ToColumnMajor().SequenceEqual(restored.Nodes.Local[i].ToColumnMajor()));
                True(nodes.World[i].ToColumnMajor().SequenceEqual(restored.Nodes.World[i].ToColumnMajor()));
            }
            True(bytes.SequenceEqual(SourceSkinCodec.Write(restored)));
            SpringPointNear(new Vec3(1,2,3),restored.JointMatrix(0,restored.Nodes.World[1]).TransformPoint(new Vec3(1,2,3)));
        });
        Test("Source skin codec distinguishes omitted and explicit identity matrices", () =>
        {
            var nodes = SkinTestNodes();
            var omitted = SourceSkinCodec.Read(SourceSkinCodec.Write(new SourceSkin(nodes,0,new[] {1},null)));
            var explicitBind = SourceSkinCodec.Read(SourceSkinCodec.Write(new SourceSkin(nodes,0,new[] {1},new[] {SourceAffine.Identity})));
            True(!omitted.HasExplicitInverseBindMatrices); True(explicitBind.HasExplicitInverseBindMatrices);
            True(omitted.InverseBindMatrices[0].ToColumnMajor().SequenceEqual(explicitBind.InverseBindMatrices[0].ToColumnMajor()));
        });
        Test("Source skin codec rejects truncation trailing bytes invalid counts and version", () =>
        {
            var bytes = SourceSkinCodec.Write(GlbSourceSkinReader.Read(SourceSkinGlb()));
            foreach(int length in new[] {1,8,70,80,200,bytes.Length-1})
                Expect("INVALID_IMPORT",()=>SourceSkinCodec.Read(bytes.Take(length).ToArray()));
            Expect("INVALID_IMPORT",()=>SourceSkinCodec.Read(bytes.Concat(new byte[] {0}).ToArray()));
            var wrongVersion=(byte[])bytes.Clone(); wrongVersion[4]=2;
            Expect("UNSUPPORTED_FORMAT",()=>SourceSkinCodec.Read(wrongVersion));
            var hugeCount=(byte[])bytes.Clone(); Array.Copy(BitConverter.GetBytes(int.MaxValue),0,hugeCount,72,4);
            Expect("INVALID_IMPORT",()=>SourceSkinCodec.Read(hugeCount));
            var invalidBasis=(byte[])bytes.Clone(); Array.Clear(invalidBasis,80,128);
            Expect("INVALID_AFFINE",()=>SourceSkinCodec.Read(invalidBasis));
        });
        Test("Source skin package codec preserves weights and rejects envelope corruption", () =>
        {
            var candidate=GlbSourceSkinImporter.Read(BuildSkinnedGlb());
            var packageBytes=SourceSkinPackageCodec.Write(new SourceSkinPackage(candidate.Skin,candidate.Binding));
            var restored=SourceSkinPackageCodec.Read(packageBytes);
            True(packageBytes.SequenceEqual(SourceSkinPackageCodec.Write(new SourceSkinPackage(restored.Skin,restored.Binding))));
            Equal(candidate.Binding.VertexCount,restored.Binding.VertexCount); Equal(candidate.Binding.Weights.Count,restored.Binding.Weights.Count);
            foreach(int length in new[] {1,8,15,packageBytes.Length-1}) Expect("INVALID_IMPORT",()=>SourceSkinPackageCodec.Read(packageBytes.Take(length).ToArray()));
            Expect("INVALID_IMPORT",()=>SourceSkinPackageCodec.Read(packageBytes.Concat(new byte[]{0}).ToArray()));
            var wrong=(byte[])packageBytes.Clone(); wrong[4]=2; Expect("UNSUPPORTED_FORMAT",()=>SourceSkinPackageCodec.Read(wrong));
            var section=(byte[])packageBytes.Clone(); Array.Copy(BitConverter.GetBytes(int.MaxValue),0,section,8,4); Expect("INVALID_IMPORT",()=>SourceSkinPackageCodec.Read(section));
        });
    }
}
