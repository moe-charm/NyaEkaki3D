using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonEdgeInsertionTests()
    {
        Test("edge insertion adds one vertex and interpolates boundary attributes",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());var c=mesh.Faces[0].Corners;
            var ids=new[]{c[0].VertexId,c[1].VertexId};var result=PolygonEdgeInsertion.Insert(mesh,ids,.25f);
            Equal(5,result.Vertices.Count);Equal(1,result.Faces.Count);Equal(5,result.Faces[0].Corners.Count);Equal(3,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            var expected=mesh.Vertices[ids.Min()].Position*.75f+mesh.Vertices[ids.Max()].Position*.25f;
            var vertex=result.Vertices[result.IdWatermarks.Vertex];Near(expected.X,vertex.Position.X);Near(expected.Y,vertex.Position.Y);Near(expected.Z,vertex.Position.Z);
            True(result.Faces[0].Corners.Single(p=>p.VertexId==vertex.Id).Id>mesh.IdWatermarks.Corner);
            foreach(var old in c) True(result.Faces[0].Corners.Any(p=>ReferenceEquals(p,old)));
            Equal(4,mesh.Vertices.Count);Expect("INVALID_EDGE_POSITION",()=>PolygonEdgeInsertion.Insert(mesh,ids,0));
            Expect("INVALID_EDGE_POSITION",()=>PolygonEdgeInsertion.Insert(mesh,ids,1));
            Expect("EDGE_NOT_FOUND",()=>PolygonEdgeInsertion.Insert(mesh,new[]{c[0].VertexId,c[2].VertexId},.5f));
            Equal(2,PolygonFaceSplit.Split(result,new[]{vertex.Id,c[2].VertexId}).Faces.Count);
        });
        Test("shared edge insertion keeps a single vertex but separate UV seam corners",()=>
        {
            var plane=PolygonPrimitives.Plane(GraphId());var c=plane.Faces[0].Corners;
            var mesh=PolygonFaceSplit.Split(plane,new[]{c[0].VertexId,c[2].VertexId});
            mesh=new PolygonMesh(mesh.DomainId,mesh.Vertices.Values,mesh.Faces.Select((f,i)=>i==0 ? f : new CageFace(f.Id,f.Material,f.Corners.Select(p=>new CageCorner(p.Id,p.VertexId,new Vec2(p.Uv0.Value.X+2,p.Uv0.Value.Y),p.Normal,p.Tangent)))),mesh.IdWatermarks);
            var result=PolygonEdgeInsertion.Insert(mesh,new[]{c[0].VertexId,c[2].VertexId},.5f);
            Equal(5,result.Vertices.Count);True(result.Faces.All(f=>f.Corners.Count==4));Equal(4,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            var inserted=result.Faces.Select(f=>f.Corners.Single(p=>p.VertexId==result.IdWatermarks.Vertex)).ToArray();
            Near(2,inserted[1].Uv0.Value.X-inserted[0].Uv0.Value.X);True(inserted[0].Id!=inserted[1].Id);
            True(result.EdgeFaces.Where(p=>p.Key.A==result.IdWatermarks.Vertex || p.Key.B==result.IdWatermarks.Vertex).All(p=>p.Value.Count==2));
        });
        Test("edge insertion command Undo redo native and Bake",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());var c=mesh.Faces[0].Corners;string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.InsertPolygonEdgeVertex(GraphEditing.Context(graph,edit),new[]{c[0].VertexId,c[1].VertexId},.3f))));string after=w.Document.StateHash;
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("edge-insert-native");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("edge-insert-bake"),reopened)).MeshContentHash);
        });
    }
}
