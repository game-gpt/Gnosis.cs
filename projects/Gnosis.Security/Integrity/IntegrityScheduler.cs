using Gnosis.Core;

namespace Gnosis.Security.Integrity;

public sealed class IntegrityScheduler
{
    #region 字段

    private readonly List<ScheduledCheck> _scheduledChecks = new();
    private readonly Random _random = new();

    #endregion

    #region 属性

    public int ScheduledCheckCount => _scheduledChecks.Count;

    #endregion

    #region 事件

    public event Action<string, string>? OnIntegrityViolation;

    #endregion

    #region 公开方法

    public void AddCheck(IIntegrityChecker checker, double intervalSeconds, double randomOffsetSeconds = 0)
    {
        var nextCheckTime = Environment.TickCount64 + (long)(intervalSeconds * 1000);
        _scheduledChecks.Add(new ScheduledCheck(checker, intervalSeconds, randomOffsetSeconds, nextCheckTime));
    }

    public bool Tick()
    {
        var allPassed = true;
        var currentTime = Environment.TickCount64;

        foreach (var check in _scheduledChecks)
        {
            if (currentTime < check.NextCheckTimeMs) continue;

            var result = check.Checker.Check();

            if (!result)
            {
                allPassed = false;
                OnIntegrityViolation?.Invoke(check.Checker.Name, $"完整性检查 '{check.Checker.Name}' 未通过");
            }

            var offset = check.RandomOffsetSeconds > 0
                ? _random.NextDouble() * check.RandomOffsetSeconds * 1000
                : 0;
            check.NextCheckTimeMs = currentTime + (long)(check.IntervalSeconds * 1000 + offset);
        }

        return allPassed;
    }

    public void RemoveCheck(string checkerName) => _scheduledChecks.RemoveAll(c => c.Checker.Name == checkerName);

    public void Clear() => _scheduledChecks.Clear();

    #endregion

    private sealed class ScheduledCheck
    {
        public IIntegrityChecker Checker { get; }
        public double IntervalSeconds { get; }
        public double RandomOffsetSeconds { get; }
        public long NextCheckTimeMs { get; set; }

        public ScheduledCheck(IIntegrityChecker checker, double intervalSeconds, double randomOffsetSeconds, long nextCheckTimeMs)
        {
            Checker = checker;
            IntervalSeconds = intervalSeconds;
            RandomOffsetSeconds = randomOffsetSeconds;
            NextCheckTimeMs = nextCheckTimeMs;
        }
    }
}
