using Gnosis.Core;

namespace Gnosis.Security;

/// <summary>
/// 反作弊系统，管理完整性检查器并检测违规行为
/// </summary>
public sealed class AntiCheatSystem : IAntiCheatSystem
{
    #region 字段

    private readonly Dictionary<string, IIntegrityChecker> _checkers = new();
    private bool _isInitialized;
    private bool _isCompromised;

    #endregion

    #region 属性

    /// <summary>
    /// 获取系统是否已被破坏，一旦设置为 true 将不会重置
    /// </summary>
    public bool IsCompromised => _isCompromised;

    #endregion

    #region 事件

    /// <summary>
    /// 当检测到违规行为时触发
    /// </summary>
    public event EventHandler<ViolationEventArgs>? OnViolation;

    #endregion

    #region 公开方法

    /// <summary>
    /// 初始化反作弊系统
    /// </summary>
    public void Initialize()
    {
        _isInitialized = true;
    }

    /// <summary>
    /// 关闭反作弊系统，清除所有已注册的检查器
    /// </summary>
    public void Shutdown()
    {
        _isInitialized = false;
        _checkers.Clear();
    }

    /// <summary>
    /// 注册完整性检查器，若同名检查器已存在则抛出 <see cref="SecurityException"/>
    /// </summary>
    /// <param name="checker">要注册的完整性检查器</param>
    /// <exception cref="SecurityException">当同名检查器已存在时抛出</exception>
    public void RegisterChecker(IIntegrityChecker checker)
    {
        if (_checkers.ContainsKey(checker.Name))
        {
            throw new SecurityException($"检查器 '{checker.Name}' 已存在");
        }

        _checkers[checker.Name] = checker;
    }

    /// <summary>
    /// 执行指定名称的完整性检查，若检查失败则标记系统为已破坏并触发违规事件
    /// </summary>
    /// <param name="checkerName">检查器名称</param>
    /// <returns>检查通过返回 true，否则返回 false</returns>
    /// <exception cref="SecurityException">当指定名称的检查器不存在时抛出</exception>
    public bool PerformCheck(string checkerName)
    {
        if (!_checkers.TryGetValue(checkerName, out var checker))
        {
            throw new SecurityException($"检查器 '{checkerName}' 不存在");
        }

        var result = checker.Check();

        if (!result)
        {
            _isCompromised = true;

            var args = new ViolationEventArgs(
                ViolationType.IntegrityCheckFailed,
                ViolationResponse.Log,
                $"完整性检查 '{checkerName}' 未通过",
                null);

            OnViolation?.Invoke(this, args);
        }

        return result;
    }

    #endregion
}
