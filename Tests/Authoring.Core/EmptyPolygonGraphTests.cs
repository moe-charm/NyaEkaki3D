using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;

internal static partial class Program
{
    static void RunEmptyPolygonGraphTests()
    {
        Test("faceless graph supports command save reopen first face and Undo",()=>
        {
            string source=GraphId(),edit=GraphId(),output=GraphId();
            var polygon=new PolygonMesh(GraphId(),Array.Empty<CageVertex>(),Array.Empty<CageFace>());
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},
                new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            True(w.Preview.IsComplete);True(w.Preview.Output.Mesh==null);True(w.Evaluate()==null);
            Expect("NO_RENDERABLE_FACES",()=>BakeStore.Export(Dir("empty-graph-export"),w));
            foreach(var p in new[]{new Vec3(),new Vec3(.1f,0,0),new Vec3(0,.1f,0)})
            {
                var context=GraphEditing.Context(w.Document.Objects[0].Graph,edit);
                Ok(Execute(w,AuthoringOperation.AddPolygonVertex(context,p)));
                string dir=Dir("empty-graph-stage-"+w.Preview.Output.Polygon.Vertices.Count);
                ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);
                Equal(w.Document.StateHash,reopened.Document.StateHash);True(reopened.Preview.IsComplete && reopened.Preview.Output.Mesh==null);
            }
            var current=GraphEditing.Context(w.Document.Objects[0].Graph,edit);
            Ok(Execute(w,AuthoringOperation.TranslatePolygonVertices(current,new ulong[]{3},new Vec3(0,.01f,0))));
            string before=w.Document.StateHash;
            Code("NO_RENDERABLE_FACES",Execute(w,AuthoringOperation.ProjectPolygonUv(current)));Equal(before,w.Document.StateHash);
            Ok(Execute(w,AuthoringOperation.CreatePolygonFace(current,new ulong[]{1,2,3})));
            Equal(1,w.Evaluate().TriangleCount);string after=w.Document.StateHash;
            Ok(Execute(w,AuthoringOperation.Undo()));Equal(before,w.Document.StateHash);True(w.Preview.IsComplete && w.Preview.Output.Mesh==null);
            Ok(Execute(w,AuthoringOperation.Redo()));Equal(after,w.Document.StateHash);
            string joined=Dir("first-face-graph");ProjectStore.Save(joined,w,0);var opened=ProjectStore.Open(joined);
            Equal(after,opened.Document.StateHash);Equal(w.Evaluate().ContentHash,BakeStore.Read(BakeStore.Export(Dir("first-face-graph-bake"),opened)).MeshContentHash);
        });
    }
}
