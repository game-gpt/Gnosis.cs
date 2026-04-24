using System.Runtime.InteropServices;
using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.RHI.OpenGL;

public sealed unsafe class OpenGLDevice : IDevice
{
    #region 内部状态

    private readonly Dictionary<ulong, OpenGLResource> _resources = [];
    private bool _isDisposed;
    private OpenGLSwapchain? _swapchain;

    #endregion

    #region 初始化

    public void Initialize(Func<string, nint> getProcAddress)
    {
        GlNative.LoadFunctions(getProcAddress);
    }

    #endregion

    #region IDevice - 资源创建

    public IResource CreateBuffer(in BufferDesc desc)
    {
        uint buffer;
        GlNative.GenBuffers!(1, &buffer);
        GlNative.BindBuffer!(GlConversions.ToGlBufferUsage(desc.Usage), buffer);

        uint usage = desc.Usage switch
        {
            BufferUsage.VertexBuffer => GlConstants.GL_STATIC_DRAW,
            BufferUsage.IndexBuffer => GlConstants.GL_STATIC_DRAW,
            BufferUsage.UniformBuffer => GlConstants.GL_DYNAMIC_DRAW,
            BufferUsage.StorageBuffer => GlConstants.GL_DYNAMIC_DRAW,
            _ => GlConstants.GL_STATIC_DRAW
        };

        var glUsage = GlConversions.ToGlBufferUsage(desc.Usage);
        if (desc.InitialData != null)
        {
            fixed (byte* pData = desc.InitialData)
            {
                GlNative.BufferData!(glUsage, (nint)desc.Size, pData, usage);
            }
        }
        else
        {
            GlNative.BufferData!(glUsage, (nint)desc.Size, null, usage);
        }

        var resource = new OpenGLResource(ResourceType.Buffer, ResourceFormat.Unknown, desc.Size, glBuffer: buffer);
        _resources[resource.Id] = resource;
        return resource;
    }

    public IResource CreateTexture(in TextureDesc desc)
    {
        uint texture;
        GlNative.GenTextures!(1, &texture);
        var target = GlConversions.ToGlTextureTarget(desc.Dimension);
        GlNative.BindTexture!(target, texture);

        var internalFormat = GlConversions.ToGlInternalFormat(desc.Format);
        var format = GlConversions.ToGlFormat(desc.Format);
        var type = GlConversions.ToGlType(desc.Format);

        GlNative.TexImage2D!(target, 0, internalFormat, (int)desc.Width, (int)desc.Height, 0, format, type, null);

        GlNative.TexParameteri!(target, GlConstants.GL_TEXTURE_MIN_FILTER, (int)GlConstants.GL_LINEAR);
        GlNative.TexParameteri!(target, GlConstants.GL_TEXTURE_MAG_FILTER, (int)GlConstants.GL_LINEAR);
        GlNative.TexParameteri!(target, GlConstants.GL_TEXTURE_WRAP_S, (int)GlConstants.GL_CLAMP_TO_EDGE);
        GlNative.TexParameteri!(target, GlConstants.GL_TEXTURE_WRAP_T, (int)GlConstants.GL_CLAMP_TO_EDGE);

        var resourceType = desc.Dimension switch
        {
            TextureDimension.Texture1D => ResourceType.Texture1D,
            TextureDimension.Texture2D => ResourceType.Texture2D,
            TextureDimension.Texture3D => ResourceType.Texture3D,
            _ => ResourceType.Texture2D
        };

        var resource = new OpenGLResource(resourceType, desc.Format, desc.Width * desc.Height * 4, glTexture: texture);
        _resources[resource.Id] = resource;
        return resource;
    }

    public IResource CreateSampler(in SamplerDesc desc)
    {
        uint sampler;
        GlNative.GenSamplers!(1, &sampler);

        GlNative.SamplerParameteri!(sampler, GlConstants.GL_TEXTURE_MIN_FILTER, (int)GlConversions.ToGlFilterMode(desc.MinFilter));
        GlNative.SamplerParameteri!(sampler, GlConstants.GL_TEXTURE_MAG_FILTER, (int)GlConversions.ToGlFilterMode(desc.MagFilter));
        GlNative.SamplerParameteri!(sampler, GlConstants.GL_TEXTURE_WRAP_S, (int)GlConversions.ToGlAddressMode(desc.AddressModeU));
        GlNative.SamplerParameteri!(sampler, GlConstants.GL_TEXTURE_WRAP_T, (int)GlConversions.ToGlAddressMode(desc.AddressModeV));
        GlNative.SamplerParameteri!(sampler, GlConstants.GL_TEXTURE_WRAP_R, (int)GlConversions.ToGlAddressMode(desc.AddressModeW));

        GlNative.SamplerParameterf!(sampler, GlConstants.GL_TEXTURE_MIN_LOD, desc.MinLod);
        GlNative.SamplerParameterf!(sampler, GlConstants.GL_TEXTURE_MAX_LOD, desc.MaxLod);

        if (desc.MaxAnisotropy > 1.0f)
        {
            GlNative.SamplerParameterf!(sampler, GlConstants.GL_TEXTURE_MAX_ANISOTROPY, desc.MaxAnisotropy);
        }

        if (desc.CompareEnable)
        {
            GlNative.SamplerParameteri!(sampler, GlConstants.GL_TEXTURE_COMPARE_MODE, (int)GlConstants.GL_COMPARE_REF_TO_TEXTURE);
            GlNative.SamplerParameteri!(sampler, GlConstants.GL_TEXTURE_COMPARE_FUNC, (int)GlConversions.ToGlCompareFunction(desc.CompareOp));
        }

        var resource = new OpenGLResource(ResourceType.Sampler, ResourceFormat.Unknown, 0, glSampler: sampler);
        _resources[resource.Id] = resource;
        return resource;
    }

