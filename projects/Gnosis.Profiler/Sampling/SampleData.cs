namespace Gnosis.Profiler.Sampling;

public sealed class SampleData
{
    #region 字段

    private double _totalMs;
    private double _minMs = double.MaxValue;
    private double _maxMs = double.MinValue;
    private int _count;

    #endregion

    #region 属性

    public string Name { get; }

    public int Count => _count;

    public double TotalMs => _totalMs;

    public double AverageMs => _count > 0 ? _totalMs / _count : 0;

    public double MinMs => _count > 0 ? _minMs : 0;

    public double MaxMs => _count > 0 ? _maxMs : 0;

    #endregion

    #region 构造函数

    internal SampleData(string name)
    {
        Name = name;
    }

    #endregion

    #region 内部方法

    internal void Record(double elapsedMs)
    {
        _count++;
        _totalMs += elapsedMs;

        if (elapsedMs < _minMs)
        {
            _minMs = elapsedMs;
        }

        if (elapsedMs > _maxMs)
        {
            _maxMs = elapsedMs;
        }
    }

    #endregion
}
