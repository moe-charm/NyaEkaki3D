using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Inspection;
internal static partial class Program
{
    static void RunAuthoringStateTests()
    {
        Test("vertex inspection pages pin revision and input snapshot",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();string source,edit;var graph=PlaneGraph(out source,out edit);
            Ok(Execute(w,AuthoringOperation.AddGraph(graph)));
            var value=w.Preview.Evaluation.MeshInputs[edit];var revision=w.Document.DocumentRevision;
            Newtonsoft.Json.Linq.JObject Read(int offset,int count,string hash=null)=>AuthoringVertexReader.Read(w,w.InstanceId,w.Document.DocumentId,revision,edit,true,hash??value.SnapshotHash,offset,count);
            var first=Read(0,2);var second=Read(2,2);
            Equal(2,(int)first["nextOffset"]);Equal(4,(int)first["total"]);
            Equal(0,(int)first["vertices"][0]["id"]);Equal(2,(int)second["vertices"][0]["id"]);
            True(second["nextOffset"].Type==Newtonsoft.Json.Linq.JTokenType.Null);
            Equal(value.Mesh.Positions[0].X,(float)first["vertices"][0]["restPosition"][0]);
            Equal(0,Read(4,2)["vertices"].Count());
            Expect("INVALID_PAGE",()=>Read(5,1));Expect("INVALID_PAGE",()=>Read(0,1025));
            Expect("SNAPSHOT_CHANGED",()=>Read(0,1,"old"));
            Ok(Execute(w,AuthoringOperation.Disconnect(edit,"mesh")));
            Expect("REVISION_CONFLICT",()=>Read(0,1));
            Expect("MESH_UNAVAILABLE",()=>AuthoringVertexReader.Read(w,w.InstanceId,w.Document.DocumentId,w.Document.DocumentRevision,edit,true,value.SnapshotHash,0,1));
            Equal(2,first["vertices"].Count());
        });
        Test("vertex page wire rejects coercion and unbounded ranges",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();
            var page=new Newtonsoft.Json.Linq.JObject { ["documentId"]=w.Document.DocumentId,["revision"]=0,["nodeId"]=System.Guid.NewGuid().ToString("D"),["port"]="input",["snapshotHash"]="observed",["offset"]=0,["count"]=2 };
            var envelope=new Newtonsoft.Json.Linq.JObject { ["version"]=1,["requestId"]=System.Guid.NewGuid().ToString("D"),["expectedInstanceId"]=w.InstanceId,["method"]="vertices_inspect",["vertices"]=page };
            var parsed=AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(envelope.ToString()));
            Equal(2,parsed.Vertices.Count);True(parsed.Vertices.Input);
            var faceEnvelope=(Newtonsoft.Json.Linq.JObject)envelope.DeepClone();
            faceEnvelope["method"]="faces_inspect";faceEnvelope["faces"]=faceEnvelope["vertices"].DeepClone();faceEnvelope.Remove("vertices");
            Equal(2,AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(faceEnvelope.ToString())).Faces.Count);
            faceEnvelope["faces"]["count"]=65;
            Expect("INVALID_PAGE",()=>AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(faceEnvelope.ToString())));
            page["count"]="2";Expect("INVALID_REQUEST",()=>MeshPageRequest.Parse(page));
            page["count"]=1025;Expect("INVALID_PAGE",()=>MeshPageRequest.Parse(page));
            page["count"]=2;page["revision"]=Newtonsoft.Json.Linq.JToken.Parse("999999999999999999999999999");Expect("INVALID_PAGE",()=>MeshPageRequest.Parse(page));
            page["revision"]=0;page["port"]="anything";Expect("INVALID_REQUEST",()=>MeshPageRequest.Parse(page));
        });
        Test("graph inspection reports current partial evaluation without stale output",()=>
        {
            var empty=AuthoringWorkspace.CreateEmpty();True(AuthoringGraphReader.Read(empty,empty.InstanceId)["graph"].Type==Newtonsoft.Json.Linq.JTokenType.Null);
            string source,edit;var graph=PlaneGraph(out source,out edit);Ok(Execute(empty,AuthoringOperation.AddGraph(graph)));
            var before=AuthoringGraphReader.Read(empty,empty.InstanceId);Equal(graph.Nodes.Count,before["graph"]["nodes"].Count());
            Equal(empty.Attachments.ContentHash,(string)before["attachmentsHash"]);
            Ok(Execute(empty,AuthoringOperation.Disconnect(edit,"mesh")));
            var after=AuthoringGraphReader.Read(empty,empty.InstanceId);True(!(bool)after["graph"]["evaluationComplete"]);True(after["graph"]["diagnostics"].Count()>0);
            var node=after["graph"]["nodes"].Single(n=>(string)n["nodeId"]==edit);True(node["meshOutput"].Type==Newtonsoft.Json.Linq.JTokenType.Null);
            True((bool)before["graph"]["evaluationComplete"]);Expect("STALE_INSTANCE",()=>AuthoringGraphReader.Read(empty,"old"));
        });
        Test("graph inspection lists every object while keeping active graph detail", () =>
        {
            var workspace = AuthoringWorkspace.CreateEmpty("inspect objects"); var commands = new AuthoringCommandService(workspace);
            string firstSource, firstEdit; Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(PlaneGraph(out firstSource, out firstEdit)))));
            string firstId = workspace.Document.ObjectId; string secondSource, secondEdit; var secondGraph = PlaneGraph(out secondSource, out secondEdit); Ok(commands.Execute(workspace.NewCommand(AuthoringOperation.AddGraph(secondGraph, System.Guid.NewGuid().ToString("D")))));
            var inspected = AuthoringGraphReader.Read(workspace, workspace.InstanceId);
            Equal(2, inspected["objects"].Count()); Equal((string)workspace.Document.ActiveObjectId, (string)inspected["activeObjectId"]);
            Equal(1, inspected["objects"].Count(item => (bool)item["active"])); True(inspected["objects"].All(item => item["graphId"].Type == Newtonsoft.Json.Linq.JTokenType.String));
            Equal(workspace.Document.ActiveObject.Graph.GraphId, (string)inspected["graph"]["graphId"]); True(inspected["objects"].All(item => item["diagnostics"] != null));
        });
        Test("read protocol rejects coercion and capabilities derive from registry",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();var cap=AuthoringReadService.Read(w,w.InstanceId,"capabilities");
            Equal(NyaForge.Authoring.Graph.BuiltinNodes.Definitions.Count,cap["nodeDefinitions"].Count());True((bool)cap["remoteEditing"]);True(cap["remoteMethods"].Values<string>().Contains("surface_fit_inspect"));
            var request=new Newtonsoft.Json.Linq.JObject { ["version"]=1,["requestId"]=System.Guid.NewGuid().ToString("D"),["expectedInstanceId"]=w.InstanceId,["method"]="capabilities" };
            var valid=System.Text.Encoding.UTF8.GetBytes(request.ToString());Equal("capabilities",AuthoringIpcRequest.Parse(valid).Method);
            request["method"]="surface_fit_inspect";Equal("surface_fit_inspect",AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(request.ToString())).Method);
            request["method"]="capabilities";
            request["version"]="1";Expect("INVALID_REQUEST",()=>AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(request.ToString())));
            request["version"]=1;request["extra"]=true;Expect("INVALID_REQUEST",()=>AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(request.ToString())));
            Expect("UNSUPPORTED_METHOD",()=>AuthoringReadService.Read(w,w.InstanceId,"execute_code"));
            request.Remove("extra");
            foreach(var method in new Newtonsoft.Json.Linq.JToken[]{Newtonsoft.Json.Linq.JValue.CreateNull(),new Newtonsoft.Json.Linq.JObject(),new Newtonsoft.Json.Linq.JArray(),new Newtonsoft.Json.Linq.JValue(1)})
            {
                request["method"]=method;
                Expect("INVALID_REQUEST",()=>AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(request.ToString())));
            }
        });
        Test("state inspection is detached and rejects another instance",()=>
        {
            var w=AuthoringWorkspace.CreateFixture();var before=AuthoringStateReader.Read(w,w.InstanceId);
            Equal(1,before["objects"].Count());Equal(w.Document.StateHash,(string)before["stateHash"]);
            Equal(w.Attachments.ContentHash,(string)before["attachmentsHash"]);
            var metadata = new ProjectAttachments(new System.Collections.Generic.Dictionary<string, byte[]> {
                [ProjectAttachments.Rig] = new byte[] { 1, 2, 3 }
            });
            w.SetAttachments(metadata);
            var withMetadata = AuthoringStateReader.Read(w,w.InstanceId);
            Equal(metadata.ContentHash,(string)withMetadata["attachmentsHash"]);
            var labelBytes = ObjectLabelsCodec.Write(new System.Collections.Generic.Dictionary<string, string> {
                [w.Document.ActiveObjectId] = "確認用ボディ"
            });
            w.SetAttachments(new ProjectAttachments(new System.Collections.Generic.Dictionary<string, byte[]> {
                [ProjectAttachments.ObjectLabels] = labelBytes
            }));
            var withLabel = AuthoringStateReader.Read(w, w.InstanceId);
            Equal("確認用ボディ", (string)withLabel["objects"][0]["displayName"]);
            Ok(Execute(w,AuthoringOperation.TranslateVertices(new[]{0,4},new Vec3(.03f,0,0))));
            var after=AuthoringStateReader.Read(w,w.InstanceId);
            True((long)after["revision"]>(long)before["revision"]);True((bool)after["canUndo"]);
            before["name"]="changed externally";True(w.Document.Name!="changed externally");
            Expect("STALE_INSTANCE",()=>AuthoringStateReader.Read(w,"other"));
            var empty=AuthoringWorkspace.CreateEmpty();Equal(0,AuthoringStateReader.Read(empty,empty.InstanceId)["objects"].Count());
        });
    }
}

