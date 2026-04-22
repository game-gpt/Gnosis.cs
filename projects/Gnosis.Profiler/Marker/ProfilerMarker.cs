using System.Diagnostics;
using Gnosis.Profiler.Sampling;

namespace Gnosis.Profiler.Marker;

public sealed class ProfilerMarker : IDisposable
{
    #region 字段

    private readonly string _name;
    private readonly long _startTimestamp;
    private bool _disposed;

    #endregion

    #region 属性

    public string Name => _name;

    public double ElapsedMilliseconds => (_disposed ? Stopwatch.GetTimestamp() - _startTimestamp : 0) * 1000.0 / Stopwatch.Frequency;

    #endregion

    #region 构造函数

    public ProfilerMarker(string name)
    {
        _name = name ?? throw new ArgumentNullException(nameof(name));
        _startTimestamp = Stopwatch.GetTimestamp();
    }

    #endregion

    #region IDisposable 实现

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        var elapsed = (Stopwatch.GetTimestamp() - _startTimestamp) * 1000.0 / Stopwatch.Frequency;
        ProfilerSampler.Record(_name, elapsed);
    }

    #endregion
}
