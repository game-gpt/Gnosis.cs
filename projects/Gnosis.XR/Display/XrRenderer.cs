using Gnosis.Core.Math;
using Gnosis.Graphic.RHI;
using Gnosis.XR.Session;
using Gnosis.XR.Tracking;

namespace Gnosis.XR.Display;

/// <summary>
/// XR 渲染器实现，提供立体渲染、单通道多视图与注视点渲染能力
/// </summary>
public sealed class XrRenderer : IXrRenderer
{
    #region 字段

    private IDevice? _device;
    private IResource? _leftRenderTarget;
    private IResource? _rightRenderTarget;
    private IResource? _leftDepthStencil;
    private IResource? _rightDepthStencil;
    private IRhiFramebuffer? _leftFramebuffer;
    private IRhiFramebuffer? _rightFramebuffer;
    private IRhiRenderPass? _renderPass;
    private ulong _frameIndex;
    private bool _isDisposed;
    private bool _isInitialized;
    private bool _inFrame;

    #endregion

    #region 属性

    /// <summary>
    /// 关联的 XR 会话
    /// </summary>
    public IXrSession Session { get; }

    /// <summary>
    /// 视图配置类型
    /// </summary>
    public XrViewConfigurationType ViewConfiguration { get; }

    /// <summary>
    /// 渲染目标宽度
    /// </summary>
    public uint RenderTargetWidth { get; private set; }

    /// <summary>
    /// 渲染目标高度
    /// </summary>
    public uint RenderTargetHeight { get; private set; }

    /// <summary>
    /// 是否支持单通道多视图渲染
    /// </summary>
    public bool SupportsMultiview { get; }

    /// <summary>
    /// 是否支持注视点渲染
    /// </summary>
    public bool SupportsFoveatedRendering { get; }

    /// <summary>
    /// 当前注视点渲染配置
    /// </summary>
    public XrFoveatedRenderingConfig FoveatedRenderingConfig { get; set; }

    /// <summary>
    /// 左眼视图信息
    /// </summary>
    public XrViewInfo LeftEyeView { get; private set; }

    /// <summary>
    /// 右眼视图信息
    /// </summary>
    public XrViewInfo RightEyeView { get; private set; }

    #endregion

    #region 事件

    /// <summary>
    /// 渲染帧开始事件
    /// </summary>
    public event EventHandler<XrRenderFrameEventArgs>? FrameBegin;

    /// <summary>
    /// 渲染帧结束事件
    /// </summary>
    public event EventHandler<XrRenderFrameEventArgs>? FrameEnd;

    #endregion

    #region 构造函数

    /// <summary>
    /// 使用 XR 会话初始化渲染器
    /// </summary>
    /// <param name="session">XR 会话</param>
    /// <param name="viewConfiguration">视图配置类型</param>
    public XrRenderer(IXrSession session, XrViewConfigurationType viewConfiguration = XrViewConfigurationType.Stereo)
    {
        ArgumentNullException.ThrowIfNull(session);

        Session = session;
        ViewConfiguration = viewConfiguration;
        RenderTargetWidth = session.RecommendedRenderTargetWidth;
        RenderTargetHeight = session.RecommendedRenderTargetHeight;
        SupportsMultiview = viewConfiguration == XrViewConfigurationType.StereoMultiview;
        SupportsFoveatedRendering = session.SupportsFoveatedRendering;
        FoveatedRenderingConfig = XrFoveatedRenderingConfig.FromLevel(
            SupportsFoveatedRendering ? XrFoveatedRenderingLevel.Medium : XrFoveatedRenderingLevel.Disabled);
        LeftEyeView = XrViewInfo.Empty;
        RightEyeView = XrViewInfo.Empty;
        _frameIndex = 0;
        _isDisposed = false;
        _isInitialized = false;
        _inFrame = false;
    }

    #endregion

    #region IXrRenderer 实现

    /// <summary>
    /// 初始化渲染器（创建渲染目标、帧缓冲等资源）
    /// </summary>
    /// <param name="device">RHI 图形设备</param>
    public void Initialize(IDevice device)
    {
        ArgumentNullException.ThrowIfNull(device);

        if (_isInitialized)
        {
            return;
        }

        _device = device;
        CreateRenderTargets();
        _isInitialized = true;
    }

    /// <summary>
    /// 开始渲染帧
    /// </summary>
    /// <returns>是否成功获取渲染帧</returns>
    public bool BeginFrame()
    {
        ThrowIfDisposed();

        if (!_isInitialized)
        {
            throw new InvalidOperationException("渲染器尚未初始化，请先调用 Initialize");
        }

        if (_inFrame)
        {
            return false;
        }

        if (!Session.IsRunning)
        {
            return false;
        }

        _inFrame = true;
        _frameIndex++;
        FrameBegin?.Invoke(this, new XrRenderFrameEventArgs(_frameIndex, 0));
        return true;
    }

    /// <summary>
    /// 获取指定眼睛的渲染目标纹理
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>渲染目标纹理资源</returns>
    public IResource? GetRenderTarget(XrEye eye)
    {
        ThrowIfDisposed();

        return eye == XrEye.Left ? _leftRenderTarget : _rightRenderTarget;
    }

