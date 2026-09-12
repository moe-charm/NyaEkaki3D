using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonVertexCreationTests()
    {
        Test("loose vertex preserves faces and allocates above history",()=>
        {
            var plane=PolygonPrimitives.Plane(GraphId());
            var mesh=PolygonVertexCreation.Add(plane,new Vec3(0,0,.2f));
            True(PolygonEditPoints.VertexIds(mesh).Take(4).SequenceEqual(PolygonEditPoints.VertexIds(plane)));Equal(5UL,PolygonEditPoints.VertexIds(mesh).Last());Equal(5,mesh.Vertices.Count);Equal(5UL,mesh.IdWatermarks.Vertex);True(ReferenceEquals(plane.Faces[0],mesh.Faces[0]));
            Equal(PolygonRenderAdapter.Build(plane).Mesh.ContentHash,PolygonRenderAdapter.Build(mesh).Mesh.ContentHash);
            var face=PolygonFaceCreation.Create(mesh,new ulong[]{4,1,5});Equal(3,PolygonRenderAdapter.Build(face).Mesh.TriangleCount);
            var removed=PolygonDeletion.DeleteFaces(face,new[]{face.Faces.Last().Id});
            Equal(4,removed.Vertices.Count);var next=PolygonVertexCreation.Add(removed,new Vec3(1,2,3));True(next.Vertices.ContainsKey(6));
            Expect("NON_FINITE",()=>PolygonVertexCreation.Add(mesh,new Vec3(float.NaN,0,0)));
        });
        Test("loose vertex command replay Undo redo native and face creation",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            var command=w.NewCommand(AuthoringOperation.AddPolygonVertex(GraphEditing.Context(graph,edit),new Vec3(0,0,.2f)));
            Ok(commands.Execute(command));string after=w.Document.StateHash;True(after!=before);Equal(5,w.Preview.Output.Polygon.Vertices.Count);Equal(2,w.Evaluate().TriangleCount);Ok(commands.Execute(command));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("vertex-create-native");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);Equal(5,reopened.Preview.Output.Polygon.Vertices.Count);Near(.2f,reopened.Preview.Output.Polygon.Vertices[5].Position.Z);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("vertex-create-bake"),reopened)).MeshContentHash);
        });
    }
}






