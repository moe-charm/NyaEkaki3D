using UnityEngine.UIElements;

namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Platform.AuthoringPipeServer authoringPipe;
        string pipeInstance;
        TextField mcpInstanceField;

        /// <summary>Presentation and lifecycle controls for the MCP connection.</summary>
        void BuildMcp(VisualElement parent)
        {
            var panel = new Foldout { text = "AI接続（MCP）", value = false, name = "mcp-panel" };
            panel.AddToClassList("mcp-panel");
            mcpInstanceField = new TextField("接続先instance ID") { isReadOnly = true, name = "mcp-instance-id" };
            mcpInstanceField.style.flexDirection = FlexDirection.Column;
            mcpInstanceField.style.width = Length.Percent(100);
            mcpInstanceField.style.minWidth = 0;
            panel.Add(mcpInstanceField);
            var start = Button("この制作プロジェクトへの接続を開始", () => Try(() =>
            {
                authoringPipe?.Dispose();
                pipeInstance = workspace.InstanceId;
                authoringPipe = new Platform.AuthoringPipeServer(pipeInstance);
                mcpInstanceField.value = pipeInstance;
            }), "mcp-start");
            start.AddToClassList("mcp-action");
            panel.Add(start);
            var stop = Button("接続を停止", StopMcp, "mcp-stop");
            stop.AddToClassList("mcp-action");
            panel.Add(stop);
            var help = new Label("状態・グラフ取得と一部の編集に対応。AIの編集はUndoで戻せます。文書を開き直すと接続を停止します。表示したIDをsidecarの --instance に指定してください。")
            {
                name = "mcp-help"
            };
            help.AddToClassList("mcp-help");
            help.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(help);
            parent.Add(panel);
        }

        void StopMcp()
        {
            authoringPipe?.Dispose();
            authoringPipe = null;
            if (mcpInstanceField != null) mcpInstanceField.value = "";
        }
    }
}
