using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Inspection;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunPaintWireTests()
    {
        Test("paint wire uses observed image identity and preserves replay save undo",()=>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(f.graph)));
            var inspected=AuthoringGraphReader.Read(w,w.InstanceId);
            var node=inspected["graph"]["nodes"].Single(n=>(string)n["nodeId"]==f.paint);
            string before=w.Document.StateHash;
            var op=new JObject {["kind"]="paint.stroke",["paintContext"]=node["paintContext"].DeepClone(),["points"]=new JArray(new JArray(.2,.5),new JArray(.8,.5)),["radius"]=4,["color"]=new JArray(255,0,0,255)};
            var wire=Wire(w,op);var service=new AuthoringCommandService(w);
            Ok(service.Execute(CommandWireReader.Read(wire)));string after=w.Document.StateHash;True(after!=before);
            Equal((byte)0,w.Preview.Output.BaseColor.Image.GetPixel(32,32).G);
            long revision=w.Document.DocumentRevision;Ok(service.Execute(CommandWireReader.Read(wire)));Equal(revision,w.Document.DocumentRevision);
            Code("PAINT_CONTEXT_STALE",service.Execute(CommandWireReader.Read(Wire(w,op))));
            Ok(Execute(w,AuthoringOperation.Undo()));Equal(before,w.Document.StateHash);Ok(Execute(w,AuthoringOperation.Redo()));Equal(after,w.Document.StateHash);
            string dir=Dir("wire-paint");ProjectStore.Save(dir,w,0);Equal(w.Preview.Output.BaseColor.ImageHash,ProjectStore.Open(dir).Preview.Output.BaseColor.ImageHash);
            op["color"][0]=256;Expect("INVALID_COMMAND_WIRE",()=>CommandWireReader.Read(Wire(w,op)));
            var add=Wire(w,new JObject {["kind"]="graph.node.add",["node"]=new JObject {["nodeId"]=GraphId(),["typeId"]="image.paint",["version"]=1,["parameters"]=new JObject {["width"]=64,["height"]=32}}});
            Equal(64,CommandWireReader.Read(add).Operations[0].Node.PaintWidth);
        });
    }
}
