namespace Gnosis.Profiler.Sampling;

public sealed class ProfilerSampler
{
    #region 字段

    private static readonly Dictionary<string, SampleData> _samples = new();

    #endregion

    #region 内部方法

    internal static void Record(string name, double elapsedMs)
    {
        lock (_samples)
        {
            if (!_samples.TryGetValue(name, out var data))
            {
                data = new SampleData(name);
                _samples[name] = data;
            }

            data.Record(elapsedMs);
        }
    }

    #endregion

    #region 公开方法

    public static SampleData? GetSample(string name)
    {
        lock (_samples)
        {
            return _samples.TryGetValue(name, out var data) ? data : null;
        }
    }

    public static IReadOnlyList<SampleData> GetAllSamples()
    {
        lock (_samples)
        {
            return _samples.Values.ToList();
        }
    }

    public static void Reset()
    {
        lock (_samples)
        {
            _samples.Clear();
        }
    }

    #endregion
}
