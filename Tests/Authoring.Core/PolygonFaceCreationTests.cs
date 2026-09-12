using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static PolygonMesh FaceCreationFixture()
    {
        var plane=PolygonPrimitives.Plane(GraphId());
        return new PolygonMesh(plane.DomainId,plane.Vertices.Values.Concat(new[]{new CageVertex(9,new Vec3(0,0,.2f))}),plane.Faces);
    }
    static void RunPolygonFaceCreationTests()
    {
        Test("ordered face creation preserves source and checks edge winding",()=>
        {
            var mesh=FaceCreationFixture();var result=PolygonFaceCreation.Create(mesh,new ulong[]{4,1,9},2);
            Equal(2,result.Faces.Count);Equal(3,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            True(ReferenceEquals(mesh.Faces[0],result.Faces[0]));Equal(2,result.Faces[1].Material);
            True(result.Faces[1].Corners.Select(c=>c.VertexId).SequenceEqual(new ulong[]{4,1,9}));
            True(result.Faces[1].Id>mesh.IdWatermarks.Face && result.Faces[1].Corners.All(c=>c.Id>mesh.IdWatermarks.Corner));
            Expect("FACE_WINDING",()=>PolygonFaceCreation.Create(mesh,new ulong[]{1,4,9}));
            Expect("DUPLICATE_FACE",()=>PolygonFaceCreation.Create(result,new ulong[]{4,1,9}));
            Expect("NONMANIFOLD_FACE",()=>PolygonFaceCreation.Create(result,new ulong[]{4,1,3}));
            Expect("INVALID_SELECTION",()=>PolygonFaceCreation.Create(mesh,new ulong[]{4,1,99}));
        });
        Test("face creation supports a concave ordered perimeter and reversed winding",()=>
        {
            var plane=PolygonPrimitives.Plane(GraphId());
            var points=new[]{new Vec3(0,0,1),new Vec3(2,0,1),new Vec3(2,2,1),new Vec3(1,1,1),new Vec3(0,2,1),new Vec3(3,1,1)};
            var mesh=new PolygonMesh(plane.DomainId,plane.Vertices.Values.Concat(points.Select((p,i)=>new CageVertex((ulong)i+10,p))),plane.Faces);
            var ids=new ulong[]{10,11,12,13,14};var front=PolygonFaceCreation.Create(mesh,ids);var back=PolygonFaceCreation.Create(mesh,ids.Reverse());
            Equal(5,PolygonRenderAdapter.Build(front).Mesh.TriangleCount);
            Near(-front.Faces[1].Corners[0].Normal.Value.Z,back.Faces[1].Corners[0].Normal.Value.Z);
            True(front.Faces[1].Corners.Select(c=>c.VertexId).SequenceEqual(ids));
            Expect("SELF_INTERSECTING_FACE",()=>PolygonFaceCreation.Create(mesh,new ulong[]{10,12,11,15}));
        });
        Test("face-create command atomic rejection Undo redo native and Bake",()=>
        {
            var mesh=FaceCreationFixture();string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            Code("FACE_WINDING",commands.Execute(w.NewCommand(AuthoringOperation.CreatePolygonFace(GraphEditing.Context(graph,edit),new ulong[]{1,4,9}))));Equal(before,w.Document.StateHash);
            var command=w.NewCommand(AuthoringOperation.CreatePolygonFace(GraphEditing.Context(graph,edit),new ulong[]{4,1,9}));
            Ok(commands.Execute(command));string after=w.Document.StateHash;Ok(commands.Execute(command));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("face-create-native");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("face-create-bake"),reopened)).MeshContentHash);
        });
    }
}




