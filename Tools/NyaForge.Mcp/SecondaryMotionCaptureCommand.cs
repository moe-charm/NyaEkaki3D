namespace NyaForge.Mcp;

public sealed class SecondaryMotionCaptureCommand
{
    public int frameCount { get; init; } = 3;
    public int warmupSteps { get; init; } = 10;
    public int width { get; init; } = 256;
    public int height { get; init; } = 256;
}
