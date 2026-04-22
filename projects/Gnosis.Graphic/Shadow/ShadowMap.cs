using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Shadow;

public sealed class ShadowMap : IDisposable
{
    #region 字段

    private readonly IDevice _device;
    private bool _isDisposed;

    #endregion

    #region 属性

    public int Resolution { get; private set; }
    public IResource DepthTexture { get; private set; }
    public IRhiRenderPass RenderPass { get; private set; }
    public IRhiFramebuffer Framebuffer { get; private set; }

    #endregion

    #region 构造函数

    public ShadowMap(IDevice device, int resolution)
    {
        _device = device;
        Resolution = resolution;

        DepthTexture = CreateDepthTexture(resolution);
        RenderPass = CreateRenderPass();
        Framebuffer = CreateFramebuffer();
    }

    #endregion

    #region 公开方法

    public void Resize(int resolution)
    {
        if (resolution == Resolution)
        {
            return;
        }

        Resolution = resolution;

        DepthTexture.Dispose();
        Framebuffer.Dispose();
        RenderPass.Dispose();

        DepthTexture = CreateDepthTexture(resolution);
        RenderPass = CreateRenderPass();
        Framebuffer = CreateFramebuffer();
    }

    #endregion

    #region 私有方法

    private IResource CreateDepthTexture(int resolution)
    {
        return _device.CreateTexture(new TextureDesc
        {
            Dimension = TextureDimension.Texture2D,
            Width = (uint)resolution,
            Height = (uint)resolution,
            Depth = 1,
            Format = ResourceFormat.D32FloatS8Uint,
            Usage = TextureUsage.DepthStencil | TextureUsage.ShaderResource,
            MipLevels = 1,
            ArrayLayers = 1,
            SampleCount = 1
        });
    }

    private IRhiRenderPass CreateRenderPass()
    {
        var attachments = new[]
        {
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
                ColorAttachments = [],
                DepthStencilAttachment = 0,
                InputAttachments = []
            }
        };

        var dependencies = new[]
        {
            new SubPassDependency
            {
                SrcSubPass = uint.MaxValue,
                DstSubPass = 0,
                SrcStage = PipelineStageFlag.EarlyFragmentTests,
                DstStage = PipelineStageFlag.EarlyFragmentTests,
                SrcAccess = AccessFlag.None,
                DstAccess = AccessFlag.DepthStencilWrite
            }
        };

        return _device.CreateRenderPass(new RenderPassDesc
        {
            Attachments = attachments,
            SubPasses = subPasses,
            Dependencies = dependencies
        });
    }

    private IRhiFramebuffer CreateFramebuffer()
    {
        return _device.CreateFramebuffer(new FramebufferDesc
        {
            RenderPass = RenderPass,
            Attachments = [DepthTexture],
            Width = (uint)Resolution,
            Height = (uint)Resolution,
            Layers = 1
        });
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        DepthTexture.Dispose();
        Framebuffer.Dispose();
        RenderPass.Dispose();

        _isDisposed = true;
    }

    #endregion
}
