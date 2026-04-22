using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Pipeline;

public sealed class ForwardRenderer
{
    private readonly IDevice _device;
    private readonly RenderPipeline _pipeline;
    private IRhiSwapchain? _swapchain;
    private IResource? _depthTexture;
    private IRhiRenderPass? _renderPass;
    private IRhiFramebuffer? _framebuffer;
    private IRhiSemaphore? _imageAvailableSemaphore;
    private IRhiSemaphore? _renderFinishedSemaphore;
    private IRhiFence? _inFlightFence;
    private uint _currentImageIndex;
    private nint _windowHandle;

    public IDevice Device => _device;
    public RenderPipeline Pipeline => _pipeline;
    public IRhiSwapchain? Swapchain => _swapchain;

    public ForwardRenderer(IDevice device)
    {
        _device = device;
        _pipeline = new RenderPipeline("ForwardRenderer");
    }

    public void Initialize(nint windowHandle, uint width, uint height)
    {
        _windowHandle = windowHandle;
        CreateSwapchain(width, height);
        CreateDepthTexture(width, height);
        CreateRenderPass();
        CreateSyncObjects();
    }

    public void Resize(uint width, uint height)
    {
        _device.WaitIdle();

        _framebuffer?.Dispose();
        _depthTexture?.Dispose();
        _renderPass?.Dispose();
        _swapchain?.Dispose();

        CreateSwapchain(width, height);
        CreateDepthTexture(width, height);
        CreateRenderPass();
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

        RebuildFramebuffer();

        var commandTable = _device.CreateCommandTable();
        commandTable.Begin();

        commandTable.BeginRenderPass(_renderPass!, _framebuffer!);

        commandTable.SetViewport(0, 0, _swapchain.Width, _swapchain.Height);
        commandTable.SetScissor(0, 0, _swapchain.Width, _swapchain.Height);

        var renderContext = context with
        {
            Device = _device,
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

    public void Shutdown()
    {
        _device.WaitIdle();

        _framebuffer?.Dispose();
        _depthTexture?.Dispose();
        _renderPass?.Dispose();
        _swapchain?.Dispose();
        _imageAvailableSemaphore?.Dispose();
        _renderFinishedSemaphore?.Dispose();
        _inFlightFence?.Dispose();
    }

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

    private void CreateDepthTexture(uint width, uint height)
    {
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

        if (_swapchain is null || _depthTexture is null || _renderPass is null)
        {
            return;
        }

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

        var attachments = new[] { swapchainImage, _depthTexture };

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
}
