using System;
using System.IO;
using Newtonsoft.Json.Linq;
using NyaForge.Authoring;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        JObject ExportMcpGlb(GlbExportRequest request)
        {
            try
            {
                string root = Path.GetFullPath(request.Directory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!string.Equals(root, McpSaveDirectory(), StringComparison.OrdinalIgnoreCase))
                    return new JObject { ["success"] = false, ["code"] = "EXPORT_TARGET_CHANGED" };
                string directory = Path.Combine(root, "exports", "glb-" + request.ExportId);
                if (Directory.Exists(directory) || File.Exists(directory))
                    return new JObject { ["success"] = false, ["code"] = "EXPORT_DESTINATION_EXISTS", ["directory"] = directory };
                GlbExportResult result;
                switch (request.Profile)
                {
                    case GlbExportProfile.StaticGeometry:
                        result = GlbExportService.ExportStatic(workspace, pipeInstance, request.DocumentId, request.ExpectedRevision, directory); break;
                    case GlbExportProfile.SkinnedGeometry:
                        result = GlbExportService.ExportSkinnedWithTransforms(workspace, pipeInstance, request.DocumentId, request.ExpectedRevision, directory, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices(), SkinnedJointLocalTransforms()); break;
                    case GlbExportProfile.SkinnedGeometryExtended:
                        result = GlbExportService.ExportSkinnedExtendedWithTransforms(workspace, pipeInstance, request.DocumentId, request.ExpectedRevision, directory, SkinnedNodeTransformsForExport(), SkinnedInverseBindMatrices(), SkinnedJointLocalTransforms()); break;
                    default: throw new AuthoringException("INVALID_GLB_EXPORT_REQUEST", "Unknown GLB export profile.");
                }
                SetStatus("AIから標準GLBを書き出しました：" + result.Path);
                return new JObject { ["success"] = true, ["code"] = "OK", ["glbPath"] = result.Path, ["reportPath"] = result.ReportPath, ["profile"] = result.Profile.ToString(), ["objectCount"] = result.ObjectCount, ["documentId"] = request.DocumentId, ["revision"] = request.ExpectedRevision, ["stateHash"] = workspace.Document.StateHash };
            }
            catch (AuthoringException error) { return new JObject { ["success"] = false, ["code"] = error.Code, ["message"] = error.Message }; }
        }
    }
}