    /// <summary>
    /// 获取指定眼睛的深度模板纹理
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>深度模板纹理资源</returns>
    public IResource? GetDepthStencil(XrEye eye)
    {
        ThrowIfDisposed();

        return eye == XrEye.Left ? _leftDepthStencil : _rightDepthStencil;
    }

    /// <summary>
    /// 获取指定眼睛的帧缓冲
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>帧缓冲对象</returns>
    public IRhiFramebuffer? GetFramebuffer(XrEye eye)
    {
        ThrowIfDisposed();

        return eye == XrEye.Left ? _leftFramebuffer : _rightFramebuffer;
    }

    /// <summary>
    /// 获取指定眼睛的视口参数
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>视口参数</returns>
    public (float x, float y, float width, float height) GetViewport(XrEye eye)
    {
        ThrowIfDisposed();

        if (ViewConfiguration == XrViewConfigurationType.StereoMultiview)
        {
            return (0, 0, RenderTargetWidth, RenderTargetHeight);
        }

        return (0, 0, RenderTargetWidth, RenderTargetHeight);
    }

    /// <summary>
    /// 获取指定眼睛的视图信息
    /// </summary>
    /// <param name="eye">眼睛标识</param>
    /// <returns>视图信息</returns>
    public XrViewInfo GetViewInfo(XrEye eye)
    {
        return eye == XrEye.Left ? LeftEyeView : RightEyeView;
    }

    /// <summary>
    /// 结束渲染帧
    /// </summary>
    public void EndFrame()
    {
        ThrowIfDisposed();

        if (!_inFrame)
        {
            return;
        }

        _inFrame = false;
        FrameEnd?.Invoke(this, new XrRenderFrameEventArgs(_frameIndex, 0));
    }

    /// <summary>
    /// 更新视图信息
    /// </summary>
    /// <param name="headTracker">头部追踪器</param>
    public void UpdateViews(IXrTracker headTracker)
    {
        ArgumentNullException.ThrowIfNull(headTracker);

        ThrowIfDisposed();

        if (!headTracker.IsTracking)
        {
            LeftEyeView = XrViewInfo.Empty;
            RightEyeView = XrViewInfo.Empty;
            return;
        }

        var headSnapshot = headTracker.CurrentSnapshot;
        var headPose = headSnapshot.Pose;

        var ipd = 0.064f;

        LeftEyeView = CreateEyeView(headPose, -ipd * 0.5f, XrEye.Left);
        RightEyeView = CreateEyeView(headPose, ipd * 0.5f, XrEye.Right);
    }

    /// <summary>
    /// 重新创建渲染目标
    /// </summary>
    /// <param name="width">新宽度</param>
    /// <param name="height">新高度</param>
    public void ResizeRenderTargets(uint width, uint height)
    {
        ThrowIfDisposed();

        if (width == 0 || height == 0)
        {
            throw new ArgumentOutOfRangeException("渲染目标尺寸不能为零");
        }

        if (RenderTargetWidth == width && RenderTargetHeight == height)
        {
            return;
        }

        RenderTargetWidth = width;
        RenderTargetHeight = height;

        if (_isInitialized)
        {
            DestroyRenderTargets();
            CreateRenderTargets();
        }
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 创建渲染目标、深度模板与帧缓冲
    /// </summary>
    private void CreateRenderTargets()
    {
        if (_device is null)
        {
            return;
        }

        var colorDesc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = RenderTargetWidth,
            Height = RenderTargetHeight,
            Format = ResourceFormat.R8G8B8A8Unorm,
            Usage = TextureUsage.RenderTarget | TextureUsage.ShaderResource,
            SampleCount = 1
        };

        var depthDesc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = RenderTargetWidth,
            Height = RenderTargetHeight,
            Format = ResourceFormat.D32FloatS8Uint,
            Usage = TextureUsage.DepthStencil | TextureUsage.ShaderResource,
            SampleCount = 1
        };

        _leftRenderTarget = _device.CreateTexture(colorDesc);
        _rightRenderTarget = _device.CreateTexture(colorDesc);
        _leftDepthStencil = _device.CreateTexture(depthDesc);
        _rightDepthStencil = _device.CreateTexture(depthDesc);

        var renderPassDesc = new RenderPassDesc
        {
            Attachments =
            [
                new AttachmentDesc
                {
                    Format = ResourceFormat.R8G8B8A8Unorm,
                    SampleCount = 1,
                    LoadAction = LoadAction.Clear,
                    StoreAction = StoreAction.Store,
                    InitialLayout = TextureLayout.Undefined,
                    FinalLayout = TextureLayout.PresentSrc
                },
                new AttachmentDesc
                {
                    Format = ResourceFormat.D32FloatS8Uint,
                    SampleCount = 1,
                    LoadAction = LoadAction.Clear,
                    StoreAction = StoreAction.DontCare,
                    InitialLayout = TextureLayout.Undefined,
                    FinalLayout = TextureLayout.DepthStencilAttachment
                }
            ],
            SubPasses =
            [
                new SubPassDesc
                {
                    ColorAttachments = [0],
                    DepthStencilAttachment = 0
                }
            ]
        };

