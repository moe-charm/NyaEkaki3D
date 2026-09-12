using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonDeletionTests()
    {
        Test("face deletion retains cap attributes and removes only orphaned vertices",()=>
        {
            var mesh=PolygonExtrusion.Extrude(PolygonPrimitives.Plane(GraphId()),new ulong[]{1},new Vec3(0,0,-.05f));
            var result=PolygonDeletion.DeleteFaces(mesh,mesh.Faces.Where(f=>f.Id!=1).Select(f=>f.Id));
            Equal(1,result.Faces.Count);Equal(4,result.Vertices.Count);Equal(2,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            Equal(mesh.DomainId,result.DomainId);True(ReferenceEquals(mesh.Faces[0],result.Faces[0]));
            True(result.Vertices.Keys.OrderBy(id=>id).SequenceEqual(result.Faces[0].Corners.Select(c=>c.VertexId).OrderBy(id=>id)));
            Equal(5,mesh.Faces.Count);Equal(8,mesh.Vertices.Count);
            foreach(var ids in new[]{Array.Empty<ulong>(),new ulong[]{1,1},new ulong[]{999}})
                Expect("INVALID_SELECTION",()=>PolygonDeletion.DeleteFaces(mesh,ids));
            var empty=PolygonDeletion.DeleteFaces(mesh,mesh.Faces.Select(f=>f.Id));Equal(0,empty.Faces.Count);Equal(0,empty.Vertices.Count);Equal(mesh.IdWatermarks.Vertex,empty.IdWatermarks.Vertex);
        });
        Test("face deletion command survives replay Undo redo native save and Bake",()=>
        {
            var mesh=PolygonExtrusion.Extrude(PolygonPrimitives.Plane(GraphId()),new ulong[]{1},new Vec3(0,0,-.05f));
            string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            var context=GraphEditing.Context(graph,edit);var command=w.NewCommand(AuthoringOperation.DeletePolygonFaces(context,new ulong[]{1}));
            Ok(commands.Execute(command));string after=w.Document.StateHash;Equal(4,w.Preview.Output.Polygon.Faces.Count);
            Ok(commands.Execute(command));Equal(after,w.Document.StateHash);
            Code("INVALID_SELECTION",commands.Execute(w.NewCommand(AuthoringOperation.DeletePolygonFaces(context,new ulong[]{1}))));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string directory=Dir("delete-faces");ProjectStore.Save(directory,w,0);var reopened=ProjectStore.Open(directory);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("delete-faces-bake"),reopened)).MeshContentHash);
        });
    }
}

