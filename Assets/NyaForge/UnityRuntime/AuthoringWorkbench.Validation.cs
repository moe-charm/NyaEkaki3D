using System;
using System.Collections.Generic;
using System.Linq;
using NyaForge.Authoring.Inspection;
using Newtonsoft.Json.Linq;
using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Foldout validationPanel;
        DropdownField validationProfile;
        Button validateOutput;
        Label validationResult;

        void BuildValidation(VisualElement parent)
        {
            validationPanel = new Foldout { text = "出力チェック", value = false, name = "validation-panel" };
            parent.Add(validationPanel);
            validationProfile = new DropdownField("目安", new System.Collections.Generic.List<string> { "PC", "モバイル" }, 0) { name = "validation-profile" };
            validationProfile.style.flexDirection = FlexDirection.Column;
            validationPanel.Add(validationProfile);
            validateOutput = Button("現在の状態を検査", RunValidation, "validate-output");
            validationPanel.Add(validateOutput);
            validationResult = new Label("未検査") { name = "validation-result" };
            validationResult.style.whiteSpace = WhiteSpace.Normal;
            validationPanel.Add(validationResult);
        }

        void RunValidation()
        {
            Try(() =>
            {
                var request = AuthoringValidationRequest.Create(workspace.Document.DocumentId, workspace.Document.DocumentRevision,
                    validationProfile.index == 1 ? "mobile" : "pc");
                var result = AuthoringValidationReader.Read(workspace, workspace.InstanceId, request);
                string status = (string)result["status"];
                var checks = result["checks"]?.Values<JObject>() ?? Enumerable.Empty<JObject>();
                validationResult.text = FormatValidationResult(result, status, checks);
                SetStatus("出力チェック: " + status);
            });
        }

        static string FormatValidationResult(JObject result, string status, IEnumerable<JObject> checks)
        {
            var lines = new List<string> { "判定: " + status };
            var metrics = result["metrics"] as JObject;
            if (metrics != null)
            {
                string[] names = { "objects", "triangles", "renderVertices", "materials", "textures", "maxTextureDimension", "bones", "maxInfluences" };
                foreach (var name in names)
                    if (metrics[name] != null) lines.Add(name + ": " + (string)metrics[name]);
            }
            var objects = result["objects"]?.Values<JObject>().ToArray();
            if (objects != null && objects.Length > 0)
            {
                lines.Add("対象別:");
                foreach (var item in objects)
                {
                    string id = ((string)item["objectId"] ?? "");
                    if (id.Length > 8) id = id.Substring(0, 8);
                    string detail = item["status"]?.ToString() ?? "unknown";
                    if (item["triangles"] != null) detail += " · △" + item["triangles"] + " · 頂点" + item["renderVertices"];
                    lines.Add("・" + id + ": " + detail);
                }
            }
            foreach (var check in checks) lines.Add(FormatValidationCheck(check));
            var warnings = result["warnings"]?.Values<string>().Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            if (warnings != null && warnings.Length > 0)
            {
                lines.Add("警告:");
                lines.AddRange(warnings.Select(value => "・" + value));
            }
            return string.Join("\n", lines);
        }

        static string FormatValidationCheck(JObject check)
        {
            string text = (string)check["name"] + ": " + (string)check["status"];
            if (check["actual"] != null && check["limit"] != null)
                text += " (" + (string)check["actual"] + "/" + (string)check["limit"] + ")";
            return check["reason"] != null ? text + " — " + (string)check["reason"] : text;
        }

        void RefreshValidation()
        {
            if (validateOutput == null) return;
            validateOutput.SetEnabled(workspace != null && workspace.Document != null && !workspace.IsExecuting);
        }
    }
}
