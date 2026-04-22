using System;
using System.Collections.Generic;

namespace Gnosis.Network.Prediction;

/// <summary>
/// 确定性执行器，确保帧同步逻辑在不同客户端上产生一致的结果
/// </summary>
public sealed class DeterministicExecutor
{
    #region 字段

    private readonly Dictionary<string, IDeterministicFunction> _functions = new();
    private readonly Stack<DeterministicRandom> _randomStack = new();
    private DeterministicRandom? _currentRandom;
    private readonly List<ExecutionRecord> _executionLog = [];
    private bool _loggingEnabled;
    private int _maxLogSize = 1024;

    #endregion

    #region 属性

    /// <summary>
    /// 获取或设置是否启用执行日志
    /// </summary>
    public bool LoggingEnabled
    {
        get => _loggingEnabled;
        set => _loggingEnabled = value;
    }

    /// <summary>
    /// 获取执行日志记录数量
    /// </summary>
    public int LogCount => _executionLog.Count;

    /// <summary>
    /// 获取当前随机数生成器的种子
    /// </summary>
    public int CurrentSeed => _currentRandom?.Seed ?? 0;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化确定性执行器
    /// </summary>
    /// <param name="seed">随机数种子</param>
    public DeterministicExecutor(int seed = 0)
    {
        _currentRandom = new DeterministicRandom(seed);
    }

    #endregion

    #region 公共方法

    /// <summary>
    /// 注册确定性函数
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <param name="function">确定性函数实例</param>
    public void RegisterFunction(string name, IDeterministicFunction function)
    {
        _functions[name] = function;
    }

    /// <summary>
    /// 注销确定性函数
    /// </summary>
    /// <param name="name">函数名称</param>
    public void UnregisterFunction(string name)
    {
        _functions.Remove(name);
    }

    /// <summary>
    /// 执行指定的确定性函数
    /// </summary>
    /// <param name="name">函数名称</param>
    /// <param name="frame">当前帧号</param>
    /// <param name="input">输入数据</param>
    /// <returns>执行结果</returns>
    public byte[] Execute(string name, int frame, byte[] input)
    {
        if (!_functions.TryGetValue(name, out var function))
        {
            throw new InvalidOperationException($"确定性函数未注册：{name}");
        }

        var result = function.Execute(frame, input, _currentRandom!);

        if (_loggingEnabled)
        {
            RecordExecution(name, frame, input, result);
        }

        return result;
    }

    /// <summary>
    /// 生成确定性随机整数
    /// </summary>
    /// <param name="min">最小值（含）</param>
    /// <param name="max">最大值（不含）</param>
    /// <returns>随机整数</returns>
    public int RandomInt(int min, int max)
    {
        return _currentRandom!.NextInt(min, max);
    }

    /// <summary>
    /// 生成确定性随机浮点数 [0, 1)
    /// </summary>
    /// <returns>随机浮点数</returns>
    public float RandomFloat()
    {
        return _currentRandom!.NextFloat();
    }

    /// <summary>
    /// 保存当前随机状态（用于回滚）
    /// </summary>
    public void PushRandomState()
    {
        _randomStack.Push(_currentRandom!.Clone());
    }

    /// <summary>
    /// 恢复之前的随机状态（用于回滚）
    /// </summary>
    public void PopRandomState()
    {
        if (_randomStack.Count > 0)
        {
            _currentRandom = _randomStack.Pop();
        }
    }

    /// <summary>
    /// 重置随机数生成器
    /// </summary>
    /// <param name="seed">新种子</param>
    public void ResetRandom(int seed)
    {
        _currentRandom = new DeterministicRandom(seed);
        _randomStack.Clear();
    }

    /// <summary>
    /// 计算当前状态的哈希值
    /// </summary>
    /// <returns>状态哈希</returns>
    public int CalculateStateHash()
    {
        var hash = new HashCode();

        foreach (var (name, function) in _functions)
        {
            hash.Add(name);
            hash.Add(function.GetStateHash());
        }

        return hash.ToHashCode();
    }

    /// <summary>
    /// 获取执行日志
    /// </summary>
    /// <returns>执行日志的只读列表</returns>
    public IReadOnlyList<ExecutionRecord> GetExecutionLog()
    {
        return _executionLog.AsReadOnly();
    }

    /// <summary>
    /// 清除执行日志
    /// </summary>
    public void ClearLog()
    {
        _executionLog.Clear();
    }

    #endregion

    #region 私有方法

    /// <summary>
    /// 记录执行日志
    /// </summary>
    private void RecordExecution(string name, int frame, byte[] input, byte[] result)
    {
        _executionLog.Add(new ExecutionRecord(name, frame, input, result));

        if (_executionLog.Count > _maxLogSize)
        {
            _executionLog.RemoveAt(0);
        }
    }

    #endregion
}
