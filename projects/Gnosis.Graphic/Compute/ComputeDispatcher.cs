using Gnosis.Graphic.RHI;

namespace Gnosis.Graphic.Compute;

/// <summary>
/// 计算调度器，管理计算管线和分发计算任务
/// </summary>
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

    /// <summary>
    /// 创建计算管线
    /// </summary>
    /// <param name="name">管线名称</param>
    /// <param name="shaderProgram">着色器程序</param>
    /// <param name="pushConstantSize">推送常量大小</param>
    public ComputePipeline CreatePipeline(string name, IShaderProgram shaderProgram, uint pushConstantSize = 0)
    {
        var pipelineState = _device.CreatePipelineState(new PipelineStateDesc
        {
            Shader = shaderProgram,
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
        if (_pipelines.TryGetValue(name, out var pipeline))
        {
            pipeline.PipelineState.Dispose();
            return _pipelines.Remove(name);
        }

        return false;
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

    /// <summary>
    /// 计算工作组数量
    /// </summary>
    /// <param name="itemCount">元素数量</param>
    /// <param name="groupSize">工作组大小</param>
    public static uint CalculateGroupCount(uint itemCount, uint groupSize)
    {
        return (itemCount + groupSize - 1) / groupSize;
    }

    public void Clear()
    {
        foreach (var pipeline in _pipelines.Values)
        {
            pipeline.PipelineState.Dispose();
        }

        _pipelines.Clear();
        _constantBuffers.Clear();
    }

    #endregion
}

/// <summary>
/// 计算管线，封装管线状态和推送常量信息
/// </summary>
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
