using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonWeldTests()
    {
        Test("weld centroid retains IDs and face corners deterministically",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());
            var result=PolygonWeld.AtCenter(mesh,new ulong[]{1,4});
            Equal(3,result.Vertices.Count);Equal(3,result.Faces[0].Corners.Count);
            var expected=(mesh.Vertices[1].Position+mesh.Vertices[4].Position)*.5f;
            Near(expected.X,result.Vertices[1].Position.X);Near(expected.Y,result.Vertices[1].Position.Y);
            Equal(mesh.IdWatermarks.Vertex,result.IdWatermarks.Vertex);Equal(mesh.IdWatermarks.Corner,result.IdWatermarks.Corner);
            Equal(1UL,result.Faces[0].Corners[0].Id);Equal(mesh.Faces[0].Corners[0].Uv0,result.Faces[0].Corners[0].Uv0);
            Equal(1,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            Equal(PolygonRenderAdapter.Build(result).Mesh.ContentHash,PolygonRenderAdapter.Build(PolygonWeld.AtCenter(mesh,new ulong[]{4,1})).Mesh.ContentHash);
            Expect("INVALID_WELD",()=>PolygonWeld.AtCenter(mesh,new ulong[]{1,3}));
            Expect("EMPTY_POLYGON",()=>PolygonWeld.AtCenter(mesh,new ulong[]{1,2,3,4}));
            Expect("INVALID_SELECTION",()=>PolygonWeld.AtCenter(mesh,new ulong[]{1,1}));
        });
        Test("weld removes collapsed triangle and rejects disconnected vertex fans",()=>
        {
            var plane=PolygonPrimitives.Plane(GraphId());
            var split=PolygonFaceSplit.Split(plane,new ulong[]{1,3});
            var result=PolygonWeld.AtCenter(split,new ulong[]{1,4});
            Equal(1,result.Faces.Count);Equal(3,result.Vertices.Count);
            var disconnected=BridgeFixture();
            Expect("NONMANIFOLD_WELD",()=>PolygonWeld.AtCenter(disconnected,new ulong[]{1,5}));
        });
        Test("weld closed surface retains per-face UV seams and unused original vertices",()=>
        {
            var caps=BridgeFixture();var loops=PolygonBoundaries.Find(caps);var box=PolygonBridge.Connect(caps,loops[0],loops[1]);
            box=new PolygonMesh(box.DomainId,box.Vertices.Values.Concat(new[]{new CageVertex(99,new Vec3(2,2,2))}),box.Faces);
            var result=PolygonWeld.AtCenter(box,new ulong[]{1,4});
            True(result.Vertices.ContainsKey(99));Equal(0,PolygonBoundaries.Find(result).Count);
            foreach(var corner in result.Faces.SelectMany(f=>f.Corners))
            {
                var original=box.Faces.SelectMany(f=>f.Corners).Single(c=>c.Id==corner.Id);
                Equal(original.Uv0,corner.Uv0);Equal(original.Normal,corner.Normal);Equal(original.Tangent,corner.Tangent);
            }
            Equal(box.IdWatermarks.Vertex,result.IdWatermarks.Vertex);
        });
        Test("weld command atomic rejection Undo redo native and Bake",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            Code("INVALID_WELD",commands.Execute(w.NewCommand(AuthoringOperation.WeldPolygonVertices(GraphEditing.Context(graph,edit),new ulong[]{1,3}))));Equal(before,w.Document.StateHash);
            var command=w.NewCommand(AuthoringOperation.WeldPolygonVertices(GraphEditing.Context(graph,edit),new ulong[]{1,4}));
            Ok(commands.Execute(command));string after=w.Document.StateHash;Ok(commands.Execute(command));Equal(after,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("weld-native");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("weld-bake"),reopened)).MeshContentHash);
        });
    }
}

