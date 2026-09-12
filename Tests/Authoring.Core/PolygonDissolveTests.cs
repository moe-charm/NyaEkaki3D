using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
internal static partial class Program
{
    static PolygonMesh DissolveFan()
    {
        var positions=new[]{new Vec3(0,0,0),new Vec3(1,0,0),new Vec3(1,1,0),new Vec3(0,1,0),new Vec3(.5f,.5f,0)};
        var vertices=positions.Select((p,i)=>new CageVertex((ulong)i+1,p));
        ulong corner=0;
        var faces=Enumerable.Range(0,4).Select(i=>new CageFace((ulong)i+1,0,new[]{(ulong)i+1,(ulong)((i+1)%4)+1,5UL}.Select(v=>new CageCorner(++corner,v,new Vec2(positions[v-1].X,positions[v-1].Y))))).ToArray();
        return new PolygonMesh(GraphId(),vertices,faces);
    }
    static void RunPolygonDissolveTests()
    {
        Test("dissolve four-face fan removes internal vertex and preserves boundary attributes",()=>
        {
            var mesh=DissolveFan();var result=PolygonFaceDissolve.Dissolve(mesh,new ulong[]{4,2,1,3});
            Equal(1,result.Faces.Count);Equal(4,result.Vertices.Count);Equal(1UL,result.Faces[0].Id);Equal(4,result.Faces[0].Corners.Count);
            Equal(2,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);Equal(mesh.IdWatermarks.Vertex,result.IdWatermarks.Vertex);Equal(mesh.IdWatermarks.Corner,result.IdWatermarks.Corner);
            foreach(var c in result.Faces[0].Corners) True(mesh.Faces.SelectMany(f=>f.Corners).Any(original=>ReferenceEquals(c,original)));
            Equal(PolygonRenderAdapter.Build(result).Mesh.ContentHash,PolygonRenderAdapter.Build(PolygonFaceDissolve.Dissolve(mesh,new ulong[]{1,2,3,4})).Mesh.ContentHash);
            Expect("INVALID_FACE_MERGE",()=>PolygonFaceDissolve.Dissolve(mesh,new ulong[]{1,3}));
            var seam=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,mesh.Faces.Select(f=>f.Id==2 ? new CageFace(f.Id,f.Material,f.Corners.Select(c=>c.VertexId==5 ? new CageCorner(c.Id,c.VertexId,new Vec2(.9f,.9f)) : c)) : f));
            Expect("ATTRIBUTE_SEAM",()=>PolygonFaceDissolve.Dissolve(seam,new ulong[]{1,2,3,4}));
        });
        Test("dissolve refuses a region with an inner hole",()=>
        {
            var positions=new[]{new Vec3(0,0,0),new Vec3(3,0,0),new Vec3(3,3,0),new Vec3(0,3,0),new Vec3(1,1,0),new Vec3(2,1,0),new Vec3(2,2,0),new Vec3(1,2,0)};
            ulong corner=0;
            var faces=Enumerable.Range(0,4).Select(i=>new CageFace((ulong)i+1,0,new[]{(ulong)i+1,(ulong)((i+1)%4)+1,(ulong)((i+1)%4)+5,(ulong)i+5}.Select(v=>new CageCorner(++corner,v)))).ToArray();
            var ring=new PolygonMesh(GraphId(),positions.Select((p,i)=>new CageVertex((ulong)i+1,p)),faces);
            Expect("INVALID_FACE_MERGE",()=>PolygonFaceDissolve.Dissolve(ring,new ulong[]{1,2,3,4}));
        });
        Test("dissolve command Undo redo save and Bake",()=>
        {
            var mesh=DissolveFan();string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));string before=w.Document.StateHash;
            var context=GraphEditing.Context(graph,edit);Code("INVALID_FACE_MERGE",Execute(w,AuthoringOperation.DissolvePolygonFaces(context,new ulong[]{1,3})));Equal(before,w.Document.StateHash);
            Ok(Execute(w,AuthoringOperation.DissolvePolygonFaces(context,new ulong[]{1,2,3,4})));string after=w.Document.StateHash;
            Ok(Execute(w,AuthoringOperation.Undo()));Equal(before,w.Document.StateHash);Ok(Execute(w,AuthoringOperation.Redo()));Equal(after,w.Document.StateHash);
            string dir=Dir("dissolved-fan");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("dissolved-fan-bake"),reopened)).MeshContentHash);
        });
    }
}

