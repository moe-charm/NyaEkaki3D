using System.Linq;
using NyaForge.Authoring;
using NyaForge.Authoring.Inspection;
using Newtonsoft.Json.Linq;

internal static partial class Program
{
    static void RunLayerWireTests()
    {
        Test("layer wire migration preserves pixels and exposes current stack identity",()=>
        {
            var f=PaintFixture();var w=AuthoringWorkspace.CreateEmpty();Ok(Execute(w,AuthoringOperation.AddGraph(f.graph)));
            JObject Node()=>(JObject)AuthoringGraphReader.Read(w,w.InstanceId)["graph"]["nodes"].Single(n=>(string)n["nodeId"]==f.paint);
            var service=new AuthoringCommandService(w);string background=GraphId(),overlay=GraphId();string hash=w.Preview.Output.BaseColor.ImageHash;
            var migrate=Wire(w,new JObject {["kind"]="layers.migrate",["paintContext"]=Node()["paintContext"],["layerId"]=background});
            Ok(service.Execute(CommandWireReader.Read(migrate)));Ok(service.Execute(CommandWireReader.Read(migrate)));Equal(hash,w.Preview.Output.BaseColor.ImageHash);
            JObject Apply(string kind,params object[] fields)
            {
                var op=new JObject {["kind"]=kind,["layerContext"]=Node()["layerStack"]["context"].DeepClone()};
                for(int i=0;i<fields.Length;i+=2) op[(string)fields[i]]=JToken.FromObject(fields[i+1]);
                Ok(service.Execute(CommandWireReader.Read(Wire(w,op))));return op;
            }
            var add=Apply("layers.add","layerId",overlay,"name","overlay","width",64,"height",64,"index",1);
            Equal(hash,w.Preview.Output.BaseColor.ImageHash);Equal(2,Node()["layerStack"]["layers"].Count());
            Code("PAINT_CONTEXT_STALE",service.Execute(CommandWireReader.Read(Wire(w,add))));
            Apply("layers.stroke","layerId",overlay,"points",new[]{new[]{.5,.5}},"radius",4,"color",new[]{0,255,0,255});
            Equal((byte)0,w.Preview.Output.BaseColor.Image.GetPixel(32,32).R);
            Apply("layers.mask.fill","layerId",overlay,"width",64,"height",64,"target",255);
            True((bool)Node()["layerStack"]["layers"][1]["hasMask"]);
            Apply("layers.mask.stroke","layerId",overlay,"points",new[]{new[]{.5,.5}},"radius",4,"target",0,"strength",1);
            Equal((byte)255,w.Preview.Output.BaseColor.Image.GetPixel(32,32).R);
            Apply("layers.mask.clear","layerId",overlay);Equal((byte)0,w.Preview.Output.BaseColor.Image.GetPixel(32,32).R);
            Ok(Execute(w,AuthoringOperation.Undo()));Equal((byte)255,w.Preview.Output.BaseColor.Image.GetPixel(32,32).R);
            Ok(Execute(w,AuthoringOperation.Redo()));Equal((byte)0,w.Preview.Output.BaseColor.Image.GetPixel(32,32).R);
            Apply("layers.appearance","layerId",background,"opacity",.5,"visible",false);
            Equal((byte)0,w.Preview.Output.BaseColor.Image.GetPixel(0,0).A);
            Apply("layers.rename","layerId",overlay,"name","linework");
            Apply("layers.move","layerId",overlay,"index",0);Equal(overlay,(string)Node()["layerStack"]["layers"][0]["id"]);
            Apply("layers.remove","layerId",overlay);Equal(1,Node()["layerStack"]["layers"].Count());
            Ok(Execute(w,AuthoringOperation.Undo()));Equal(2,Node()["layerStack"]["layers"].Count());
            string dir=Dir("wire-layers");ProjectStore.Save(dir,w,0);Equal(w.Document.StateHash,ProjectStore.Open(dir).Document.StateHash);
            var source=new NyaForge.Authoring.Paint.PaintImage(8,4,new NyaForge.Authoring.Paint.Rgba32(0,0,255));
            var bytes=NyaForge.Authoring.Paint.PaintPng.Encode(source);string sourceHash=Checks.Hash(bytes),importId=GraphId();
            var import=Wire(w,new JObject {["kind"]="layers.import",["layerContext"]=Node()["layerStack"]["context"],["layerId"]=importId,["name"]="png",["width"]=64,["height"]=64,["index"]=2,["path"]=System.IO.Path.Combine(Root,"input.png"),["sourceHash"]=sourceHash,["fit"]=true});
            var request=new JObject {["version"]=1,["requestId"]=GraphId(),["expectedInstanceId"]=w.InstanceId,["method"]="import_image",["command"]=import};
            var parsed=AuthoringIpcRequest.Parse(System.Text.Encoding.UTF8.GetBytes(request.ToString()));
            NyaForge.Authoring.Paint.PaintImage Load(string path,string hash) { Equal(sourceHash,hash);NyaForge.Authoring.Paint.PaintPngInput.RequireSourceHash(bytes,hash);return source; }
            Ok(service.Execute(CommandWireReader.Read(parsed.ImageImport,Load)));long revision=w.Document.DocumentRevision;
            Ok(service.Execute(CommandWireReader.Read(parsed.ImageImport,Load)));Equal(revision,w.Document.DocumentRevision);
            Equal(importId,(string)Node()["layerStack"]["layers"][2]["id"]);
            bytes[bytes.Length-1]^=1;Expect("IMPORT_SOURCE_CHANGED",()=>CommandWireReader.Read(parsed.ImageImport,Load));Equal(revision,w.Document.DocumentRevision);
        });
    }
}
