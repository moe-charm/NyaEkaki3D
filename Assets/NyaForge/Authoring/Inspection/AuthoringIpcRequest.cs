using System;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace NyaForge.Authoring.Inspection
{
    public sealed class AuthoringIpcRequest
    {
        public string RequestId { get; private set; }
        public string ExpectedInstanceId { get; private set; }
        public string Method { get; private set; }
        public CommandEnvelope Command { get; private set; }
        public JObject ImageImport { get; private set; }
        public ProjectSaveRequest Save { get; private set; }
        public ProjectExportRequest Export { get; private set; }
        public GlbExportRequest GlbExport { get; private set; }
        public AuthoringValidationRequest Validation { get; private set; }
        public MeshPageRequest Vertices { get; private set; }
        public MeshPageRequest Faces { get; private set; }
        public SecondaryMotionCaptureRequest SecondaryCapture { get; private set; }
        public static AuthoringIpcRequest Parse(byte[] bytes)
        {
            Checks.Require(bytes!=null && bytes.Length>0 && bytes.Length<=65536,"INVALID_REQUEST","Invalid request size.");
            JObject r;
            using(var reader=new JsonTextReader(new StringReader(new UTF8Encoding(false,true).GetString(bytes))) { MaxDepth=12,DateParseHandling=DateParseHandling.None })
            {
                r=JObject.Load(reader,new JsonLoadSettings { DuplicatePropertyNameHandling=DuplicatePropertyNameHandling.Error });
                Checks.Require(!reader.Read(),"INVALID_REQUEST","Trailing JSON.");
            }
            Checks.Require(r["method"]?.Type==JTokenType.String,"INVALID_REQUEST","Expected string method.");
            bool apply=(string)r["method"]=="apply";
            bool import=(string)r["method"]=="import_image";
            bool save=(string)r["method"]=="save_project";
            bool export=(string)r["method"]=="export";
            bool glbExport=(string)r["method"]=="export_glb";
            bool validate=(string)r["method"]=="validate";
            bool secondaryCapture=(string)r["method"]=="secondary_motion_capture";
            var fields=apply ? new[]{"command","expectedInstanceId","method","requestId","version"} : save ? new[]{"expectedInstanceId","method","requestId","save","version"} : validate ? new[]{"expectedInstanceId","method","requestId","validation","version"} : new[]{"expectedInstanceId","method","requestId","version"};
            if(export) fields=new[]{"expectedInstanceId","export","method","requestId","version"};
            if(glbExport) fields=new[]{"expectedInstanceId","export","method","requestId","version"};
            bool vertices=(string)r["method"]=="vertices_inspect";
            if(vertices) fields=new[]{"expectedInstanceId","method","requestId","version","vertices"};
            bool faces=(string)r["method"]=="faces_inspect";
            if(import) fields=new[]{"command","expectedInstanceId","method","requestId","version"};
            if(faces) fields=new[]{"expectedInstanceId","faces","method","requestId","version"};
            if(secondaryCapture) fields=new[]{"capture","expectedInstanceId","method","requestId","version"};
            Checks.Require(r.Properties().Select(p=>p.Name).OrderBy(n=>n,StringComparer.Ordinal).SequenceEqual(fields),"INVALID_REQUEST","Unexpected request fields.");
            Checks.Require(r["version"].Type==JTokenType.Integer && r["version"].ToString()=="1","INVALID_REQUEST","Unsupported request version.");
            foreach(string field in new[]{"requestId","expectedInstanceId","method"}) Checks.Require(r[field].Type==JTokenType.String,"INVALID_REQUEST","Expected string: "+field);
            string id=(string)r["requestId"],instance=(string)r["expectedInstanceId"],method=(string)r["method"];
            Checks.Require(Guid.TryParseExact(id,"D",out _) && Guid.TryParseExact(instance,"D",out _) && method.Length>0 && method.Length<=64,"INVALID_REQUEST","Invalid request identity or method.");
            var imageImport=import ? r["command"] as JObject : null;
            if(import) Checks.Require(imageImport!=null && imageImport["expectedInstanceId"]?.Type==JTokenType.String && (string)imageImport["expectedInstanceId"]==instance && imageImport["operations"] is JArray ops && ops.Count==1 && ops[0] is JObject && ops[0]["kind"]?.Type==JTokenType.String && (string)ops[0]["kind"]=="layers.import","INVALID_REQUEST","Import requires one layers.import operation for this instance.");
            var command=apply ? CommandWireReader.Read(r["command"] as JObject) : null;
            Checks.Require(command==null || command.ExpectedInstanceId==instance,"INVALID_REQUEST","Command instance differs from request.");
            return new AuthoringIpcRequest { ImageImport=imageImport,RequestId=id,ExpectedInstanceId=instance,Method=method,Command=command,Save=save ? ProjectSaveWireReader.Read(r["save"] as JObject,instance) : null,Export=export ? ProjectExportRequest.Read(r["export"] as JObject) : null,GlbExport=glbExport ? GlbExportRequest.Read(r["export"] as JObject) : null,Validation=validate ? AuthoringValidationRequest.Read(r["validation"] as JObject) : null,Vertices=vertices ? MeshPageRequest.Parse(r["vertices"] as JObject) : null,Faces=faces ? MeshPageRequest.Parse(r["faces"] as JObject,true) : null,SecondaryCapture=secondaryCapture ? SecondaryMotionCaptureRequest.Read(r["capture"] as JObject) : null };
        }
    }
}


