using System.Runtime.InteropServices;
using Gnosis.Graphic.Pipeline;
using Gnosis.Graphic.RHI;
using Gnosis.Graphic.Shader;

namespace Gnosis.Graphic.UI;

public sealed class UIRenderPass : IRenderPass, IDisposable
{
    #region 属性

    public string Name => "UI_RenderPass";
    public bool Enabled { get; set; } = true;

    #endregion

    #region 内部状态

    private readonly GpuWidgetRenderer _renderer;
    private IResource? _vertexBuffer;
    private IResource? _indexBuffer;
    private IPipelineState? _pipelineState;
    private bool _isDisposed;

    #endregion

    #region 构造函数

    public UIRenderPass(GpuWidgetRenderer renderer)
    {
        _renderer = renderer;
    }

    #endregion

    #region IRenderPass 实现

    public void Execute(RenderContext context, ICommandTable commandTable)
    {
        if (!Enabled || context.Device == null)
        {
            return;
        }

        var (vertices, indices) = _renderer.GetGpuData();

        if (vertices.Length == 0 || indices.Length == 0)
        {
            return;
        }

        EnsurePipelineState(context.Device);

        UploadBuffers(context.Device, vertices, indices);

        commandTable.SetViewport(0, 0, context.Width, context.Height);

        if (_pipelineState != null)
        {
            commandTable.SetPipelineState(_pipelineState);
        }

        if (_vertexBuffer != null)
        {
            commandTable.SetVertexBuffer(_vertexBuffer);
        }

        if (_indexBuffer != null)
        {
            commandTable.SetIndexBuffer(_indexBuffer);
            commandTable.DrawIndexed((uint)indices.Length);
        }
    }

    #endregion

    #region 公开方法

    public (byte[] vertexData, byte[] indexData) GetRawBufferData()
    {
        var (vertices, indices) = _renderer.GetGpuData();
        var vertexData = MemoryMarshal.AsBytes<UIVertex>(vertices);
        var indexData = MemoryMarshal.AsBytes(indices);
        return (vertexData.ToArray(), indexData.ToArray());
    }

    #endregion

    #region 私有方法

    private void EnsurePipelineState(IDevice device)
    {
        if (_pipelineState != null)
        {
            return;
        }

        var uiShaderModule = BuiltinShaderModules.CreateUiShader();

        if (uiShaderModule.Bytecode is { Length: > 0 })
        {
            var vertexShader = device.CreateShader(new ShaderDesc
            {
                Bytecode = uiShaderModule.Bytecode,
                Stage = ShaderStage.Vertex,
                EntryPoint = "vs_main",
                IsSpirv = true
            });

            var fragmentShader = device.CreateShader(new ShaderDesc
            {
                Bytecode = uiShaderModule.Bytecode,
                Stage = ShaderStage.Fragment,
                EntryPoint = "fs_main",
                IsSpirv = true
            });

            var shaderProgram = new UiShaderProgram(vertexShader, fragmentShader);

            _pipelineState = device.CreatePipelineState(new PipelineStateDesc
            {
                Shader = shaderProgram,
                PipelineType = PipelineType.Graphics,
                Topology = PrimitiveTopology.TriangleList,
                BlendMode = BlendMode.Alpha,
                DepthTest = false,
                DepthWrite = false,
                CullMode = CullMode.None,
                ColorAttachmentCount = 1,
                ColorFormats = [ResourceFormat.R8G8B8A8Unorm]
            });
        }
    }

    private void UploadBuffers(IDevice device, UIVertex[] vertices, uint[] indices)
    {
        var vertexData = MemoryMarshal.AsBytes<UIVertex>(vertices);
        _vertexBuffer?.Dispose();
        _vertexBuffer = device.CreateBuffer(new BufferDesc
        {
            Size = (ulong)vertexData.Length,
            Usage = BufferUsage.VertexBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = vertexData.ToArray()
        });

        var indexData = MemoryMarshal.AsBytes(indices);
        _indexBuffer?.Dispose();
        _indexBuffer = device.CreateBuffer(new BufferDesc
        {
            Size = (ulong)indexData.Length,
            Usage = BufferUsage.IndexBuffer | BufferUsage.TransferDst,
            HostVisible = true,
            InitialData = indexData.ToArray()
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

        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
        _pipelineState?.Dispose();

        _isDisposed = true;
    }

    #endregion

    private sealed class UiShaderProgram : IShaderProgram
    {
        private readonly IResource _vertexShader;
        private readonly IResource _fragmentShader;

        public string Name => "ui_uber";
        public ShaderStageFlags Stages => ShaderStageFlags.Vertex | ShaderStageFlags.Fragment;

        public UiShaderProgram(IResource vertexShader, IResource fragmentShader)
        {
            _vertexShader = vertexShader;
            _fragmentShader = fragmentShader;
        }

        public void Dispose()
        {
            _vertexShader.Dispose();
            _fragmentShader.Dispose();
        }
    }
}
