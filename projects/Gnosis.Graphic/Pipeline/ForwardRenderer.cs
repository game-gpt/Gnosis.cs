using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public sealed class ForwardRenderer
{
    #region 字段

    private readonly IDevice _device;
    private readonly RenderPipeline _pipeline;
    private IRhiSwapchain? _swapchain;
    private IResource? _colorTexture;
    private IResource? _depthTexture;
    private IRhiRenderPass? _renderPass;
    private IRhiFramebuffer? _framebuffer;
    private IRhiSemaphore? _imageAvailableSemaphore;
    private IRhiSemaphore? _renderFinishedSemaphore;
    private IRhiFence? _inFlightFence;
    private uint _currentImageIndex;
    private nint _windowHandle;
    private uint _width;
    private uint _height;

    #endregion

    #region 属性

    public IDevice Device => _device;
    public RenderPipeline Pipeline => _pipeline;
    public IRhiSwapchain? Swapchain => _swapchain;

    #endregion

    #region 构造函数

    public ForwardRenderer(IDevice device)
    {
        _device = device;
        _pipeline = new RenderPipeline("ForwardRenderer");
    }

    #endregion

    #region 初始化

    public void Initialize(nint windowHandle, uint width, uint height)
    {
        _windowHandle = windowHandle;
        _width = width;
        _height = height;
        CreateSwapchain(width, height);
        CreateColorTexture(width, height);
        CreateDepthTexture(width, height);
        CreateRenderPass();
        CreateSyncObjects();
    }

    public void Resize(uint width, uint height)
    {
        _device.WaitIdle();

        _width = width;
        _height = height;

        _framebuffer?.Dispose();
        _colorTexture?.Dispose();
        _depthTexture?.Dispose();
        _renderPass?.Dispose();
        _swapchain?.Dispose();

        CreateSwapchain(width, height);
        CreateColorTexture(width, height);
        CreateDepthTexture(width, height);
        CreateRenderPass();
    }

    #endregion

    #region 渲染

    public void Render(RenderContext context)
    {
        if (_swapchain is null || _imageAvailableSemaphore is null || _renderFinishedSemaphore is null || _inFlightFence is null)
        {
            return;
        }

        _inFlightFence.Wait();
        _inFlightFence.Reset();

        _currentImageIndex = _swapchain.AcquireNextImage(_imageAvailableSemaphore, null);

        RebuildFramebuffer();

        var commandTable = _device.CreateCommandTable();
        commandTable.Begin();

        var clearColors = new List<(float r, float g, float b, float a)>
        {
            (0.53f, 0.81f, 0.92f, 1.0f),
            (1.0f, 0.0f, 0.0f, 0.0f)
        };

        commandTable.BeginRenderPass(_renderPass!, _framebuffer!, clearColors, 1.0f, 0);

        commandTable.SetViewport(0, 0, _swapchain.Width, _swapchain.Height);
        commandTable.SetScissor(0, 0, _swapchain.Width, _swapchain.Height);

        var renderContext = new RenderContext
        {
            View = context.View,
            Device = _device,
            DeltaTime = context.DeltaTime,
            FrameIndex = context.FrameIndex,
            Width = _swapchain.Width,
            Height = _swapchain.Height
        };

        foreach (var pass in _pipeline.Passes)
        {
            if (!pass.Enabled)
            {
                continue;
            }

            pass.Execute(renderContext, commandTable);
        }

        commandTable.EndRenderPass();
        commandTable.End();

        _device.Submit(commandTable, _inFlightFence);
        commandTable.Dispose();

        _swapchain.Present([_renderFinishedSemaphore]);
    }

    #endregion

    #region Pass 管理

    public void AddRenderPass(IRenderPass pass)
    {
        _pipeline.AddPass(pass);
    }

    public bool RemoveRenderPass(string passName)
    {
        return _pipeline.RemovePass(passName);
    }

    #endregion

    #region 关闭

    public void Shutdown()
    {
        _device.WaitIdle();

        _framebuffer?.Dispose();
        _colorTexture?.Dispose();
        _depthTexture?.Dispose();
        _renderPass?.Dispose();
        _swapchain?.Dispose();
        _imageAvailableSemaphore?.Dispose();
        _renderFinishedSemaphore?.Dispose();
        _inFlightFence?.Dispose();
    }

    #endregion

    #region 私有方法

    private void CreateSwapchain(uint width, uint height)
    {
        var desc = new SwapchainDesc
        {
            WindowHandle = _windowHandle,
            Width = width,
            Height = height,
            Format = ResourceFormat.B8G8R8A8Unorm,
            PresentMode = PresentMode.Fifo,
            ImageCount = 2,
            VSync = true
        };

        _swapchain = _device.CreateSwapchain(desc);
    }

    private void CreateColorTexture(uint width, uint height)
    {
        _colorTexture?.Dispose();

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = width,
            Height = height,
            Depth = 1,
            Format = ResourceFormat.B8G8R8A8Unorm,
            Usage = TextureUsage.RenderTarget,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        };

        _colorTexture = _device.CreateTexture(desc);
    }

    private void CreateDepthTexture(uint width, uint height)
    {
        _depthTexture?.Dispose();

        var desc = new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = width,
            Height = height,
            Depth = 1,
            Format = ResourceFormat.D32FloatS8Uint,
            Usage = TextureUsage.DepthStencil,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        };

        _depthTexture = _device.CreateTexture(desc);
    }

    private void CreateRenderPass()
    {
        _renderPass?.Dispose();

        var attachments = new[]
        {
            new AttachmentDesc
            {
                Format = ResourceFormat.B8G8R8A8Unorm,
                SampleCount = 1,
                LoadAction = LoadAction.Clear,
                StoreAction = StoreAction.Store,
                StencilLoadAction = LoadAction.DontCare,
                StencilStoreAction = StoreAction.DontCare,
                InitialLayout = TextureLayout.Undefined,
                FinalLayout = TextureLayout.PresentSrc
            },
            new AttachmentDesc
            {
                Format = ResourceFormat.D32FloatS8Uint,
                SampleCount = 1,
                LoadAction = LoadAction.Clear,
                StoreAction = StoreAction.DontCare,
                StencilLoadAction = LoadAction.DontCare,
                StencilStoreAction = StoreAction.DontCare,
                InitialLayout = TextureLayout.Undefined,
                FinalLayout = TextureLayout.DepthStencilAttachment
            }
        };

        var subPasses = new[]
        {
            new SubPassDesc
            {
                ColorAttachments = [0],
                DepthStencilAttachment = 1,
                InputAttachments = []
            }
        };

        var dependencies = new[]
        {
            new SubPassDependency
            {
                SrcSubPass = uint.MaxValue,
                DstSubPass = 0,
                SrcStage = PipelineStageFlag.ColorAttachmentOutput,
                DstStage = PipelineStageFlag.ColorAttachmentOutput,
                SrcAccess = AccessFlag.None,
                DstAccess = AccessFlag.ColorAttachmentWrite
            }
        };

        var desc = new RenderPassDesc
        {
            Attachments = attachments,
            SubPasses = subPasses,
            Dependencies = dependencies
        };

        _renderPass = _device.CreateRenderPass(desc);
    }

    private void RebuildFramebuffer()
    {
        _framebuffer?.Dispose();

        if (_colorTexture is null || _depthTexture is null || _renderPass is null || _swapchain is null)
        {
            return;
        }

        var attachments = new[] { _colorTexture, _depthTexture };

        var desc = new FramebufferDesc
        {
            RenderPass = _renderPass,
            Attachments = attachments,
            Width = _swapchain.Width,
            Height = _swapchain.Height,
            Layers = 1
        };

        _framebuffer = _device.CreateFramebuffer(desc);
    }

    private void CreateSyncObjects()
    {
        _imageAvailableSemaphore = _device.CreateSemaphore();
        _renderFinishedSemaphore = _device.CreateSemaphore();
        _inFlightFence = _device.CreateFence(signaled: true);
    }

    #endregion
}
