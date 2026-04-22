using System.Runtime.InteropServices;
using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Capture;

/// <summary>
/// RenderDoc 调试捕获实现，通过 IDebugCapture 接口提供一键捕获功能
/// </summary>
public sealed class RenderDocCapture : IDebugCapture
{
    #region 字段

    private bool _isCapturing;
    private string? _captureFilePathTemplate;
    private string? _lastCaptureFilePath;
    private bool _apiCallTracing;
    private bool _shaderDebugInfo = true;
    private bool _captureValidation;
    private bool _disposed;

    #endregion

    #region 属性

    /// <inheritdoc/>
    public bool IsAvailable => RenderDocNative.IsAvailable;

    /// <inheritdoc/>
    public string ToolName => "RenderDoc";

    /// <inheritdoc/>
    public GraphicsBackend Backend { get; }

    /// <inheritdoc/>
    public bool IsCapturing => _isCapturing;

    /// <inheritdoc/>
    public string? LastCaptureFilePath => _lastCaptureFilePath;

    /// <inheritdoc/>
    public bool ApiCallTracing
    {
        get => _apiCallTracing;
        set
        {
            _apiCallTracing = value;

            if (!IsAvailable)
            {
                return;
            }

            if (value)
            {
                RenderDocNative.MaskOverlayBits(RenderDocNative.Overlay_All, RenderDocNative.Overlay_Enabled);
            }
            else
            {
                RenderDocNative.MaskOverlayBits(~RenderDocNative.Overlay_Enabled, 0);
            }
        }
    }

    /// <inheritdoc/>
    public bool ShaderDebugInfo
    {
        get => _shaderDebugInfo;
        set => _shaderDebugInfo = value;
    }

    /// <inheritdoc/>
    public bool CaptureValidation
    {
        get => _captureValidation;
        set => _captureValidation = value;
    }

    #endregion

    #region 事件

    /// <summary>
    /// 捕获完成事件
    /// </summary>
    public event EventHandler<DebugCaptureEventArgs>? CaptureCompleted;

    #endregion

    #region 构造

    /// <summary>
    /// 创建 RenderDoc 捕获实例
    /// </summary>
    /// <param name="backend">当前图形后端类型</param>
    public RenderDocCapture(GraphicsBackend backend)
    {
        Backend = backend;

        RenderDocNative.Initialize();

        if (IsAvailable)
        {
            ConfigureDefaults();
        }
    }

    /// <summary>
    /// 创建 RenderDoc 捕获实例，并关联图形设备
    /// </summary>
    /// <param name="backend">当前图形后端类型</param>
    /// <param name="deviceHandle">图形设备原生句柄</param>
    /// <param name="windowHandle">窗口原生句柄</param>
    public RenderDocCapture(GraphicsBackend backend, nint deviceHandle, nint windowHandle)
        : this(backend)
    {
        if (IsAvailable && windowHandle != nint.Zero)
        {
            RenderDocNative.SetActiveWindow(deviceHandle, windowHandle);
        }
    }

    #endregion

    #region IDebugCapture 实现

    /// <inheritdoc/>
    public bool TriggerCapture()
    {
        if (!IsAvailable)
        {
            OnCaptureCompleted(DebugCaptureType.SingleFrame, false, errorMessage: "RenderDoc 不可用，请确认已通过 RenderDoc 启动应用");
            return false;
        }

        if (!string.IsNullOrEmpty(_captureFilePathTemplate))
        {
            RenderDocNative.SetCaptureFilePathTemplate(_captureFilePathTemplate);
        }

        RenderDocNative.TriggerCapture();

        OnCaptureCompleted(DebugCaptureType.SingleFrame, true);
        return true;
    }

    /// <inheritdoc/>
    public bool TriggerMultiFrameCapture(int frameCount)
    {
        if (!IsAvailable)
        {
            OnCaptureCompleted(DebugCaptureType.MultiFrame, false, errorMessage: "RenderDoc 不可用");
            return false;
        }

        if (frameCount < 1)
        {
            frameCount = 1;
        }

        if (!string.IsNullOrEmpty(_captureFilePathTemplate))
        {
            RenderDocNative.SetCaptureFilePathTemplate(_captureFilePathTemplate);
        }

        RenderDocNative.TriggerMultiFrameCapture(frameCount);

        OnCaptureCompleted(DebugCaptureType.MultiFrame, true);
        return true;
    }

    /// <inheritdoc/>
    public void BeginCapture()
    {
        if (!IsAvailable || _isCapturing)
        {
            return;
        }

        _isCapturing = true;
        RenderDocNative.StartFrameCapture();
    }

