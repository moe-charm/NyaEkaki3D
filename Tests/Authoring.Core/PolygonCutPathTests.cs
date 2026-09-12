using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Topology;
internal static partial class Program
{
    static void RunPolygonCutPathTests()
    {
        Test("cut path crosses two faces with one shared seam vertex",()=>
        {
            var positions=new[]{new Vec3(0,0,0),new Vec3(1,0,0),new Vec3(2,0,0),new Vec3(0,1,0),new Vec3(1,1,0),new Vec3(2,1,0)};
            ulong corner=0;
            CageFace Face(ulong id,ulong[] vertices)=>new CageFace(id,0,vertices.Select(v=>new CageCorner(++corner,v,new Vec2(positions[v-1].X+(id==2 ? 10 : 0),positions[v-1].Y))));
            var mesh=new PolygonMesh(GraphId(),positions.Select((p,i)=>new CageVertex((ulong)i+1,p)),new[]{Face(1,new ulong[]{1,2,5,4}),Face(2,new ulong[]{2,3,6,5})});
            var path=new[]{new EdgeCutLocation(1,4,.5f),new EdgeCutLocation(2,5,.5f),new EdgeCutLocation(3,6,.5f)};
            var result=PolygonCutPath.Cut(mesh,path);Equal(9,result.Vertices.Count);Equal(4,result.Faces.Count);Equal(8,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            var shared=result.Faces.SelectMany(f=>f.Corners).Where(c=>c.VertexId==8).ToArray();Equal(4,shared.Length);Equal(2,shared.Select(c=>c.Uv0.Value.X).Distinct().Count());
            Equal(2,result.EdgeFaces[new CageEdgeId(7,8)].Count);Equal(2,result.EdgeFaces[new CageEdgeId(8,9)].Count);
            True(PolygonBinaryCodec.Write(result).SequenceEqual(PolygonBinaryCodec.Write(PolygonBinaryCodec.Read(PolygonBinaryCodec.Write(result)))));
            Expect("INVALID_CUT_PATH",()=>PolygonCutPath.Cut(mesh,new[]{path[0],path[1],path[0]}));
            Expect("INVALID_CUT_PATH",()=>PolygonCutPath.Cut(mesh,new[]{path[0],path[2]}));
            Equal(6,mesh.Vertices.Count);Equal(2,mesh.Faces.Count);
        });
    }
}
