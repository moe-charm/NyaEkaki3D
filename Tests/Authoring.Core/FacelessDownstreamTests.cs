using System;
using System.Linq;
using System.Collections.Generic;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Topology;
internal static partial class Program
{
    static void RunFacelessDownstreamTests()
    {
        foreach(string kind in new[]{"mirror","paint","material"}) Test("faceless downstream "+kind+" preserves graph and recovers with Undo",()=>
        {
            string source=GraphId(),edit=GraphId(),next=GraphId(),material=GraphId(),output=GraphId();
            var nodes=new List<GraphNode>{GraphNode.Polygon(source,PolygonPrimitives.Plane(GraphId()),new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)};
            var edges=new List<GraphEdge>{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",next,"mesh")};
            if(kind=="mirror") { nodes.Add(GraphNode.Mirror(next));edges.Add(new GraphEdge(next,"mesh",output,"mesh")); }
            else if(kind=="paint") { nodes.Add(GraphNode.Paint(next,16,16));edges.Add(new GraphEdge(edit,"mesh",output,"mesh"));edges.Add(new GraphEdge(next,"image",output,"baseColor")); }
            else { nodes.Add(GraphNode.AssignMaterial(next));nodes.Add(GraphNode.StandardMaterial(material));edges.Add(new GraphEdge(material,"material",next,"material"));edges.Add(new GraphEdge(next,"mesh",output,"mesh")); }
            var graph=new AuthoringGraph(GraphId(),nodes,edges,output);var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));True(w.Preview.IsComplete);string before=w.Document.StateHash;
            Ok(Execute(w,AuthoringOperation.DeletePolygonFaces(GraphEditing.Context(graph,edit),new ulong[]{1})));
            False(w.Preview.IsComplete);True(w.Preview.Evaluation.Diagnostics.Any(d=>d.NodeId==next && d.Code=="NO_RENDERABLE_FACES"));
            Equal(0,w.Preview.Evaluation.MeshOutputs[edit].Polygon.Faces.Count);True(ReferenceEquals(graph.Nodes[next],w.Document.Objects[0].Graph.Nodes[next]));
            string deleted=w.Document.StateHash;string dir=Dir("faceless-"+kind);ProjectStore.Save(dir,w,0);var reopened=ProjectStore.Open(dir);Equal(deleted,reopened.Document.StateHash);False(reopened.Preview.IsComplete);
            Expect("GRAPH_INCOMPLETE",()=>BakeStore.Export(Dir("faceless-export-"+kind),reopened));
            Ok(Execute(w,AuthoringOperation.Undo()));True(w.Preview.IsComplete);Equal(before,w.Document.StateHash);
            Ok(Execute(w,AuthoringOperation.Redo()));False(w.Preview.IsComplete);Equal(deleted,w.Document.StateHash);
        });
    }
}
