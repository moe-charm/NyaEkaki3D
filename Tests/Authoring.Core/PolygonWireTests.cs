using System;
using NyaForge.Authoring;
using NyaForge.Authoring.Graph;
using NyaForge.Authoring.Inspection;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunPolygonWireTests()
    {
        Test("wire builds first face from empty polygon and adds thickness and UV",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();string source=GraphId(),edit=GraphId(),output=GraphId();
            var polygon=new NyaForge.Authoring.Topology.PolygonMesh(GraphId(),Array.Empty<NyaForge.Authoring.Topology.CageVertex>(),Array.Empty<NyaForge.Authoring.Topology.CageFace>());
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            var inspected=AuthoringGraphReader.Read(w,w.InstanceId);
            var context=(JObject)System.Linq.Enumerable.Single(inspected["graph"]["nodes"],n=>(string)n["nodeId"]==edit)["editContext"];
            var service=new AuthoringCommandService(w);
            void Apply(JObject op) { op["context"]=context.DeepClone();Ok(service.Execute(CommandWireReader.Read(Wire(w,op)))); }
            foreach(var position in new[]{new JArray(0,0,0),new JArray(.1,0,0),new JArray(0,.1,0)}) Apply(new JObject {["kind"]="polygon.vertices.add",["position"]=position});
            var value=w.Preview.Evaluation.MeshOutputs[edit];
            var page=AuthoringVertexReader.Read(w,w.InstanceId,w.Document.DocumentId,w.Document.DocumentRevision,edit,false,value.SnapshotHash,0,10);
            var ids=new JArray(System.Linq.Enumerable.Select(page["vertices"],v=>v["id"].DeepClone()));
            Apply(new JObject {["kind"]="polygon.faces.create",["elementIds"]=ids,["materialSlot"]=0});Equal(1,w.Preview.Output.Polygon.Faces.Count);
            Apply(new JObject {["kind"]="polygon.solidify",["thickness"]=.01});True(w.Preview.Output.Polygon.Faces.Count>1);
            Apply(new JObject {["kind"]="polygon.uv.project"});True(System.Linq.Enumerable.All(w.Preview.Output.Polygon.Faces,f=>System.Linq.Enumerable.All(f.Corners,c=>c.Uv0.HasValue)));
            var bad=new JObject {["kind"]="polygon.solidify",["context"]=context,["thickness"]=".01"};Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(Wire(w,bad)));
            string dir=Dir("wire-created-solid");ProjectStore.Save(dir,w,0);Equal(w.Document.StateHash,ProjectStore.Open(dir).Document.StateHash);
        });
        Test("polygon wire edits use observed context and preserve replay undo",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();string source=GraphId(),edit=GraphId(),output=GraphId();
            var polygon=NyaForge.Authoring.Topology.PolygonPrimitives.Plane(GraphId());
            var graph=new AuthoringGraph(GraphId(),new[]{GraphNode.Polygon(source,polygon,new RestTransform(1,new Vec3())),GraphNode.PolygonEdit(edit),GraphNode.Output(output)},new[]{new GraphEdge(source,"mesh",edit,"mesh"),new GraphEdge(edit,"mesh",output,"mesh")},output);
            Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            var inspected=AuthoringGraphReader.Read(w,w.InstanceId);
            var context=(JObject)System.Linq.Enumerable.Single(inspected["graph"]["nodes"],n=>(string)n["nodeId"]==edit)["editContext"];
            string before=w.Document.StateHash;
            var op=new JObject {["kind"]="polygon.faces.extrude",["context"]=context,["elementIds"]=new JArray(polygon.Faces[0].Id.ToString()),["delta"]=new JArray(0,0,.05)};
            var wire=Wire(w,op);var service=new AuthoringCommandService(w);
            Ok(service.Execute(CommandWireReader.Read(wire)));string after=w.Document.StateHash;
            True(before!=after);True(w.Preview.Output.Polygon.Faces.Count>1);
            long revision=w.Document.DocumentRevision;Ok(service.Execute(CommandWireReader.Read(wire)));Equal(revision,w.Document.DocumentRevision);
            Ok(Execute(w,AuthoringOperation.Undo()));Equal(before,w.Document.StateHash);Ok(Execute(w,AuthoringOperation.Redo()));Equal(after,w.Document.StateHash);
            op["elementIds"]=new JArray("1","1");Expect("INVALID_SELECTION",()=>CommandWireReader.Read(Wire(w,op)));
            op["elementIds"]=new JArray(1);Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(Wire(w,op)));
            var move=new JObject {["kind"]="polygon.vertices.translate",["context"]=context,["elementIds"]=new JArray("1"),["delta"]=new JArray(.01,0,0)};
            Ok(service.Execute(CommandWireReader.Read(Wire(w,move))));
            var delete=new JObject {["kind"]="polygon.faces.delete",["context"]=context,["elementIds"]=new JArray(w.Preview.Output.Polygon.Faces[0].Id.ToString())};
            int count=w.Preview.Output.Polygon.Faces.Count;Ok(service.Execute(CommandWireReader.Read(Wire(w,delete))));Equal(count-1,w.Preview.Output.Polygon.Faces.Count);
        });
        Test("polygon command wire preserves explicit identities through IPC replay and inspection",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();string source=GraphId(),output=GraphId();
            var parameters=new JObject { ["domainId"]=GraphId(),["scale"]=1,["translation"]=new JArray(0,0,0),
                ["vertices"]=new JArray(new JObject {["id"]="11",["position"]=new JArray(0,0,0)},new JObject {["id"]="22",["position"]=new JArray(1,0,0)},new JObject {["id"]="33",["position"]=new JArray(0,1,0)}),
                ["faces"]=new JArray(new JObject {["id"]="9007199254740993",["materialSlot"]=0,["corners"]=new JArray(new JObject {["id"]="101",["vertexId"]="11"},new JObject {["id"]="102",["vertexId"]="22"},new JObject {["id"]="103",["vertexId"]="33"})}) };
            var graph=new JObject { ["graphId"]=GraphId(),["outputNodeId"]=output,
                ["nodes"]=new JArray(new JObject {["nodeId"]=source,["typeId"]=BuiltinNodes.PolygonSource,["version"]=1,["parameters"]=parameters},new JObject {["nodeId"]=output,["typeId"]=BuiltinNodes.Output,["version"]=1,["parameters"]=new JObject()}),
                ["edges"]=new JArray(new JObject {["fromNode"]=source,["fromPort"]="mesh",["toNode"]=output,["toPort"]="mesh"}) };
            var command=Wire(w,new JObject {["kind"]="object.add_graph",["newObjectId"]=GraphId(),["graph"]=graph});
            var envelope=new JObject {["version"]=1,["requestId"]=GraphId(),["expectedInstanceId"]=w.InstanceId,["method"]="apply",["command"]=command};
            CommandEnvelope Parse()=>AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(envelope.ToString())).Command;
            var service=new AuthoringCommandService(w);Ok(service.Execute(Parse()));Ok(service.Execute(Parse()));Equal(1L,w.Document.DocumentRevision);
            var value=w.Preview.Evaluation.MeshOutputs[source];
            var page=AuthoringFaceReader.Read(w,w.InstanceId,w.Document.DocumentId,1,source,false,value.SnapshotHash,0,1);
            Equal("9007199254740993",(string)page["faces"][0]["id"]);Equal(1,w.Evaluate().TriangleCount);
            parameters["vertices"][0]["id"]=11;Expect("INVALID_COMMAND_WIRE",()=>Parse());
            parameters["vertices"][0]["id"]="011";Expect("INVALID_COMMAND_WIRE",()=>Parse());
            parameters["vertices"][0]["id"]="11";parameters["faces"][0]["corners"][0]["vertexId"]="999";Expect("INVALID_ELEMENT_ID",()=>Parse());
        });
    }
}