    /// <inheritdoc/>
    public void EndCapture()
    {
        if (!IsAvailable || !_isCapturing)
        {
            return;
        }

        var success = RenderDocNative.EndFrameCapture();
        _isCapturing = false;

        OnCaptureCompleted(DebugCaptureType.RangeCapture, success);
    }

    /// <inheritdoc/>
    public void SetCaptureFilePath(string filePath)
    {
        _captureFilePathTemplate = filePath;

        if (IsAvailable && !string.IsNullOrEmpty(filePath))
        {
            RenderDocNative.SetCaptureFilePathTemplate(filePath);
        }
    }

    /// <inheritdoc/>
    public bool OpenCapture(string? filePath = null)
    {
        if (!IsAvailable)
        {
            return false;
        }

        var path = filePath ?? _lastCaptureFilePath;

        if (!string.IsNullOrEmpty(path))
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        var result = RenderDocNative.LaunchReplayUI(1, null);
        return result != 0;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 检查 RenderDoc 是否已连接到目标进程
    /// </summary>
    public bool IsConnected()
    {
        return IsAvailable && RenderDocNative.IsTargetControlConnected();
    }

    /// <summary>
    /// 启动 RenderDoc Replay UI
    /// </summary>
    /// <param name="connectToTarget">是否连接到目标进程</param>
    /// <param name="cmdLine">命令行参数</param>
    /// <returns>Replay UI 进程 ID，0 表示失败</returns>
    public uint LaunchReplayUI(bool connectToTarget = true, string? cmdLine = null)
    {
        if (!IsAvailable)
        {
            return 0;
        }

        return RenderDocNative.LaunchReplayUI(connectToTarget ? 1 : 0, cmdLine);
    }

    /// <summary>
    /// 设置 RenderDoc overlay 显示选项
    /// </summary>
    /// <param name="enabled">是否启用 overlay</param>
    /// <param name="showFrameRate">是否显示帧率</param>
    /// <param name="showFrameNumber">是否显示帧号</param>
    /// <param name="showCaptureList">是否显示捕获列表</param>
    public void SetOverlayOptions(bool enabled, bool showFrameRate = true, bool showFrameNumber = true, bool showCaptureList = true)
    {
        if (!IsAvailable)
        {
            return;
        }

        uint andMask = RenderDocNative.Overlay_All;
        uint orMask = 0;

        if (!enabled)
        {
            andMask = ~RenderDocNative.Overlay_Enabled;
        }
        else
        {
            orMask |= RenderDocNative.Overlay_Enabled;
        }

        if (showFrameRate)
        {
            orMask |= RenderDocNative.Overlay_FrameRate;
        }
        else
        {
            andMask &= ~RenderDocNative.Overlay_FrameRate;
        }

        if (showFrameNumber)
        {
            orMask |= RenderDocNative.Overlay_FrameNumber;
        }
        else
        {
            andMask &= ~RenderDocNative.Overlay_FrameNumber;
        }

        if (showCaptureList)
        {
            orMask |= RenderDocNative.Overlay_CaptureList;
        }
        else
        {
            andMask &= ~RenderDocNative.Overlay_CaptureList;
        }

        RenderDocNative.MaskOverlayBits(andMask, orMask);
    }

    /// <summary>
    /// 获取当前 overlay 位掩码
    /// </summary>
    public uint GetOverlayBits()
    {
        if (!IsAvailable)
        {
            return 0;
        }

        return RenderDocNative.GetOverlayBits();
    }

    /// <summary>
    /// 获取已完成的捕获数量
    /// </summary>
    public int GetCaptureCount()
    {
        if (!IsAvailable)
        {
            return 0;
        }

        return RenderDocNative.GetNumCaptures();
    }

    #endregion

    #region 私有方法

    private void ConfigureDefaults()
    {
        SetOverlayOptions(true, true, true, false);
    }

    private void OnCaptureCompleted(DebugCaptureType captureType, bool success, string? filePath = null, string? errorMessage = null)
    {
        if (success && filePath == null)
        {
            var captureCount = RenderDocNative.GetNumCaptures();

            if (captureCount > 0 && !string.IsNullOrEmpty(_captureFilePathTemplate))
            {
                _lastCaptureFilePath = $"{_captureFilePathTemplate}_{captureCount - 1}.rdc";
            }
        }
        else
        {
            _lastCaptureFilePath = filePath;
        }

        CaptureCompleted?.Invoke(this, new DebugCaptureEventArgs(captureType, success, _lastCaptureFilePath, errorMessage));
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_isCapturing && IsAvailable)
        {
            try
            {
                RenderDocNative.EndFrameCapture();
                _isCapturing = false;
            }
            catch (Exception)
            {
            }
        }
    }

    #endregion
}
