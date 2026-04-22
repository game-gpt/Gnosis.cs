using Gnosis.Core.Time;

namespace Gnosis.Security.Integrity;

public sealed class CustomChecker : IIntegrityChecker
{
    #region 字段

    private readonly Func<bool> _checkFunc;
    private readonly double _intervalSeconds;
    private Timestamp _lastCheckTime;
    private Timestamp _lastExecuteTime;

    #endregion

    #region 属性

    public string Name { get; }
    public Timestamp LastCheckTime => _lastCheckTime;
    public double IntervalSeconds => _intervalSeconds;

    #endregion

    #region 构造函数

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

    public bool Check()
    {
        _lastCheckTime = Timestamp.Now;
        return _checkFunc();
    }

    #endregion
}
