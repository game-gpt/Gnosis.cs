using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Compute;

public sealed class ComputeDispatcher
{
    #region 字段

    private readonly IDevice _device;
    private readonly Dictionary<string, ComputePipeline> _pipelines;
    private readonly Dictionary<string, IResource> _constantBuffers;

    #endregion

    #region 属性

    public IReadOnlyDictionary<string, ComputePipeline> Pipelines => _pipelines;

    #endregion

    #region 构造函数

    public ComputeDispatcher(IDevice device)
    {
        _device = device;
        _pipelines = [];
        _constantBuffers = [];
    }

    #endregion

    #region 公开方法

    public ComputePipeline CreatePipeline(string name, ulong shaderHandle, uint pushConstantSize = 0)
    {
        var pipelineState = _device.CreatePipelineState(new PipelineStateDesc
        {
            ShaderHandle = shaderHandle,
            BlendMode = BlendMode.None,
            CullMode = CullMode.None,
            DepthTest = false,
            DepthWrite = false
        });

        var pipeline = new ComputePipeline(name, pipelineState, pushConstantSize);
        _pipelines[name] = pipeline;
        return pipeline;
    }

    public bool RemovePipeline(string name)
    {
        return _pipelines.Remove(name);
    }

    public ComputePipeline? GetPipeline(string name)
    {
        return _pipelines.GetValueOrDefault(name);
    }

    public void Dispatch(ICommandTable commandTable, string pipelineName, uint groupCountX, uint groupCountY = 1, uint groupCountZ = 1)
    {
        if (!_pipelines.TryGetValue(pipelineName, out var pipeline))
        {
            return;
        }

        commandTable.SetPipelineState(pipeline.PipelineState);
        commandTable.Dispatch(groupCountX, groupCountY, groupCountZ);
    }

    public void Dispatch(ICommandTable commandTable, ComputePipeline pipeline, uint groupCountX, uint groupCountY = 1, uint groupCountZ = 1)
    {
        commandTable.SetPipelineState(pipeline.PipelineState);
        commandTable.Dispatch(groupCountX, groupCountY, groupCountZ);
    }

    public static uint CalculateGroupCount(uint itemCount, uint groupSize)
    {
        return (itemCount + groupSize - 1) / groupSize;
    }

    public void Clear()
    {
        _pipelines.Clear();
        _constantBuffers.Clear();
    }

    #endregion
}

public sealed class ComputePipeline
{
    #region 属性

    public string Name { get; }
    public IPipelineState PipelineState { get; }
    public uint PushConstantSize { get; }

    #endregion

    #region 构造函数

    internal ComputePipeline(string name, IPipelineState pipelineState, uint pushConstantSize)
    {
        Name = name;
        PipelineState = pipelineState;
        PushConstantSize = pushConstantSize;
    }

    #endregion
}
