using System;
using System.Linq;
using System.Threading.Tasks;
using NyaForge.Authoring;
using NyaForge.Authoring.Paint;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonDerivedDataTests()
    {
        Test("immutable polygon derived cache preserves bytes and changes on geometry or UV replacement",()=>
        {
            var source=PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"));
            var render=PolygonRenderAdapter.Build(source);string hash=PolygonDerivedData.ContentHash(source),uv=PaintUvBinding.Hash(source);
            True(ReferenceEquals(render,PolygonRenderAdapter.Build(source)));
            Equal(Checks.Hash(PolygonBinaryCodec.Write(source)),hash);Equal(PaintUvBinding.HashUncached(source),uv);
            var moved=source.MoveVertices(new[]{source.Vertices.Keys.First()},new Vec3(0,0,.01f));
            False(ReferenceEquals(render,PolygonRenderAdapter.Build(moved)));False(hash==PolygonDerivedData.ContentHash(moved));Equal(uv,PaintUvBinding.Hash(moved));
            var flipped=new PolygonMesh(source.DomainId,source.Vertices.Values,source.Faces.Select(f=>new CageFace(f.Id,f.Material,
                f.Corners.Select(c=>new CageCorner(c.Id,c.VertexId,new Vec2(1-c.Uv0.Value.X,c.Uv0.Value.Y),c.Normal,c.Tangent)))));
            False(uv==PaintUvBinding.Hash(flipped));False(render.Mesh.ContentHash==PolygonRenderAdapter.Build(flipped).Mesh.ContentHash);
            Equal(hash,PolygonDerivedData.ContentHash(source));True(ReferenceEquals(render,PolygonRenderAdapter.Build(source)));
        });
        Test("concurrent reads share one immutable polygon projection and stable hashes",()=>
        {
            var source=PolygonPrimitives.Plane(Guid.NewGuid().ToString("D"));
            var renders=new PolygonRenderMesh[16];var hashes=new string[16];
            Parallel.For(0,16,i=> { renders[i]=PolygonRenderAdapter.Build(source);hashes[i]=PolygonDerivedData.ContentHash(source)+PaintUvBinding.Hash(source); });
            True(renders.All(r=>ReferenceEquals(r,renders[0])));True(hashes.All(h=>h==hashes[0]));
        });
    }
}
