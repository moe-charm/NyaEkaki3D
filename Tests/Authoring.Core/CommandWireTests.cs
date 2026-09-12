using System;
using NyaForge.Authoring;
using Newtonsoft.Json.Linq;
internal static partial class Program
{
    static JObject Wire(AuthoringWorkspace w,JObject operation)
    {
        var c=w.NewCommand();
        return new JObject { ["expectedInstanceId"]=c.ExpectedInstanceId,["documentId"]=c.DocumentId,["expectedDocumentRevision"]=c.ExpectedDocumentRevision,["commandId"]=c.CommandId,["objectId"]=c.ObjectId,["expectedBaselineHash"]=c.ExpectedBaselineHash,["operations"]=new JArray(operation) };
    }
    static void RunCommandWireTests()
    {
        Test("wire material preserves explicit parameters and rejects invalid values",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();var p=new JObject { ["baseColor"]=new JArray(.8,.15,.3,1),["metallic"]=.25,["roughness"]=.6,["emission"]=new JArray(0,0,0),["alphaMode"]="Blend",["alphaCutoff"]=.5 };
            var wire=Wire(w,new JObject { ["kind"]="graph.node.add",["node"]=new JObject { ["nodeId"]=Guid.NewGuid().ToString("D"),["typeId"]="material.standard",["version"]=1,["parameters"]=p } });
            var material=CommandWireReader.Read(wire).Operations[0].Node.Material;Equal(.8f,material.BaseColor.X);Equal(NyaForge.Authoring.Graph.MaterialAlphaMode.Blend,material.AlphaMode);
            var graphWire=Wire(w,new JObject { ["kind"]="object.add_graph",["newObjectId"]=Guid.NewGuid().ToString("D"),["graph"]=new JObject { ["graphId"]=Guid.NewGuid().ToString("D"),["outputNodeId"]="",["nodes"]=new JArray(wire["operations"][0]["node"].DeepClone()),["edges"]=new JArray() } });
            var ipc=new JObject { ["version"]=1,["requestId"]=Guid.NewGuid().ToString("D"),["expectedInstanceId"]=w.InstanceId,["method"]="apply",["command"]=graphWire };
            True(NyaForge.Authoring.Inspection.AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(ipc.ToString())).Command!=null);
            p["metallic"]=2;Expect("INVALID_MATERIAL",()=>CommandWireReader.Read(wire));p["metallic"]=.25;p["alphaMode"]="1";Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(wire));
        });
        Test("wire command matches direct edit and preserves replay revision contract",()=>
        {
            var w=AuthoringWorkspace.CreateFixture();var direct=AuthoringWorkspace.CreateFixture();
            var input=Wire(w,new JObject { ["kind"]="vertices.translate",["vertexIds"]=new JArray(0,4),["delta"]=new JArray(.01,0,0) });
            var service=new AuthoringCommandService(w);var first=service.Execute(CommandWireReader.Read(input));Ok(first);
            Ok(Execute(direct,AuthoringOperation.TranslateVertices(new[]{0,4},new Vec3(.01f,0,0))));Equal(direct.Evaluate().ContentHash,w.Evaluate().ContentHash);
            var repeat=service.Execute(CommandWireReader.Read(input));Ok(repeat);Equal(first.DocumentRevision,repeat.DocumentRevision);Equal(1L,w.Document.DocumentRevision);
            input["commandId"]=Guid.NewGuid().ToString("D");Equal("REVISION_CONFLICT",service.Execute(CommandWireReader.Read(input)).Code);
            var undo=Wire(w,new JObject { ["kind"]="history.undo" });Ok(service.Execute(CommandWireReader.Read(undo)));Equal(AuthoringWorkspace.CreateFixture().Evaluate().ContentHash,w.Evaluate().ContentHash);
        });
        Test("wire operations enforce explicit types and reject unknown payloads",()=>
        {
            var w=AuthoringWorkspace.CreateFixture();var input=Wire(w,new JObject { ["kind"]="history.undo",["unexpected"]=true });
            Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(input));
            input["operations"]=new JArray(new JObject { ["kind"]="execute_code" });Expect("UNSUPPORTED_OPERATION",()=>CommandWireReader.Read(input));
            input["operations"]=new JArray(new JObject { ["kind"]="history.undo" });input["expectedDocumentRevision"]="0";Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(input));
        });
    }
}
