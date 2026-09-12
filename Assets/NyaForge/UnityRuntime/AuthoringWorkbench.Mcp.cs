using NyaForge.Authoring.Inspection;
using UnityEngine.UIElements;
namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        Platform.AuthoringPipeServer authoringPipe;
        string pipeInstance;
        TextField mcpInstanceField;
        void BuildMcp(VisualElement parent)
        {
            var panel=new Foldout { text="AI接続（MCP）",value=false };parent.Add(panel);
            mcpInstanceField=new TextField("接続先instance ID") { isReadOnly=true };mcpInstanceField.style.flexDirection=FlexDirection.Column;panel.Add(mcpInstanceField);
            panel.Add(Button("この制作プロジェクトへの接続を開始",()=>Try(()=>
            {
                authoringPipe?.Dispose();pipeInstance=workspace.InstanceId;authoringPipe=new Platform.AuthoringPipeServer(pipeInstance);mcpInstanceField.value=pipeInstance;
            }),"mcp-start"));
            panel.Add(Button("接続を停止",StopMcp,"mcp-stop"));
            var help=new Label("状態・グラフ取得と一部の編集に対応。AIの編集はUndoで戻せます。文書を開き直すと接続を停止します。表示したIDをsidecarの --instance に指定してください。");help.style.whiteSpace=WhiteSpace.Normal;panel.Add(help);
        }
        void StopMcp() { authoringPipe?.Dispose();authoringPipe=null;if(mcpInstanceField!=null) mcpInstanceField.value=""; }
        void Update()
        {
            if(authoringPipe==null) return;
            if(workspace==null || workspace.InstanceId!=pipeInstance) { StopMcp();return; }
            if(authoringPipe.Failure!=null) { SetStatus("AI接続を停止しました："+authoringPipe.Failure);StopMcp();return; }
            authoringPipe.Pump(DispatchMcp);
        }
    }
}
