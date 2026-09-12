using System;
using System.IO;
using NyaForge.Authoring;
using Newtonsoft.Json.Linq;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        JObject ExportMcpProject(ProjectExportRequest request)
        {
            try
            {
                string root=Path.GetFullPath(request.Directory).TrimEnd(Path.DirectorySeparatorChar,Path.AltDirectorySeparatorChar);
                if(!string.Equals(root,McpSaveDirectory(),StringComparison.OrdinalIgnoreCase)) return new JObject { ["success"]=false,["code"]="EXPORT_TARGET_CHANGED" };
                string directory=Path.Combine(root,"exports",request.ExportId);
                if(Directory.Exists(directory) || File.Exists(directory)) return new JObject { ["success"]=false,["code"]="EXPORT_DESTINATION_EXISTS",["directory"]=directory };
                var result=ProjectExportService.Export(workspace,pipeInstance,request.DocumentId,request.ExpectedRevision,directory);
                SetStatus("AIから書き出しました："+result.ManifestPath);
                return new JObject { ["success"]=true,["code"]="OK",["manifestPath"]=result.ManifestPath,["kind"]=result.Kind.ToString(),["documentId"]=result.DocumentId,["revision"]=result.Revision,["stateHash"]=result.StateHash };
            }
            catch(AuthoringException e) { return new JObject { ["success"]=false,["code"]=e.Code,["message"]=e.Message }; }
        }
    }
}
