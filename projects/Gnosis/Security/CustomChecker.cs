using Gnosis.Core;
using Gnosis.Infrastructure;

namespace Gnosis.Security;

/// <summary>
/// 自定义检测器，允许用户定义自定义的安全检测逻辑
/// </summary>
public sealed class CustomChecker : IIntegrityChecker
{
    #region 字段

    private readonly Func<bool> _checkFunc;
    private readonly double _intervalSeconds;
    private Timestamp _lastCheckTime;
    private Timestamp _lastExecuteTime;

    #endregion

    #region 属性

    /// <summary>
    /// 检查器名称
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// 上次检查的时间戳
    /// </summary>
    public Timestamp LastCheckTime => _lastCheckTime;

    /// <summary>
    /// 检测间隔（秒）
    /// </summary>
    public double IntervalSeconds => _intervalSeconds;

    #endregion

    #region 构造函数

    /// <summary>
    /// 初始化自定义检测器
    /// </summary>
    /// <param name="name">检查器名称</param>
    /// <param name="checkFunc">自定义检测函数，返回 true 表示通过</param>
    /// <param name="intervalSeconds">检测间隔（秒），默认 5.0</param>
    public CustomChecker(string name, Func<bool> checkFunc, double intervalSeconds = 5.0)
    {
        Name = name;
        _checkFunc = checkFunc;
        _intervalSeconds = intervalSeconds;
        _lastCheckTime = default;
        _lastExecuteTime = default;
    }

    #endregion

    #region 公开方法

    /// <summary>
    /// 执行自定义检测，如果未到达检测间隔则返回上次的检测结果
    /// </summary>
    /// <returns>检测通过返回 true，否则返回 false</returns>
    public bool Check()
    {
        _lastCheckTime = Timestamp.Now;
        return _checkFunc();
    }

    #endregion
}
