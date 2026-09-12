using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Import;

internal static partial class Program
{
    static (SourceSkin skin, SourceSkinBinding binding, MeshData mesh) SimpleSourceSkin()
    {
        var nodes = new SourceNodeTransforms(new string('c',64), new[] {-1,0}, new[] {
            SourceAffine.Identity,
            SourceAffine.FromTrs(new Vec3(0,.2f,0),new Vec4(0,0,0,1),new Vec3(1,1,1)) }, null);
        var skin = new SourceSkin(nodes,0,new[] {0,1},new[] {nodes.World[0].Inverse(),nodes.World[1].Inverse()});
        var mesh = AuthoringFixtures.Panel(1);
        var raw = new List<SourceSkinWeight>();
        for(int i=0;i<mesh.VertexCount;i++)
        {
            if(i == 0) raw.Add(new SourceSkinWeight(i,1,1));
            else if(i == 1) raw.Add(new SourceSkinWeight(i,0,.25f));
            else if(i == 2) { raw.Add(new SourceSkinWeight(i,0,.25f)); raw.Add(new SourceSkinWeight(i,1,.75f)); }
            else raw.Add(new SourceSkinWeight(i,0,1));
        }
        return (skin,SourceSkinBinding.Create(mesh,skin,raw),mesh);
    }

    static void RunSourceSkinDeformerTests()
    {
        Test("Source skin deformer cancels rest inverse binds and applies a posed joint palette", () =>
        {
            var value=SimpleSourceSkin(); var posed = new[] {value.skin.Nodes.World[0],
                SourceAffine.FromTrs(new Vec3(1,.2f,0),new Vec4(0,0,0,1),new Vec3(1,1,1))};
            var output=SourceSkinDeformer.Apply(value.mesh,value.skin,value.binding,posed);
            SpringPointNear(value.mesh.Positions[0]+new Vec3(1,0,0),output.Positions[0]);
            SpringPointNear(value.mesh.Positions[1],output.Positions[1]);
            SpringPointNear(value.mesh.Positions[2]+new Vec3(.75f,0,0),output.Positions[2]);
            for(int i=0;i<value.mesh.VertexCount;i++)
            {
                SpringPointNear(value.mesh.Normals[i],output.Normals[i]);
                Equal(value.mesh.Uv0[i].X,output.Uv0[i].X); Equal(value.mesh.Tangents[i].W,output.Tangents[i].W);
            }
            Equal(value.mesh.TopologyHash,output.TopologyHash);
        });
        Test("Source skin deformer supports many source influences and preserves inputs", () =>
        {
            var value=SimpleSourceSkin(); string sourceHash=value.mesh.ContentHash;
            var output=SourceSkinDeformer.Apply(value.mesh,value.skin,value.binding,new[] {SourceAffine.Identity,value.skin.Nodes.World[1]});
            Equal(sourceHash,value.mesh.ContentHash); Equal(value.binding.MeshTopologyHash,value.mesh.TopologyHash);
            for(int i=0;i<value.mesh.VertexCount;i++) SpringPointNear(value.mesh.Positions[i],output.Positions[i]);
            var weights=Enumerable.Range(0,17).Select(i=>new SourceSkinWeight(0,i,1f/17)).ToArray();
            var expandedNodes=new SourceNodeTransforms(new string('d',64),Enumerable.Repeat(-1,17).ToArray(),Enumerable.Repeat(SourceAffine.Identity,17).ToArray(),null);
            var expandedSkin=new SourceSkin(expandedNodes,0,Enumerable.Range(0,17).ToArray(),null);
            var expanded=SourceSkinBinding.Create(value.mesh,expandedSkin,Enumerable.Range(0,value.mesh.VertexCount).SelectMany(v=>v==0?weights:new[]{new SourceSkinWeight(v,0,1)}));
            Equal(17,expanded.Weights[0].Count);
            SourceSkinDeformer.Apply(value.mesh,expandedSkin,expanded,Enumerable.Repeat(SourceAffine.Identity,17).ToArray());
        });
        Test("Source skin binding and deformer reject stale domains, missing weights and bad palettes", () =>
        {
            var value=SimpleSourceSkin();
            var changed=new MeshData(value.mesh.Positions.ToArray(),value.mesh.Normals.ToArray(),value.mesh.Tangents.ToArray(),value.mesh.Uv0.ToArray(),new[] {new[] {0,1,2,0,3,2},new[] {4,6,5,4,7,6}});
            Expect("SKIN_SOURCE_CHANGED",()=>SourceSkinDeformer.Apply(changed,value.skin,value.binding,new[]{SourceAffine.Identity,SourceAffine.Identity}));
            Expect("POSE_JOINT_COUNT",()=>SourceSkinDeformer.Apply(value.mesh,value.skin,value.binding,new[]{SourceAffine.Identity}));
            var partial=new SourceSkinWeight[ value.mesh.VertexCount - 1 ];
            for(int i=1;i<value.mesh.VertexCount;i++) partial[i-1]=new SourceSkinWeight(i,0,1);
            Expect("UNWEIGHTED_VERTEX",()=>SourceSkinBinding.Create(value.mesh,value.skin,partial));
            var tooMany=Enumerable.Range(0,33).Select(i=>new SourceSkinWeight(0,i,.01f)).ToArray();
            var nodes=new SourceNodeTransforms(new string('e',64),Enumerable.Repeat(-1,33).ToArray(),Enumerable.Repeat(SourceAffine.Identity,33).ToArray(),null);
            var skin=new SourceSkin(nodes,0,Enumerable.Range(0,33).ToArray(),null);
            Expect("INFLUENCE_LIMIT",()=>SourceSkinBinding.Create(value.mesh,skin,Enumerable.Range(0,value.mesh.VertexCount).SelectMany(v=>v==0?tooMany:new[]{new SourceSkinWeight(v,0,1)})));
        });
    }
}