    public IResource CreateShader(in ShaderDesc desc)
    {
        var glStage = GlConversions.ToGlShaderStage(desc.Stage);
        uint shader = GlNative.CreateShader!(glStage);

        fixed (byte* pSource = desc.Bytecode)
        {
            int length = desc.Bytecode.Length;
            GlNative.ShaderSource!(shader, 1, &pSource, &length);
        }

        GlNative.CompileShader!(shader);

        int compileStatus;
        GlNative.GetShaderiv!(shader, GlConstants.GL_COMPILE_STATUS, &compileStatus);
        if (compileStatus == 0)
        {
            int infoLogLength;
            GlNative.GetShaderiv!(shader, GlConstants.GL_INFO_LOG_LENGTH, &infoLogLength);
            byte* infoLog = stackalloc byte[infoLogLength > 0 ? infoLogLength : 1];
            GlNative.GetShaderInfoLog!(shader, infoLogLength, &infoLogLength, infoLog);
            GlNative.DeleteShader!(shader);
            throw new InvalidOperationException($"着色器编译失败：{Marshal.PtrToStringAnsi((nint)infoLog)}");
        }

        uint program = GlNative.CreateProgram!();
        GlNative.AttachShader!(program, shader);
        GlNative.LinkProgram!(program);

        int linkStatus;
        GlNative.GetProgramiv!(program, GlConstants.GL_LINK_STATUS, &linkStatus);
        if (linkStatus == 0)
        {
            int infoLogLength;
            GlNative.GetProgramiv!(program, GlConstants.GL_INFO_LOG_LENGTH, &infoLogLength);
            byte* infoLog = stackalloc byte[infoLogLength > 0 ? infoLogLength : 1];
            GlNative.GetProgramInfoLog!(program, infoLogLength, &infoLogLength, infoLog);
            GlNative.DeleteProgram!(program);
            GlNative.DeleteShader!(shader);
            throw new InvalidOperationException($"着色器链接失败：{Marshal.PtrToStringAnsi((nint)infoLog)}");
        }

        var resource = new OpenGLResource(ResourceType.Shader, ResourceFormat.Unknown, (ulong)desc.Bytecode.Length,
            glProgram: program, glShader: shader, shaderStage: desc.Stage);
        _resources[resource.Id] = resource;
        return resource;
    }

    public IPipelineState CreatePipelineState(in PipelineStateDesc desc)
    {
        return new OpenGLPipelineState(in desc);
    }

    public IRhiRenderPass CreateRenderPass(in RenderPassDesc desc)
    {
        uint attachmentCount = (uint)desc.Attachments.Length;
        return new OpenGLRenderPass(attachmentCount, 1);
    }

    public IRhiFramebuffer CreateFramebuffer(in FramebufferDesc desc)
    {
        uint fbo;
        GlNative.GenFramebuffers!(1, &fbo);
        GlNative.BindFramebuffer!(GlConstants.GL_FRAMEBUFFER, fbo);

        var attachments = new List<IResource>();
        uint colorIndex = 0;

        foreach (var attachment in desc.Attachments)
        {
            var res = attachment as OpenGLResource;
            if (res == null) continue;

            if (res.ResourceType == ResourceType.Texture2D)
            {
                GlNative.FramebufferTexture2D!(GlConstants.GL_FRAMEBUFFER,
                    GlConstants.GL_COLOR_ATTACHMENT0 + colorIndex,
                    GlConstants.GL_TEXTURE_2D, res.GlTexture, 0);
                colorIndex++;
            }

            attachments.Add(res);
        }

        var status = GlNative.CheckFramebufferStatus!(GlConstants.GL_FRAMEBUFFER);
        if (status != GlConstants.GL_FRAMEBUFFER_COMPLETE)
        {
            throw new InvalidOperationException($"帧缓冲不完整：{status}");
        }

        GlNative.BindFramebuffer!(GlConstants.GL_FRAMEBUFFER, 0);

        return new OpenGLFramebuffer(fbo, desc.Width, desc.Height, attachments, desc.RenderPass);
    }

    public IRhiSwapchain CreateSwapchain(in SwapchainDesc desc)
    {
        _swapchain = new OpenGLSwapchain(desc.WindowHandle, nint.Zero, nint.Zero, desc.Width, desc.Height);
        return _swapchain;
    }

    public IRhiFence CreateFence(bool signaled)
    {
        return new OpenGLFence();
    }

    public IRhiSemaphore CreateSemaphore()
    {
        return new OpenGLSemaphore();
    }

    public IRhiDescriptorSet CreateDescriptorSet(DescriptorSetBinding[] bindings)
    {
        return new OpenGLDescriptorSet();
    }

    public ICommandTable CreateCommandTable()
    {
        return new OpenGLCommandTable();
    }

    #endregion

    #region IDevice - 命令提交

    public void Submit(ICommandTable commandTable, IRhiFence? fence = null)
    {
        GlNative.Flush!();
    }

    public void WaitIdle()
    {
        GlNative.Finish!();
    }

    #endregion

    #region IDisposable

    public void Dispose()
    {
        if (_isDisposed)
        {
            return;
        }

        _isDisposed = true;

        foreach (var resource in _resources.Values)
        {
            resource.Dispose();
        }

        _resources.Clear();
        _swapchain?.Dispose();
    }

    #endregion
}
