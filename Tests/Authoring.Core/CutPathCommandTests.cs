using System;
using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
internal static partial class Program
{
    static void RunCutPathCommandTests()
    {
        Test("cut path command snapshots ordered payload and supports atomic undo native bake",()=>
        {
            var positions=new[]{new Vec3(0,0,0),new Vec3(1,0,0),new Vec3(2,0,0),new Vec3(0,1,0),new Vec3(1,1,0),new Vec3(2,1,0)};
            ulong corner=0;
            CageFace Face(ulong id,ulong[] vertices)=>new CageFace(id,0,vertices.Select(v=>new CageCorner(++corner,v,new Vec2(positions[v-1].X+(id==2 ? 10 : 0),positions[v-1].Y))));
            var mesh=new PolygonMesh(GraphId(),positions.Select((p,i)=>new CageVertex((ulong)i+1,p)),new[]{Face(1,new ulong[]{1,2,5,4}),Face(2,new ulong[]{2,3,6,5})});
            string source=GraphId(),edit=GraphId(),output=GraphId();
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,mesh,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));var context=GraphEditing.Context(graph,edit);
            string before=w.Document.StateHash;long revision=w.Document.DocumentRevision;
            // First diagonal (new point to vertex 2) is valid; second (2 to 3) already exists.
            var invalid=new[]{new EdgeCutLocation(1,4,.5f),new EdgeCutLocation(2,5,0),new EdgeCutLocation(3,6,0)};
            Code("INVALID_DIAGONAL",Execute(w,AuthoringOperation.CutPolygonPath(context,invalid)));
            Equal(before,w.Document.StateHash);Equal(revision,w.Document.DocumentRevision);Equal(6,w.Preview.Output.Polygon.Vertices.Count);
            var path=new[]{new EdgeCutLocation(1,4,.5f),new EdgeCutLocation(2,5,.5f),new EdgeCutLocation(3,6,.5f)};
            var operation=AuthoringOperation.CutPolygonPath(context,path);path[0]=new EdgeCutLocation(1,4,.25f);
            Near(.5f,operation.CutPath[0].Fraction);
            Expect("INVALID_CUT_PATH",()=>AuthoringOperation.CutPolygonPath(context,new EdgeCutLocation[]{null,null}));
            Expect("INVALID_CUT_PATH",()=>AuthoringOperation.CutPolygonPath(context,Enumerable.Repeat(path[0],257)));
            var command=w.NewCommand(operation);var service=new AuthoringCommandService(w);Ok(service.Execute(command));
            string after=w.Document.StateHash;Equal(9,w.Preview.Output.Polygon.Vertices.Count);Equal(4,w.Preview.Output.Polygon.Faces.Count);
            Ok(service.Execute(command));Equal(after,w.Document.StateHash);
            command.Operations=new[]{AuthoringOperation.CutPolygonPath(context,path)};Code("COMMAND_ID_REUSED",service.Execute(command));Equal(after,w.Document.StateHash);
            command.Operations=new[]{AuthoringOperation.CutPolygonPath(context,operation.CutPath.Reverse())};Code("COMMAND_ID_REUSED",service.Execute(command));Equal(after,w.Document.StateHash);
            command.Operations=new[]{operation};Ok(service.Execute(command));
            Ok(Execute(w,AuthoringOperation.Undo()));Equal(before,w.Document.StateHash);Equal(6,w.Preview.Output.Polygon.Vertices.Count);
            Ok(Execute(w,AuthoringOperation.Redo()));Equal(after,w.Document.StateHash);
            string dir=Dir("cut-path-command");ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(after,reopened.Document.StateHash);
            True(PolygonBinaryCodec.Write(w.Preview.Output.Polygon).SequenceEqual(PolygonBinaryCodec.Write(reopened.Preview.Output.Polygon)));
            Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("cut-path-command-bake"),reopened)).MeshContentHash);
        });
    }
}

