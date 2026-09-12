using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunPolygonFaceSplitTests()
    {
        Test("face diagonal split preserves attributes and allocates new corner IDs",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());var corners=mesh.Faces[0].Corners;
            var result=PolygonFaceSplit.Split(mesh,new[]{corners[0].VertexId,corners[2].VertexId});
            Equal(2,result.Faces.Count);True(result.Faces.All(f=>f.Corners.Count==3));Equal(2,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            Equal(mesh.Vertices.Count,result.Vertices.Count);Equal(mesh.IdWatermarks.Corner+2,result.IdWatermarks.Corner);
            foreach(var c in result.Faces.SelectMany(f=>f.Corners))
            {
                var before=corners.Single(p=>p.VertexId==c.VertexId);
                True(before.Uv0.Equals(c.Uv0) && before.Normal.Equals(c.Normal) && before.Tangent.Equals(c.Tangent));
            }
            Equal(1,PolygonFaceMerge.Merge(result,result.Faces.Select(f=>f.Id)).Faces.Count);
            Equal(1,mesh.Faces.Count);
            Expect("INVALID_DIAGONAL",()=>PolygonFaceSplit.Split(mesh,new[]{corners[0].VertexId,corners[1].VertexId}));
            Expect("INVALID_SELECTION",()=>PolygonFaceSplit.Split(mesh,new[]{corners[0].VertexId,corners[0].VertexId}));
            Expect("AMBIGUOUS_FACE",()=>PolygonFaceSplit.Split(result,new[]{corners[0].VertexId,corners[2].VertexId}));
        });
        Test("concave face rejects an exterior diagonal and accepts an interior diagonal",()=>
        {
            var positions=new[]{new Vec3(0,0,0),new Vec3(2,0,0),new Vec3(2,1,0),new Vec3(1,1,0),new Vec3(1,2,0),new Vec3(0,2,0)};
            var mesh=new PolygonMesh(GraphId(),positions.Select((p,i)=>new CageVertex((ulong)i+1,p)),new[]{new CageFace(1,0,positions.Select((_,i)=>new CageCorner((ulong)i+1,(ulong)i+1)))});
            Expect("INVALID_DIAGONAL",()=>PolygonFaceSplit.Split(mesh,new ulong[]{3,5}));
            Equal(2,PolygonFaceSplit.Split(mesh,new ulong[]{1,4}).Faces.Count);
        });
        Test("face split command Undo native reopen Bake and rejection preserve state",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());var c=mesh.Faces[0].Corners;string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();var commands=new AuthoringCommandService(w);Ok(commands.Execute(w.NewCommand(AuthoringOperation.AddGraph(graph))));string before=w.Document.StateHash;
            Code("INVALID_DIAGONAL",commands.Execute(w.NewCommand(AuthoringOperation.SplitPolygonFace(GraphEditing.Context(graph,edit),new[]{c[0].VertexId,c[1].VertexId}))));Equal(before,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.SplitPolygonFace(GraphEditing.Context(graph,edit),new[]{c[0].VertexId,c[2].VertexId}))));string after=w.Document.StateHash;
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Undo())));Equal(before,w.Document.StateHash);
            Ok(commands.Execute(w.NewCommand(AuthoringOperation.Redo())));Equal(after,w.Document.StateHash);
            string dir=Dir("split-native");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("split-bake"),reopened)).MeshContentHash);
        });
    }
}
