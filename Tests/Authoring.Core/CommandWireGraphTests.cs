using System;
using NyaForge.Authoring;
using Newtonsoft.Json.Linq;
internal static partial class Program
{
    static void RunCommandWireGraphTests()
    {
        Test("wire graph creation retains explicit IDs across replay undo and redo",()=>
        {
            var w=AuthoringWorkspace.CreateEmpty();string obj=Guid.NewGuid().ToString("D"),graph=Guid.NewGuid().ToString("D"),plane=Guid.NewGuid().ToString("D"),output=Guid.NewGuid().ToString("D");
            var payload=new JObject
            {
                ["graphId"]=graph,["outputNodeId"]=output,
                ["nodes"]=new JArray(
                    new JObject { ["nodeId"]=plane,["typeId"]="primitive.plane",["version"]=1,["parameters"]=new JObject { ["width"]=.2,["height"]=.1 } },
                    new JObject { ["nodeId"]=output,["typeId"]="mesh.output",["version"]=1,["parameters"]=new JObject() }),
                ["edges"]=new JArray(new JObject { ["fromNode"]=plane,["fromPort"]="mesh",["toNode"]=output,["toPort"]="mesh" })
            };
            var wire=Wire(w,new JObject { ["kind"]="object.add_graph",["newObjectId"]=obj,["graph"]=payload });
            var service=new AuthoringCommandService(w);Ok(service.Execute(CommandWireReader.Read(wire)));string hash=w.Evaluate().ContentHash;
            Equal(obj,w.Document.ObjectId);Equal(graph,w.Document.Objects[0].Graph.GraphId);
            Ok(service.Execute(CommandWireReader.Read(wire)));Equal(1L,w.Document.DocumentRevision);
            var undo=Wire(w,new JObject { ["kind"]="history.undo" });Ok(service.Execute(CommandWireReader.Read(undo)));True(w.Document.IsEmpty);
            var redo=Wire(w,new JObject { ["kind"]="history.redo" });Ok(service.Execute(CommandWireReader.Read(redo)));Equal(obj,w.Document.ObjectId);Equal(hash,w.Evaluate().ContentHash);
            ((JObject)payload["nodes"][0])["version"]="1";Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(wire));
        });
    }
}
