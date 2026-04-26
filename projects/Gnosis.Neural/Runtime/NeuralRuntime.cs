using Gnosis.Neural.Graph;
using Gnosis.Neural.Model;

namespace Gnosis.Neural.Runtime;

/// <summary>
/// 神经推理运行时实现，支持模型生命周期管理和热加载
/// </summary>
public sealed class NeuralRuntime : INeuralRuntime
{
    #region 字段

    private readonly Dictionary<string, NeuralModel> _models = new();
    private readonly Dictionary<string, InferenceEngine> _engines = new();
    private readonly OnnxModelLoader _modelLoader = new();
    private readonly ModelHotLoader _hotLoader;
    private bool _isInitialized;

    #endregion

    #region INeuralRuntime 实现

    /// <summary>
    /// 是否已初始化
    /// </summary>
    public bool IsInitialized => _isInitialized;

    /// <summary>
    /// 获取已加载的模型名称列表
    /// </summary>
    public IReadOnlyList<string> LoadedModels => _models.Keys.ToList();

    #endregion

    #region 属性

    /// <summary>
    /// 模型热加载器
    /// </summary>
    public ModelHotLoader HotLoader => _hotLoader;

    /// <summary>
    /// 模型加载器
    /// </summary>
    public OnnxModelLoader ModelLoader => _modelLoader;

    #endregion

    #region 构造函数

    public NeuralRuntime()
    {
        _hotLoader = new ModelHotLoader(this);
    }

    #endregion

    #region INeuralRuntime 实现

    /// <summary>
    /// 加载模型
    /// </summary>
    public INeuralModel LoadModel(string modelPath, QuantizationType quantization = QuantizationType.None)
    {
        var model = _modelLoader.Load(modelPath, quantization);

        if (model is NeuralModel neuralModel)
        {
            _models[neuralModel.Name] = neuralModel;
            _hotLoader.WatchModel(neuralModel);
        }

        return model;
    }

    /// <summary>
    /// 卸载模型
    /// </summary>
    public void UnloadModel(string modelName)
    {
        if (_models.TryGetValue(modelName, out var model))
        {
            _hotLoader.UnwatchModel(modelName);

            var enginesToRemove = _engines.Where(e => e.Value.ModelName == modelName).ToList();
            foreach (var kvp in enginesToRemove)
            {
                _engines.Remove(kvp.Key);
            }

            _modelLoader.Unload(model);
            _models.Remove(modelName);
        }
    }

    /// <summary>
    /// 创建推理引擎
    /// </summary>
    public IInferenceEngine CreateEngine(INeuralModel model)
    {
        var engine = new InferenceEngine(model.Name, model);
        _engines[engine.Id] = engine;
        return engine;
    }

    /// <summary>
    /// 销毁推理引擎
    /// </summary>
    public void DestroyEngine(IInferenceEngine engine)
    {
        if (engine is InferenceEngine inferenceEngine)
        {
            _engines.Remove(inferenceEngine.Id);
        }
    }

    /// <summary>
    /// 初始化运行时
    /// </summary>
    public void Initialize()
    {
        _isInitialized = true;
    }

    /// <summary>
    /// 关闭运行时
    /// </summary>
    public void Shutdown()
    {
        _hotLoader.StopAllWatching();

        foreach (var engine in _engines.Values)
        {
            engine.Reset();
        }

        _engines.Clear();

        foreach (var model in _models.Values)
        {
            _modelLoader.Unload(model);
        }

        _models.Clear();
        _isInitialized = false;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 获取指定名称的模型
    /// </summary>
    public NeuralModel? GetModel(string modelName)
    {
        return _models.GetValueOrDefault(modelName);
    }

    /// <summary>
    /// 热替换模型（从新路径加载并替换现有模型的权重）
    /// </summary>
    public bool HotSwapModel(string modelName, string newModelPath)
    {
        if (!_models.TryGetValue(modelName, out var existingModel))
        {
            return false;
        }

        var newModel = _modelLoader.Load(newModelPath, existingModel.Quantization);
        if (newModel is not NeuralModel neuralNewModel)
        {
            return false;
        }

        existingModel.ReplaceFrom(neuralNewModel);

        foreach (var engine in _engines.Values.Where(e => e.ModelName == modelName))
        {
            engine.OnModelSwapped(existingModel);
        }

        return true;
    }

    /// <summary>
    /// 重新加载指定模型的权重（从原始路径）
    /// </summary>
    public bool ReloadModel(string modelName)
    {
        if (!_models.TryGetValue(modelName, out var model))
        {
            return false;
        }

        var success = _modelLoader.ReloadWeights(model);

        if (success)
        {
            foreach (var engine in _engines.Values.Where(e => e.ModelName == modelName))
            {
                engine.OnModelSwapped(model);
            }
        }

        return success;
    }

    /// <summary>
    /// 更新运行时（每帧调用），处理热加载检测
    /// </summary>
    public void Update(float delta)
    {
        if (_isInitialized)
        {
            _hotLoader.Update();
        }
    }

    #endregion
}
