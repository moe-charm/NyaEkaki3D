using NyaForge.Authoring.Inspection;
using Newtonsoft.Json.Linq;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        JObject DispatchMcp(AuthoringIpcRequest request)
        {
            if(request.Method=="vertices_inspect") return request.Vertices.Read(workspace,pipeInstance);
            if(request.Method=="faces_inspect") return request.Faces.ReadFaces(workspace,pipeInstance);
            if(request.Method=="validate") return AuthoringValidationReader.Read(workspace,pipeInstance,request.Validation);
            if(request.Method=="export") return ExportMcpProject(request.Export);
            if(request.Method=="save_project") return SaveMcpProject(request.Save);
            if(request.Method=="get_state") { var state=AuthoringReadService.Read(workspace,pipeInstance,request.Method);state["saveTarget"]=McpSaveTarget();return state; }
            if(request.Method=="capture") return CaptureMcpEvidence();
            if(request.Method.StartsWith("secondary_motion_",System.StringComparison.Ordinal)) return DispatchSecondaryMotionMcp(request.Method,request.SecondaryCapture);
            if(request.Method!="apply" && request.Method!="import_image") return AuthoringReadService.Read(workspace,pipeInstance,request.Method);
            var command=request.Method=="import_image" ? NyaForge.Authoring.CommandWireReader.Read(request.ImageImport,PaintPngImporter.Read) : request.Command;
            var result=commands.Execute(command,projection);
            selection.RemoveWhere(i=>i<0 || i>=projection.Points.Length);projection.Select(selection);Refresh();
            SetStatus(result.Success ? "AIの編集を反映しました。Undoで戻せます。" : "AIの編集を適用できませんでした："+result.Code);
            return new JObject
            {
                ["success"]=result.Success,["code"]=result.Code,["message"]=result.Message,
                ["revision"]=result.DocumentRevision,["meshContentHash"]=result.MeshContentHash,
                ["evaluationComplete"]=result.EvaluationComplete,["previewRevision"]=result.PreviewRevision
            };
        }
    }
}