        _renderPass = _device.CreateRenderPass(renderPassDesc);

        if (_renderPass is not null && _leftRenderTarget is not null && _leftDepthStencil is not null)
        {
            _leftFramebuffer = _device.CreateFramebuffer(new FramebufferDesc
            {
                RenderPass = _renderPass,
                Attachments = [_leftRenderTarget, _leftDepthStencil],
                Width = RenderTargetWidth,
                Height = RenderTargetHeight,
                Layers = 1
            });
        }

        if (_renderPass is not null && _rightRenderTarget is not null && _rightDepthStencil is not null)
        {
            _rightFramebuffer = _device.CreateFramebuffer(new FramebufferDesc
            {
                RenderPass = _renderPass,
                Attachments = [_rightRenderTarget, _rightDepthStencil],
                Width = RenderTargetWidth,
                Height = RenderTargetHeight,
                Layers = 1
            });
        }
    }

    /// <summary>
    /// 销毁渲染目标、深度模板与帧缓冲
    /// </summary>
    private void DestroyRenderTargets()
    {
        _leftFramebuffer?.Dispose();
        _rightFramebuffer?.Dispose();
        _leftRenderTarget?.Dispose();
        _rightRenderTarget?.Dispose();
        _leftDepthStencil?.Dispose();
        _rightDepthStencil?.Dispose();
        _renderPass?.Dispose();

        _leftFramebuffer = null;
        _rightFramebuffer = null;
        _leftRenderTarget = null;
        _rightRenderTarget = null;
        _leftDepthStencil = null;
        _rightDepthStencil = null;
        _renderPass = null;
    }

    /// <summary>
    /// 根据头部姿态与瞳距创建单眼视图信息
    /// </summary>
    /// <param name="headPose">头部姿态</param>
    /// <param name="eyeOffset">眼睛水平偏移（瞳距一半）</param>
    /// <param name="eye">眼睛标识</param>
    /// <returns>单眼视图信息</returns>
    private XrViewInfo CreateEyeView(XrPose headPose, float eyeOffset, XrEye eye)
    {
        var eyePosition = headPose.Position + headPose.Right * eyeOffset;

        var viewMatrix = CreateViewMatrix(eyePosition, headPose.Forward, headPose.Up);

        var fovH = MathF.PI * 100f / 180f;
        var fovV = MathF.PI * 100f / 180f;
        var projectionMatrix = CreatePerspectiveProjection(fovH, fovV, 0.01f, 1000.0f);

        return new XrViewInfo
        {
            ViewMatrix = viewMatrix,
            ProjectionMatrix = projectionMatrix,
            Pose = new XrPose
            {
                Position = eyePosition,
                Rotation = headPose.Rotation
            },
            FieldOfViewHorizontal = fovH,
            FieldOfViewVertical = fovV,
            NearPlane = 0.01f,
            FarPlane = 1000.0f,
            IsValid = true
        };
    }

    /// <summary>
    /// 创建视图矩阵（行主序 4x4）
    /// </summary>
    /// <param name="position">眼睛位置</param>
    /// <param name="forward">前方向量</param>
    /// <param name="up">上方向量</param>
    /// <returns>16 元素行主序视图矩阵</returns>
    private static float[] CreateViewMatrix(Vector3 position, Vector3 forward, Vector3 up)
    {
        var f = forward.Normalize();
        var r = Vector3.Cross(up, f).Normalize();
        var u = Vector3.Cross(f, r);

        return
        [
            r.X, r.Y, r.Z, -Vector3.Dot(r, position),
            u.X, u.Y, u.Z, -Vector3.Dot(u, position),
            f.X, f.Y, f.Z, -Vector3.Dot(f, position),
            0, 0, 0, 1
        ];
    }

    /// <summary>
    /// 创建透视投影矩阵（行主序 4x4）
    /// </summary>
    /// <param name="fovH">水平视场角（弧度）</param>
    /// <param name="fovV">垂直视场角（弧度）</param>
    /// <param name="near">近裁剪面</param>
    /// <param name="far">远裁剪面</param>
    /// <returns>16 元素行主序投影矩阵</returns>
    private static float[] CreatePerspectiveProjection(float fovH, float fovV, float near, float far)
    {
        var tanHalfFovH = MathF.Tan(fovH * 0.5f);
        var tanHalfFovV = MathF.Tan(fovV * 0.5f);

        return
        [
            1.0f / tanHalfFovH, 0, 0, 0,
            0, 1.0f / tanHalfFovV, 0, 0,
            0, 0, far / (near - far), near * far / (near - far),
            0, 0, -1, 0
        ];
    }

    /// <summary>
    /// 若已释放则抛出异常
    /// </summary>
    private void ThrowIfDisposed()
    {
        if (_isDisposed)
        {
            throw new ObjectDisposedException(nameof(XrRenderer));
        }
    }

    #endregion

    #region IDisposable 实现

    /// <summary>
    /// 释放渲染器资源
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        DestroyRenderTargets();
        _isDisposed = true;
    }

    #endregion
}
