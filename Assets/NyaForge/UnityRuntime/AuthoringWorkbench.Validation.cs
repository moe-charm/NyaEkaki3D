using System;
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
                validationResult.text = "判定: " + status + "\n" + string.Join(" / ", checks.Select(FormatValidationCheck));
                SetStatus("出力チェック: " + status);
            });
        }

        static string FormatValidationCheck(JObject check)
        {
            string text = (string)check["name"] + ": " + (string)check["status"];
            return check["actual"] != null && check["limit"] != null
                ? text + " (" + (string)check["actual"] + "/" + (string)check["limit"] + ")"
                : text;
        }

        void RefreshValidation()
        {
            if (validateOutput == null) return;
            validateOutput.SetEnabled(workspace != null && workspace.Document != null && !workspace.IsExecuting);
        }
    }
}
