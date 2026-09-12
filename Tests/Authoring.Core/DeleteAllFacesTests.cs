using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunDeleteAllFacesTests()
    {
        Test("delete all faces preserves preexisting loose points and ID history",()=>
        {
            var plane=PolygonPrimitives.Plane(GraphId());
            var mesh=PolygonVertexCreation.Add(plane,new Vec3(1,2,3));
            var deleted=PolygonDeletion.DeleteFaces(mesh,new ulong[]{1});
            Equal(0,deleted.Faces.Count);Equal(5UL,deleted.Vertices.Keys.Single());
            var reopened=PolygonBinaryCodec.Read(PolygonBinaryCodec.Write(deleted));
            var a=PolygonVertexCreation.Add(reopened,new Vec3(1,3,3));var b=PolygonVertexCreation.Add(a,new Vec3(2,2,3));
            var face=PolygonFaceCreation.Create(b,new ulong[]{5,6,7});
            True(face.Faces[0].Id>mesh.IdWatermarks.Face);True(face.Faces[0].Corners.All(c=>c.Id>mesh.IdWatermarks.Corner));
            Equal(1,PolygonRenderAdapter.Build(face).Mesh.TriangleCount);
        });
        Test("delete final face graph native Undo redo retains empty result",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));string before=w.Document.StateHash;
            Ok(Execute(w,AuthoringOperation.DeletePolygonFaces(GraphEditing.Context(graph,edit),new ulong[]{1})));
            string after=w.Document.StateHash;True(w.Preview.IsComplete && w.Preview.Output.Mesh==null);Equal(0,w.Preview.Output.Polygon.Vertices.Count);
            string dir=Dir("delete-last-face");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Expect("NO_RENDERABLE_FACES",()=>BakeStore.Export(Dir("deleted-last-bake"),reopened));
            Ok(Execute(w,AuthoringOperation.Undo()));Equal(before,w.Document.StateHash);Equal(2,w.Evaluate().TriangleCount);
            Ok(Execute(w,AuthoringOperation.Redo()));Equal(after,w.Document.StateHash);True(w.Preview.Output.Mesh==null);
        });
    }
}
