namespace Gnosis.Graphic.RHI;

/// <summary>
/// 管线阶段标志
/// </summary>
[Flags]
public enum PipelineStageFlag
{
    None = 0,
    TopOfPipe = 1 << 0,
    DrawIndirect = 1 << 1,
    VertexInput = 1 << 2,
    VertexShader = 1 << 3,
    FragmentShader = 1 << 4,
    EarlyFragmentTests = 1 << 5,
    LateFragmentTests = 1 << 6,
    ColorAttachmentOutput = 1 << 7,
    ComputeShader = 1 << 8,
    Transfer = 1 << 9,
    BottomOfPipe = 1 << 10,
    Host = 1 << 11,
    AllGraphics = 1 << 12,
    AllCommands = 1 << 13
}
