namespace Gnosis.Graphic.RHI;

/// <summary>
/// GPU 调试捕获接口，支持 RenderDoc 等图形调试工具的一键捕获
/// </summary>
public interface IDebugCapture : IDisposable
{
    /// <summary>
    /// 调试捕获工具是否可用
    /// </summary>
    bool IsAvailable { get; }

    /// <summary>
    /// 调试捕获工具名称（如 "RenderDoc"、"PIX"）
    /// </summary>
    string ToolName { get; }

    /// <summary>
    /// 当前连接的后端类型
    /// </summary>
    GraphicsBackend Backend { get; }

    /// <summary>
    /// 触发一帧捕获
    /// </summary>
    /// <returns>是否成功触发捕获</returns>
    bool TriggerCapture();

    /// <summary>
    /// 触发多帧捕获
    /// </summary>
    /// <param name="frameCount">捕获帧数</param>
    /// <returns>是否成功触发捕获</returns>
    bool TriggerMultiFrameCapture(int frameCount);

    /// <summary>
    /// 开始捕获范围（用于精确捕获特定渲染 Pass）
    /// </summary>
    void BeginCapture();

    /// <summary>
    /// 结束捕获范围
    /// </summary>
    void EndCapture();

    /// <summary>
    /// 是否正在捕获中
    /// </summary>
    bool IsCapturing { get; }

    /// <summary>
    /// 设置捕获文件输出路径
    /// </summary>
    /// <param name="filePath">输出文件路径（.rdc 格式）</param>
    void SetCaptureFilePath(string filePath);

    /// <summary>
    /// 获取最近一次捕获的文件路径
    /// </summary>
    string? LastCaptureFilePath { get; }

    /// <summary>
    /// 打开捕获文件在调试工具中查看
    /// </summary>
    /// <param name="filePath">捕获文件路径，为 null 则打开最近一次捕获</param>
    /// <returns>是否成功打开</returns>
    bool OpenCapture(string? filePath = null);

    /// <summary>
    /// 启用/禁用 API 调用追踪
    /// </summary>
    bool ApiCallTracing { get; set; }

    /// <summary>
    /// 启用/禁用着色器调试信息捕获
    /// </summary>
    bool ShaderDebugInfo { get; set; }

    /// <summary>
    /// 启用/禁用捕获时的验证层
    /// </summary>
    bool CaptureValidation { get; set; }
}

/// <summary>
/// 调试捕获事件参数
/// </summary>
public sealed class DebugCaptureEventArgs : EventArgs
{
    /// <summary>
    /// 捕获类型
    /// </summary>
    public DebugCaptureType CaptureType { get; }

    /// <summary>
    /// 是否成功
    /// </summary>
    public bool Success { get; }

    /// <summary>
    /// 捕获文件路径
    /// </summary>
    public string? FilePath { get; }

    /// <summary>
    /// 错误消息
    /// </summary>
    public string? ErrorMessage { get; }

    public DebugCaptureEventArgs(DebugCaptureType captureType, bool success, string? filePath = null, string? errorMessage = null)
    {
        CaptureType = captureType;
        Success = success;
        FilePath = filePath;
        ErrorMessage = errorMessage;
    }
}

/// <summary>
/// 调试捕获类型
/// </summary>
public enum DebugCaptureType
{
    /// <summary>
    /// 单帧捕获
    /// </summary>
    SingleFrame,

    /// <summary>
    /// 多帧捕获
    /// </summary>
    MultiFrame,

    /// <summary>
    /// 范围捕获（BeginCapture/EndCapture）
    /// </summary>
    RangeCapture
}
