using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;
using NyaForge.Authoring.Rig;

internal static partial class Program
{
    static void RunSourceMeshTransformTests()
    {
        Test("Source mesh affine transforms positions directions and mirrored winding without changing source", () =>
        {
            var source = AuthoringFixtures.Panel(1); string before = source.ContentHash;
            var matrix = new SourceAffine(new double[] {-2,0,0,0, 1,3,0,0, 0,0,4,0, 5,6,7,1});
            var result = SourceMeshTransform.Apply(source,matrix);
            Equal(before,source.ContentHash); Equal(before,result.OriginalMeshHash);
            Equal(source.VertexCount,result.Mesh.VertexCount); Equal(source.Submeshes.Count,result.Mesh.Submeshes.Count);
            SpringPointNear(new Vec3(5.15f,5.85f,6.92f),result.Mesh.Positions[0]);
            for(int i=0;i<source.VertexCount;i++)
            {
                SpringPointNear(source.Normals[i],result.Mesh.Normals[i]);
                Equal(-source.Tangents[i].W,result.Mesh.Tangents[i].W);
                True(source.Uv0[i].Equals(result.Mesh.Uv0[i]));
            }
            for(int s=0;s<source.Submeshes.Count;s++)
            {
                var original=source.Submeshes[s]; var changed=result.Mesh.Submeshes[s];
                for(int i=0;i<original.Length;i+=3)
                { Equal(original[i],changed[i]); Equal(original[i+1],changed[i+2]); Equal(original[i+2],changed[i+1]); }
            }
            var restored = SourceMeshTransform.Apply(result.Mesh,matrix.Inverse()).Mesh;
            Equal(source.TopologyHash,restored.TopologyHash);
            for(int i=0;i<source.VertexCount;i++) SpringPointNear(source.Positions[i],restored.Positions[i]);
        });
        Test("Source mesh coordinate conversion commutes with position morph and updates topology pin", () =>
        {
            var source=AuthoringFixtures.Panel(1); string id=Guid.NewGuid().ToString("D");
            var morphs=MorphSet.Create(source,new[] {MorphTarget.Create(source,id,"shift",new[] {new MorphDelta(0,new Vec3(.1f,.2f,.3f))})});
            var matrix=SourceAffine.FromTrs(new Vec3(10,20,30),new Vec4(0,0,0,1),new Vec3(-2,3,4));
            var result=SourceMeshTransform.Apply(source,matrix,morphs);
            Equal(id,result.Morphs.Targets[0].TargetId); Equal("shift",result.Morphs.Targets[0].Name);
            Equal(result.Mesh.TopologyHash,result.Morphs.MeshTopologyHash);
            True(result.Mesh.TopologyHash!=source.TopologyHash);
            SpringPointNear(new Vec3(-.2f,.6f,1.2f),result.Morphs.Targets[0].Deltas[0]);
            var weights=new Dictionary<string,float> {{id,.5f}};
            var before=SourceMeshTransform.Apply(MorphDeformer.Apply(source,morphs,weights),matrix).Mesh;
            var after=MorphDeformer.Apply(result.Mesh,result.Morphs,weights);
            for(int i=0;i<source.VertexCount;i++) SpringPointNear(before.Positions[i],after.Positions[i]);
        });
        Test("Source mesh rejects invalid directions and stale morphs without modifying inputs", () =>
        {
            var source=AuthoringFixtures.Panel(1); string hash=source.ContentHash;
            var missingNormals=new MeshData(source.Positions.ToArray(),Array.Empty<Vec3>(),source.Tangents.ToArray(),source.Uv0.ToArray(),source.Submeshes.ToArray());
            Expect("UNSUPPORTED_FORMAT",()=>SourceMeshTransform.Apply(missingNormals,SourceAffine.Identity));
            var parallelTangents=new MeshData(source.Positions.ToArray(),source.Normals.ToArray(),Enumerable.Repeat(new Vec4(0,0,1,1),source.VertexCount).ToArray(),source.Uv0.ToArray(),source.Submeshes.ToArray());
            Expect("INVALID_AFFINE",()=>SourceMeshTransform.Apply(parallelTangents,SourceAffine.Identity));
            var flipped=SourceMeshTransform.Apply(source,SourceAffine.FromTrs(new Vec3(),new Vec4(0,0,0,1),new Vec3(-1,1,1))).Mesh;
            var stale=MorphSet.Create(flipped,new[] {MorphTarget.Create(flipped,Guid.NewGuid().ToString("D"),"stale",new[] {new MorphDelta(0,new Vec3(0,.1f,0))})});
            Expect("MORPH_TOPOLOGY_CHANGED",()=>SourceMeshTransform.Apply(source,SourceAffine.Identity,stale));
            Equal(hash,source.ContentHash);
            var bare=new MeshData(source.Positions.ToArray(),Array.Empty<Vec3>(),Array.Empty<Vec4>(),Array.Empty<Vec2>(),source.Submeshes.ToArray());
            var result=SourceMeshTransform.Apply(bare,SourceAffine.Identity);
            Equal(0,result.Mesh.Normals.Count); Equal(0,result.Mesh.Tangents.Count); True(result.Morphs==null);
        });
    }
}
