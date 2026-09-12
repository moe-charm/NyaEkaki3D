using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
internal static partial class Program
{
    static void RunPolygonEdgeCutTests()
    {
        Test("edge cut adds two shared points and splits a quad preserving originals",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());var result=PolygonEdgeCut.Cut(mesh,new ulong[]{1,4,2,3},.25f,.75f);
            Equal(6,result.Vertices.Count);Equal(2,result.Faces.Count);True(result.Faces.All(f=>f.Corners.Count==4));Equal(4,PolygonRenderAdapter.Build(result).Mesh.TriangleCount);
            var a=mesh.Vertices[1].Position*.75f+mesh.Vertices[4].Position*.25f;
            Near(a.X,result.Vertices[5].Position.X);Near(a.Y,result.Vertices[5].Position.Y);
            Equal(2,result.EdgeFaces[new CageEdgeId(5,6)].Count);Equal(mesh.IdWatermarks.Vertex+2,result.IdWatermarks.Vertex);
            foreach(var c in mesh.Faces[0].Corners) True(result.Faces.SelectMany(f=>f.Corners).Any(k=>ReferenceEquals(k,c)));
            var endpoints=PolygonEdgeCut.Cut(mesh,new ulong[]{1,4,2,3},0,1);Equal(4,endpoints.Vertices.Count);Equal(2,endpoints.Faces.Count);
            Expect("INVALID_CUT",()=>PolygonEdgeCut.Cut(mesh,new ulong[]{1,4,4,1},.5f,.5f));
            Expect("INVALID_EDGE_POSITION",()=>PolygonEdgeCut.Cut(mesh,new ulong[]{1,4,2,3},-.1f,.5f));
        });
        Test("edge cut command is atomic after staged insertion and one Undo restores original",()=>
        {
            var mesh=PolygonPrimitives.Plane(GraphId());string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));string before=w.Document.StateHash;var context=GraphEditing.Context(graph,edit);
            Code("INVALID_DIAGONAL",Execute(w,AuthoringOperation.CutPolygonBetweenEdges(context,new ulong[]{1,4,1,2},.5f,0)));Equal(before,w.Document.StateHash);Equal(4,w.Preview.Output.Polygon.Vertices.Count);
            var command=w.NewCommand(AuthoringOperation.CutPolygonBetweenEdges(context,new ulong[]{1,4,2,3},.25f,.75f));var commands=new AuthoringCommandService(w);
            Ok(commands.Execute(command));string after=w.Document.StateHash;Ok(commands.Execute(command));Equal(after,w.Document.StateHash);
            Ok(Execute(w,AuthoringOperation.Undo()));Equal(before,w.Document.StateHash);Ok(Execute(w,AuthoringOperation.Redo()));Equal(after,w.Document.StateHash);
            string dir=Dir("edge-cut");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("edge-cut-bake"),reopened)).MeshContentHash);
        });
    }
}
