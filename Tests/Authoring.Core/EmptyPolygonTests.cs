using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunEmptyPolygonTests()
    {
        Test("empty polygon stores zero counts with v3 and preserves allocation history",()=>
        {
            var empty=new PolygonMesh(GraphId(),Array.Empty<CageVertex>(),Array.Empty<CageFace>(),new PolygonIdWatermarks(40,9,30));
            Equal(0,empty.Vertices.Count);Equal(0,PolygonEditPoints.VertexIds(empty).Length);
            var bytes=PolygonBinaryCodec.Write(empty);Equal(3,BitConverter.ToInt32(bytes,4));Equal(80,bytes.Length);
            var reopened=PolygonBinaryCodec.Read(bytes);True(bytes.SequenceEqual(PolygonBinaryCodec.Write(reopened)));
            Equal(41UL,PolygonVertexCreation.Add(reopened,new Vec3()).Vertices.Keys.Single());
            Expect("NO_RENDERABLE_FACES",()=>PolygonRenderAdapter.Build(empty));
            var legacy=(byte[])bytes.Clone();Array.Copy(BitConverter.GetBytes(2),0,legacy,4,4);
            Expect("BUDGET_EXCEEDED",()=>PolygonBinaryCodec.Read(legacy));
            Expect("INVALID_BLOB",()=>PolygonBinaryCodec.Read(bytes.Take(70).ToArray()));
            var flags=(byte[])bytes.Clone();flags[52]=1;Expect("INVALID_BLOB",()=>PolygonBinaryCodec.Read(flags));
        });
        Test("first face can be created after saving one and two point stages",()=>
        {
            var mesh=new PolygonMesh(GraphId(),Array.Empty<CageVertex>(),Array.Empty<CageFace>());
            foreach(var position in new[]{new Vec3(0,0,0),new Vec3(.1f,0,0),new Vec3(0,.1f,0)})
            {
                mesh=PolygonVertexCreation.Add(mesh,position);
                mesh=PolygonBinaryCodec.Read(PolygonBinaryCodec.Write(mesh));
                Equal(0,mesh.Faces.Count);Equal(mesh.Vertices.Count,PolygonEditPoints.VertexIds(mesh).Length);
            }
            var face=PolygonFaceCreation.Create(mesh,new ulong[]{1,2,3});
            Equal(1,face.Faces.Count);Equal(1,PolygonRenderAdapter.Build(face).Mesh.TriangleCount);
            True(face.Faces[0].Corners.All(c=>c.Uv0.HasValue && c.Normal.HasValue && c.Tangent.HasValue));
            var bytes=PolygonBinaryCodec.Write(face);Equal(1,BitConverter.ToInt32(bytes,4));
            Equal(PolygonRenderAdapter.Build(face).Mesh.ContentHash,PolygonRenderAdapter.Build(PolygonBinaryCodec.Read(bytes)).Mesh.ContentHash);
        });
    }
}
