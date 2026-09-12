using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonCapTests()
    {
        Test("boundary cap closes extrusion preserving existing attributes and winding",()=>
        {
            var mesh=PolygonExtrusion.Extrude(PolygonPrimitives.Plane(GraphId()),new ulong[]{1},new Vec3(0,0,-.05f));
            var boundary=PolygonBoundaries.Find(mesh);Equal(1,boundary.Count);Equal(4,boundary[0].Length);
            var capped=PolygonCap.Fill(mesh,boundary[0]);Equal(6,capped.Faces.Count);Equal(12,PolygonRenderAdapter.Build(capped).Mesh.TriangleCount);
            Equal(0,PolygonBoundaries.Find(capped).Count);Equal(8,capped.Vertices.Count);
            foreach(var face in mesh.Faces) True(ReferenceEquals(face,capped.Faces.Single(f=>f.Id==face.Id)));
            foreach(var edge in capped.EdgeFaces)
            {
                Equal(2,edge.Value.Count);
                int forward=capped.Faces.Where(f=>edge.Value.Contains(f.Id)).Sum(f=>Enumerable.Range(0,f.Corners.Count).Count(i=>f.Corners[i].VertexId==edge.Key.A && f.Corners[(i+1)%f.Corners.Count].VertexId==edge.Key.B));
                Equal(1,forward);
            }
            Expect("INVALID_SELECTION",()=>PolygonCap.Fill(mesh,new ulong[]{1,2,3}));
            var plane=PolygonPrimitives.Plane(GraphId());Expect("DUPLICATE_FACE",()=>PolygonCap.Fill(plane,PolygonBoundaries.Find(plane)[0]));
            var opened=PolygonDeletion.DeleteFaces(capped,new ulong[]{1});
            Equal(6,PolygonCap.Fill(opened,PolygonBoundaries.Find(opened)[0]).Faces.Count);
        });
        Test("boundary cap command Undo native and Bake roundtrip",()=>
        {
            var mesh=PolygonExtrusion.Extrude(PolygonPrimitives.Plane(GraphId()),new ulong[]{1},new Vec3(0,0,-.05f));
            string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));
            string before=w.Document.StateHash;var operation=AuthoringOperation.FillPolygonBoundary(GraphEditing.Context(graph,edit),PolygonBoundaries.Find(mesh)[0]);
            Ok(commands.Execute(w.NewCommand(operation)));string after=w.Document.StateHash;
            Code("INVALID_SELECTION",commands.Execute(w.NewCommand(operation)));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("cap-native");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("cap-bake"),reopened)).MeshContentHash);
        });
    }
}
