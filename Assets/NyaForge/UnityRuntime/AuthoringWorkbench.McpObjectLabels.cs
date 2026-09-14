using NyaForge.Authoring;
using Newtonsoft.Json.Linq;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        JObject SetObjectLabelMcp(ObjectLabelRequest request)
        {
            try
            {
                var result = ObjectLabelService.Set(workspace, request);
                objectLabelsAttachmentHash = "";
                Refresh();
                SetStatus(string.IsNullOrEmpty(result.DisplayName) ? "AIから表示名を自動名へ戻しました。" : "AIから表示名を保存しました。Undoで戻せます。");
                return new JObject
                {
                    ["success"] = true,
                    ["code"] = "OK",
                    ["objectId"] = result.ObjectId,
                    ["displayName"] = result.DisplayName,
                    ["attachmentsHash"] = result.AttachmentsHash,
                    ["revision"] = result.Revision
                };
            }
            catch (AuthoringException error)
            {
                return new JObject { ["success"] = false, ["code"] = error.Code, ["message"] = error.Message };
            }
        }
    }
}
