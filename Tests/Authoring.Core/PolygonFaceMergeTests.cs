using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static PolygonMesh MergeFixture()=>TriangleMeshAdapter.Import(GraphId(),PolygonRenderAdapter.Build(PolygonPrimitives.Plane(GraphId())).Mesh);
    static void RunPolygonFaceMergeTests()
    {
        Test("coplanar face merge removes diagonal and retains attributes allocation history and source",()=>
        {
            var mesh=MergeFixture();var ids=mesh.Faces.Select(f=>f.Id).ToArray();
            var result=PolygonFaceMerge.Merge(mesh,ids);
            Equal(1,result.Faces.Count);Equal(4,result.Faces[0].Corners.Count);Equal(2,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            Equal(ids.Min(),result.Faces[0].Id);Equal(mesh.IdWatermarks.Corner,result.IdWatermarks.Corner);Equal(mesh.IdWatermarks.Face,result.IdWatermarks.Face);
            Equal(4,result.EdgeFaces.Count);Equal(5,mesh.EdgeFaces.Count);Equal(2,mesh.Faces.Count);
            foreach(var c in result.Faces[0].Corners) True(mesh.Faces.SelectMany(f=>f.Corners).Any(old=>ReferenceEquals(c,old)));
            Expect("MATERIAL_SEAM",()=>PolygonFaceMerge.Merge(PolygonMaterialAssignment.Assign(mesh,new[]{ids[1]},1),ids));
            var seam=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,mesh.Faces.Select((f,i)=>i==0 ? f : new CageFace(f.Id,f.Material,f.Corners.Select(c=>new CageCorner(c.Id,c.VertexId,new Vec2(c.Uv0.Value.X+.1f,c.Uv0.Value.Y),c.Normal,c.Tangent)))));
            Expect("ATTRIBUTE_SEAM",()=>PolygonFaceMerge.Merge(seam,ids));
            ulong unique=mesh.Faces[1].Corners.Select(c=>c.VertexId).Except(mesh.Faces[0].Corners.Select(c=>c.VertexId)).First();
            Expect("NONPLANAR_MERGE",()=>PolygonFaceMerge.Merge(mesh.MoveVertices(new[]{unique},new Vec3(0,0,.03f)),ids));
            Expect("INVALID_SELECTION",()=>PolygonFaceMerge.Merge(mesh,new[]{ids[0],ids[0]}));
        });
        Test("face merge command Undo redo native and Bake",()=>
        {
            var mesh=MergeFixture();string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.MergePolygonFaces(GraphEditing.Context(graph,edit),mesh.Faces.Select(f=>f.Id).ToArray()))));string after=w.Document.StateHash;
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("merge-native");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("merge-bake"),reopened)).MeshContentHash);
        });
    }
}
