using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static PolygonMesh BridgeFixture()
    {
        var plane=PolygonPrimitives.Plane(GraphId());
        var vertices=plane.Vertices.Values.Concat(plane.Vertices.Values.Select(v=>new CageVertex(v.Id+4,v.Position+new Vec3(0,0,.2f))));
        var back=new CageFace(2,0,plane.Faces[0].Corners.Reverse().Select(c=>new CageCorner(c.Id+4,c.VertexId+4,c.Uv0,c.Normal.Value*-1,new Vec4(c.Tangent.Value.X,c.Tangent.Value.Y,c.Tangent.Value.Z,-c.Tangent.Value.W))));
        return new PolygonMesh(plane.DomainId,vertices,new[]{plane.Faces[0],back});
    }
    static void RunPolygonBridgeTests()
    {
        Test("bridge joins opposite loops into a closed box with preserved originals",()=>
        {
            var mesh=BridgeFixture();var loops=PolygonBoundaries.Find(mesh);
            var result=PolygonBridge.Connect(mesh,loops[0],loops[1]);
            Equal(6,result.Faces.Count);Equal(8,result.Vertices.Count);Equal(12,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            Equal(0,PolygonBoundaries.Find(result).Count);True(result.EdgeFaces.All(p=>p.Value.Count==2));
            foreach(var edge in result.EdgeFaces)
            {
                int forward=result.Faces.Where(f=>edge.Value.Contains(f.Id)).Sum(f=>Enumerable.Range(0,f.Corners.Count).Count(i=>f.Corners[i].VertexId==edge.Key.A && f.Corners[(i+1)%f.Corners.Count].VertexId==edge.Key.B));Equal(1,forward);
            }
            foreach(var original in mesh.Faces) True(ReferenceEquals(original,result.Faces.Single(f=>f.Id==original.Id)));
            True(result.Faces.Skip(2).All(f=>f.Id>mesh.IdWatermarks.Face && f.Corners.All(c=>c.Id>mesh.IdWatermarks.Corner)));
            Expect("INVALID_BRIDGE",()=>PolygonBridge.Connect(mesh,loops[0],loops[0]));
            Expect("INVALID_BRIDGE",()=>PolygonBridge.Connect(mesh,loops[0],loops[1],4));
            var strip=PolygonDeletion.DeleteFaces(result,new ulong[]{1,2});var stripLoops=PolygonBoundaries.Find(strip);
            Expect("DUPLICATE_FACE",()=>PolygonBridge.Connect(strip,stripLoops[0],stripLoops[1]));
        });
        Test("bridge command Undo redo native and Bake",()=>
        {
            var mesh=BridgeFixture();var loops=PolygonBoundaries.Find(mesh);string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            var command=w.NewCommand(AuthoringOperation.BridgePolygonBoundaries(GraphEditing.Context(graph,edit),loops[0],loops[1]));
            Ok(commands.Execute(command));string after=w.Document.StateHash;Ok(commands.Execute(command));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("bridge-native");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("bridge-bake"),reopened)).MeshContentHash);
        });
    }
}
