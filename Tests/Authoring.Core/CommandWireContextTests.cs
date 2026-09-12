using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using Newtonsoft.Json.Linq;
internal static partial class Program
{
    static void RunCommandWireContextTests()
    {
        Test("wire graph context edits existing stage and rejects changed upstream",()=>
        {
            string plane,edit;var graph=PlaneGraph(out plane,out edit);var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            var c=GraphEditing.Context(graph,edit);var operation=new JObject
            {
                ["kind"]="graph.vertices.translate",["vertexIds"]=new JArray(0),["delta"]=new JArray(.01,0,0),
                ["context"]=new JObject { ["graphId"]=c.GraphId,["nodeId"]=c.NodeId,["inputSnapshot"]=c.InputSnapshot,["domainId"]=c.DomainId }
            };
            var service=new AuthoringCommandService(w);Ok(service.Execute(CommandWireReader.Read(Wire(w,operation))));
            var expected=GraphEditing.Translate(graph,c,new[]{0},new Vec3(.01f,0,0));Equal(GraphEvaluator.Evaluate(expected).Output.Mesh.ContentHash,w.Evaluate().ContentHash);
            Ok(Execute(w,AuthoringOperation.UpdateNode(GraphNode.Plane(plane,.4f,.1f))));string before=w.Document.StateHash;
            Equal("EDIT_CONTEXT_STALE",service.Execute(CommandWireReader.Read(Wire(w,operation))).Code);Equal(before,w.Document.StateHash);
        });
    }
}
