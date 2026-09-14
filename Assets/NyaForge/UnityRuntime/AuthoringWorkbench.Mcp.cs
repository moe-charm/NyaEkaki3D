namespace NyaForge.UnityRuntime
{
    public sealed partial class AuthoringWorkbench
    {
        void Update()
        {
            if (springAutomaticTick) TickSpringPlayback(UnityEngine.Time.unscaledDeltaTime);
            if (authoringPipe == null) return;
            if (workspace == null || workspace.InstanceId != pipeInstance) { StopMcp(); return; }
            if (authoringPipe.Failure != null) { SetStatus("AI接続を停止しました：" + authoringPipe.Failure); StopMcp(); return; }
            authoringPipe.Pump(DispatchMcp);
        }
    }
}
