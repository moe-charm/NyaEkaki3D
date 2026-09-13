using System.Linq;
using NyaForge.Authoring;
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
            if(request.Method=="export_glb") return ExportMcpGlb(request.GlbExport);
            if(request.Method=="save_project") return SaveMcpProject(request.Save);
            if(request.Method=="get_state")
            {
                var state=AuthoringReadService.Read(workspace,pipeInstance,request.Method); state["saveTarget"]=McpSaveTarget();
                var protectedIds = new JArray();
                foreach (var id in referenceProtectedObjectIds.OrderBy(id => id, System.StringComparer.Ordinal)) protectedIds.Add(id);
                state["referenceProtectedObjectIds"] = protectedIds;
                var deliveryIds = new JArray();
                foreach (var id in deliveryAllowedObjectIds.OrderBy(id => id, System.StringComparer.Ordinal)) deliveryIds.Add(id);
                state["deliveryAllowlistObjectIds"] = deliveryIds;
                state["deliveryAllowlistExplicit"] = deliveryAllowedObjectIds.Count > 0;
                state["activeObjectReferenceProtected"] = workspace != null && !workspace.Document.IsEmpty &&
                    referenceProtectedObjectIds.Contains(workspace.Document.ActiveObjectId);
                return state;
            }
            if(request.Method=="capture") return CaptureMcpEvidence();
            if(request.Method.StartsWith("secondary_motion_",System.StringComparison.Ordinal)) return DispatchSecondaryMotionMcp(request.Method,request.SecondaryCapture);
            if(request.Method!="apply" && request.Method!="import_image") return AuthoringReadService.Read(workspace,pipeInstance,request.Method);
            var command=request.Method=="import_image" ? NyaForge.Authoring.CommandWireReader.Read(request.ImageImport,PaintPngImporter.Read) : request.Command;
            var result=ExecuteMcpCommand(command);
            SetStatus(result.Success ? "AIの編集を反映しました。Undoで戻せます。" : "AIの編集を適用できませんでした："+result.Code);
            return new JObject
            {
                ["success"]=result.Success,["code"]=result.Code,["message"]=result.Message,
                ["revision"]=result.DocumentRevision,["meshContentHash"]=result.MeshContentHash,
                ["evaluationComplete"]=result.EvaluationComplete,["previewRevision"]=result.PreviewRevision
            };
        }

        // Keep the MCP command path's projection, selection and metadata refresh
        // identical to the GUI command path. In particular, history operations
        // restore ProjectAttachments after projection commit, so secondary-motion
        // state must be reread before the next status/playback action.
        internal CommandResult ExecuteMcpCommand(CommandEnvelope command)
        {
            if (ReferenceProtectionBlocks(command?.Operations))
                return new CommandResult { Success = false, Code = "REFERENCE_PROTECTED", Message = ReferenceProtectionMessage,
                    DocumentRevision = workspace.Document.DocumentRevision, MeshContentHash = null,
                    EvaluationComplete = workspace.Preview.IsComplete, PreviewRevision = workspace.Preview.OutputRevision };
            var result=commands.Execute(command,projection);
            // GUI Execute() refreshes metadata-backed secondary-motion state
            // after history operations. MCP bypasses that helper, so mirror the
            // refresh here; otherwise an MCP Undo/Redo can restore attachment
            // bytes while the status panel and playback owner keep the old asset.
            if (result.Success && command?.Operations?.Length == 1 &&
                (command.Operations[0].Kind == "history.undo" || command.Operations[0].Kind == "history.redo"))
            {
                RefreshSecondaryMotionAttachmentFromWorkspace();
                RefreshReferenceProtectionFromWorkspace();
                RefreshDeliveryAllowlistFromWorkspace();
            }
            selection.RemoveWhere(i=>i<0 || i>=projection.Points.Length);projection.Select(selection);Refresh();
            return result;
        }
    }
}

