using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public sealed class DeferredRenderer
{
    #region 字段

    private readonly IDevice _device;
    private readonly RenderPipeline _pipeline;
    private IRhiSwapchain? _swapchain;
    private GBuffer? _gbuffer;
    private IRhiRenderPass? _geometryPass;
    private IRhiRenderPass? _lightingPass;
    private IRhiFramebuffer? _geometryFramebuffer;
    private IRhiFramebuffer? _lightingFramebuffer;
    private IRhiSemaphore? _imageAvailableSemaphore;
    private IRhiSemaphore? _renderFinishedSemaphore;
    private IRhiFence? _inFlightFence;
    private uint _currentImageIndex;
    private nint _windowHandle;

    #endregion

    #region 属性

    public IDevice Device => _device;
    public RenderPipeline Pipeline => _pipeline;
    public IRhiSwapchain? Swapchain => _swapchain;
    public GBuffer? GBuffer => _gbuffer;

    #endregion

    #region 构造函数

    public DeferredRenderer(IDevice device)
    {
        _device = device;
        _pipeline = new RenderPipeline("DeferredRenderer");
    }

    #endregion

    #region 公开方法

    public void Initialize(nint windowHandle, uint width, uint height)
    {
        _windowHandle = windowHandle;
        CreateSwapchain(width, height);
        CreateGBuffer(width, height);
        CreateGeometryRenderPass();
        CreateLightingRenderPass();
        CreateSyncObjects();
    }

    public void Resize(uint width, uint height)
    {
        _device.WaitIdle();

        _geometryFramebuffer?.Dispose();
        _lightingFramebuffer?.Dispose();
        _gbuffer?.Dispose();
        _geometryPass?.Dispose();
        _lightingPass?.Dispose();
        _swapchain?.Dispose();

        CreateSwapchain(width, height);
        CreateGBuffer(width, height);
        CreateGeometryRenderPass();
        CreateLightingRenderPass();
    }

    public void Render(RenderContext context)
    {
        if (_swapchain is null || _imageAvailableSemaphore is null || _renderFinishedSemaphore is null || _inFlightFence is null)
        {
            return;
        }

        _inFlightFence.Wait();
        _inFlightFence.Reset();

        _currentImageIndex = _swapchain.AcquireNextImage(_imageAvailableSemaphore, null);

        RebuildFramebuffers();

        RenderGeometryPass(context);
        RenderLightingPass(context);

        _swapchain.Present([_renderFinishedSemaphore]);
    }

    public void Shutdown()
    {
        _device.WaitIdle();

        _geometryFramebuffer?.Dispose();
        _lightingFramebuffer?.Dispose();
        _gbuffer?.Dispose();
        _geometryPass?.Dispose();
        _lightingPass?.Dispose();
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

    private void CreateGBuffer(uint width, uint height)
    {
        _gbuffer = new GBuffer(_device, width, height);
    }

    private void CreateGeometryRenderPass()
    {
        var attachments = new[]
        {
            new AttachmentDesc
            {
                Format = ResourceFormat.R8G8B8A8Unorm,
                SampleCount = 1,
                LoadAction = LoadAction.Clear,
                StoreAction = StoreAction.Store,
                StencilLoadAction = LoadAction.DontCare,
                StencilStoreAction = StoreAction.DontCare,
                InitialLayout = TextureLayout.Undefined,
                FinalLayout = TextureLayout.ShaderReadOnly
            },
            new AttachmentDesc
            {
                Format = ResourceFormat.R16G16B16A16Float,
                SampleCount = 1,
                LoadAction = LoadAction.Clear,
                StoreAction = StoreAction.Store,
                StencilLoadAction = LoadAction.DontCare,
                StencilStoreAction = StoreAction.DontCare,
                InitialLayout = TextureLayout.Undefined,
                FinalLayout = TextureLayout.ShaderReadOnly
            },
            new AttachmentDesc
            {
                Format = ResourceFormat.R8G8B8A8Unorm,
                SampleCount = 1,
                LoadAction = LoadAction.Clear,
                StoreAction = StoreAction.Store,
                StencilLoadAction = LoadAction.DontCare,
                StencilStoreAction = StoreAction.DontCare,
                InitialLayout = TextureLayout.Undefined,
                FinalLayout = TextureLayout.ShaderReadOnly
            },
            new AttachmentDesc
            {
                Format = ResourceFormat.D32FloatS8Uint,
                SampleCount = 1,
                LoadAction = LoadAction.Clear,
                StoreAction = StoreAction.Store,
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
                ColorAttachments = [0, 1, 2],
                DepthStencilAttachment = 3,
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

        _geometryPass = _device.CreateRenderPass(desc);
    }

    private void CreateLightingRenderPass()
    {
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
            }
        };

        var subPasses = new[]
        {
            new SubPassDesc
            {
                ColorAttachments = [0],
                DepthStencilAttachment = uint.MaxValue,
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

        _lightingPass = _device.CreateRenderPass(desc);
    }

    private void RebuildFramebuffers()
    {
        if (_swapchain is null || _gbuffer is null || _geometryPass is null || _lightingPass is null)
        {
            return;
        }

        _geometryFramebuffer?.Dispose();
        _lightingFramebuffer?.Dispose();

        var geometryAttachments = new[]
        {
            _gbuffer.AlbedoTexture,
            _gbuffer.NormalTexture,
            _gbuffer.MaterialTexture,
            _gbuffer.DepthTexture
        };

        _geometryFramebuffer = _device.CreateFramebuffer(new FramebufferDesc
        {
            RenderPass = _geometryPass,
            Attachments = geometryAttachments,
            Width = _swapchain.Width,
            Height = _swapchain.Height,
            Layers = 1
        });

        var swapchainImage = _device.CreateTexture(new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = _swapchain.Width,
            Height = _swapchain.Height,
            Format = _swapchain.Format,
            Usage = TextureUsage.RenderTarget,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        });

        _lightingFramebuffer = _device.CreateFramebuffer(new FramebufferDesc
        {
            RenderPass = _lightingPass,
            Attachments = [swapchainImage],
            Width = _swapchain.Width,
            Height = _swapchain.Height,
            Layers = 1
        });
    }

    private void RenderGeometryPass(RenderContext context)
    {
        if (_geometryPass is null || _geometryFramebuffer is null || _gbuffer is null)
        {
            return;
        }

        var commandTable = _device.CreateCommandTable();
        commandTable.Begin();

        var clearColors = new (float r, float g, float b, float a)[]
        {
            (0.0f, 0.0f, 0.0f, 1.0f),
            (0.0f, 0.0f, 0.0f, 0.0f),
            (0.0f, 0.0f, 0.0f, 1.0f)
        };

        commandTable.BeginRenderPass(_geometryPass, _geometryFramebuffer, clearColors, 1.0f, 0);
        commandTable.SetViewport(0, 0, _gbuffer.Width, _gbuffer.Height);
        commandTable.SetScissor(0, 0, _gbuffer.Width, _gbuffer.Height);

        var renderContext = context with
        {
            Device = _device,
            Width = _gbuffer.Width,
            Height = _gbuffer.Height
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
    }

    private void RenderLightingPass(RenderContext context)
    {
        if (_lightingPass is null || _lightingFramebuffer is null || _swapchain is null)
        {
            return;
        }

        var commandTable = _device.CreateCommandTable();
        commandTable.Begin();

        var clearColors = new (float r, float g, float b, float a)[]
        {
            (0.0f, 0.0f, 0.0f, 1.0f)
        };

        commandTable.BeginRenderPass(_lightingPass, _lightingFramebuffer, clearColors);
        commandTable.SetViewport(0, 0, _swapchain.Width, _swapchain.Height);
        commandTable.SetScissor(0, 0, _swapchain.Width, _swapchain.Height);

        var renderContext = context with
        {
            Device = _device,
            Width = _swapchain.Width,
            Height = _swapchain.Height
        };

        commandTable.EndRenderPass();
        commandTable.End();

        _device.Submit(commandTable);
        commandTable.Dispose();
    }

    private void CreateSyncObjects()
    {
        _imageAvailableSemaphore = _device.CreateSemaphore();
        _renderFinishedSemaphore = _device.CreateSemaphore();
        _inFlightFence = _device.CreateFence(signaled: true);
    }

    #endregion
}
