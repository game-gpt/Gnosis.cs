namespace Gnosis.Interpreter.VM;

/// <summary>
/// 原生函数注册表
/// </summary>
public class NativeFunctionRegistry : INativeFunctionRegistry
{
    #region Fields

    private readonly Dictionary<int, INativeFunction> _functionsById = new();
    private readonly Dictionary<string, INativeFunction> _functionsByName = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// 注册原生函数
    /// </summary>
    public void Register(INativeFunction function)
    {
        if (_functionsById.ContainsKey(function.Id))
        {
            throw new VMDuplicateFunctionException(function.Id);
        }

        _functionsById[function.Id] = function;
        _functionsByName[function.Name] = function;
    }

    /// <summary>
    /// 按 ID 注销原生函数
    /// </summary>
    public void Unregister(int id)
    {
        if (_functionsById.TryGetValue(id, out var function))
        {
            _functionsById.Remove(id);
            _functionsByName.Remove(function.Name);
        }
    }

    /// <summary>
    /// 按名称注销原生函数
    /// </summary>
    public void Unregister(string name)
    {
        if (_functionsByName.TryGetValue(name, out var function))
        {
            _functionsByName.Remove(name);
            _functionsById.Remove(function.Id);
        }
    }

    /// <summary>
    /// 按 ID 查找原生函数
    /// </summary>
    public INativeFunction? Get(int id)
    {
        return _functionsById.TryGetValue(id, out var function) ? function : null;
    }

    /// <summary>
    /// 按名称查找原生函数
    /// </summary>
    public INativeFunction? Get(string name)
    {
        return _functionsByName.TryGetValue(name, out var function) ? function : null;
    }

    /// <summary>
    /// 检查指定 ID 的原生函数是否已注册
    /// </summary>
    public bool Contains(int id)
    {
        return _functionsById.ContainsKey(id);
    }

    /// <summary>
    /// 检查指定名称的原生函数是否已注册
    /// </summary>
    public bool Contains(string name)
    {
        return _functionsByName.ContainsKey(name);
    }

    /// <summary>
    /// 获取所有已注册的原生函数
    /// </summary>
    public IReadOnlyList<INativeFunction> GetAll()
    {
        return _functionsById.Values.ToList();
    }

    /// <summary>
    /// 清空所有已注册的原生函数
    /// </summary>
    public void Clear()
    {
        _functionsById.Clear();
        _functionsByName.Clear();
    }

    #endregion
}
