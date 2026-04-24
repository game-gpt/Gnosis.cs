using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.RHI.OpenGL;

internal sealed unsafe class OpenGLResource : IResource
{
    private static ulong _nextId = 1;

    #region IResource 属性

    public ulong Id { get; }
    public ResourceType ResourceType { get; }
    public ResourceFormat Format { get; }
    public ulong Size { get; }
    public bool IsDisposed => _isDisposed;

    #endregion

    #region OpenGL 句柄

    public uint GlBuffer { get; }
    public uint GlTexture { get; }
    public uint GlVertexArray { get; }
    public uint GlProgram { get; }
    public uint GlShader { get; }
    public uint GlSampler { get; }
    public ShaderStage ShaderStage { get; }
    public string EntryPoint { get; } = "main";

    #endregion

    #region 内部状态

    private bool _isDisposed;

    #endregion

    #region 构造函数

    public OpenGLResource(ResourceType type, ResourceFormat format, ulong size,
        uint glBuffer = 0, uint glTexture = 0, uint glVertexArray = 0,
        uint glProgram = 0, uint glShader = 0, uint glSampler = 0, ShaderStage shaderStage = default)
    {
        Id = _nextId++;
        ResourceType = type;
        Format = format;
        Size = size;
        GlBuffer = glBuffer;
        GlTexture = glTexture;
        GlVertexArray = glVertexArray;
        GlProgram = glProgram;
        GlShader = glShader;
        GlSampler = glSampler;
        ShaderStage = shaderStage;
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

        if (GlBuffer != 0)
        {
            uint buf = GlBuffer;
            GlNative.DeleteBuffers!(1, &buf);
        }

        if (GlTexture != 0)
        {
            uint tex = GlTexture;
            GlNative.DeleteTextures!(1, &tex);
        }

        if (GlVertexArray != 0)
        {
            uint vao = GlVertexArray;
            GlNative.DeleteVertexArrays!(1, &vao);
        }

        if (GlShader != 0)
        {
            GlNative.DeleteShader!(GlShader);
        }

        if (GlProgram != 0)
        {
            GlNative.DeleteProgram!(GlProgram);
        }

        if (GlSampler != 0)
        {
            uint sam = GlSampler;
            GlNative.DeleteSamplers!(1, &sam);
        }
    }

    #endregion
}
