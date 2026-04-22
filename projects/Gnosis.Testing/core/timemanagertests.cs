using Gnosis.Core;
using NUnit.Framework;

namespace Gnosis.Testing.Core;

[TestFixture]
public class TimeManagerTests
{
    private TimeManager _timeManager = null!;

    [SetUp]
    public void SetUp()
    {
        _timeManager = new TimeManager();
    }

    #region 帧时间追踪

    [Test]
    public void BeginFrame_IncrementsFrameCount()
    {
        Assert.That(_timeManager.FrameCount, Is.EqualTo(0));

        _timeManager.BeginFrame();
        Assert.That(_timeManager.FrameCount, Is.EqualTo(1));

        _timeManager.BeginFrame();
        Assert.That(_timeManager.FrameCount, Is.EqualTo(2));
    }

    [Test]
    public void BeginFrame_UpdatesDeltaTime()
    {
        _timeManager.BeginFrame();

        Thread.Sleep(16);

        _timeManager.BeginFrame();

        Assert.That(_timeManager.DeltaTime, Is.GreaterThan(0));
        Assert.That(_timeManager.UnscaledDeltaTime, Is.GreaterThan(0));
    }

    [Test]
    public void BeginFrame_UpdatesTotalTime()
    {
        _timeManager.BeginFrame();

        Thread.Sleep(16);

        _timeManager.BeginFrame();

        Assert.That(_timeManager.TotalTime, Is.GreaterThan(0));
        Assert.That(_timeManager.UnscaledTotalTime, Is.GreaterThan(0));
    }

    [Test]
    public void DeltaTime_ApplyTimeScale()
    {
        _timeManager.TimeScale = 0.5f;

        _timeManager.BeginFrame();

        Thread.Sleep(20);

        _timeManager.BeginFrame();

        Assert.That(_timeManager.DeltaTime, Is.LessThan(_timeManager.UnscaledDeltaTime));
        Assert.That(_timeManager.DeltaTime, Is.EqualTo(_timeManager.UnscaledDeltaTime * 0.5f).Within(0.001));
    }

    [Test]
    public void TotalTime_UsesScaledDelta()
    {
        _timeManager.TimeScale = 0.5f;

        _timeManager.BeginFrame();
        Thread.Sleep(20);
        _timeManager.BeginFrame();
        Thread.Sleep(20);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.TotalTime, Is.LessThan(_timeManager.UnscaledTotalTime));
    }

    #endregion

    #region 固定时间步

    [Test]
    public void FixedDeltaTime_DefaultValue()
    {
        Assert.That(_timeManager.FixedDeltaTime, Is.EqualTo(1f / 60f).Within(0.0001f));
    }

    [Test]
    public void FixedDeltaTime_CanBeSet()
    {
        _timeManager.FixedDeltaTime = 1f / 30f;
        Assert.That(_timeManager.FixedDeltaTime, Is.EqualTo(1f / 30f).Within(0.0001f));
    }

    [Test]
    public void FixedFrameCount_IncrementedByMethod()
    {
        Assert.That(_timeManager.FixedFrameCount, Is.EqualTo(0));

        _timeManager.IncrementFixedFrameCount();
        Assert.That(_timeManager.FixedFrameCount, Is.EqualTo(1));

        _timeManager.IncrementFixedFrameCount();
        Assert.That(_timeManager.FixedFrameCount, Is.EqualTo(2));
    }

    #endregion

    #region 时间缩放

    [Test]
    public void TimeScale_DefaultIsOne()
    {
        Assert.That(_timeManager.TimeScale, Is.EqualTo(1f));
    }

    [Test]
    public void TimeScale_CanBeSet()
    {
        _timeManager.TimeScale = 2f;
        Assert.That(_timeManager.TimeScale, Is.EqualTo(2f));
    }

    [Test]
    public void TimeScale_NegativeThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _timeManager.TimeScale = -1f);
    }

    [Test]
    public void TimeScale_ZeroStopsScaledTime()
    {
        _timeManager.TimeScale = 0f;

        _timeManager.BeginFrame();
        Thread.Sleep(20);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.DeltaTime, Is.EqualTo(0f));
        Assert.That(_timeManager.UnscaledDeltaTime, Is.GreaterThan(0f));
    }

    #endregion

    #region 帧率控制

    [Test]
    public void TargetFrameRate_DefaultIsZero()
    {
        Assert.That(_timeManager.TargetFrameRate, Is.EqualTo(0));
    }

    [Test]
    public void TargetFrameRate_CanBeSet()
    {
        _timeManager.TargetFrameRate = 60;
        Assert.That(_timeManager.TargetFrameRate, Is.EqualTo(60));
    }

    [Test]
    public void TargetFrameRate_NegativeThrowsException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _timeManager.TargetFrameRate = -1);
    }

    [Test]
    public void WaitForTargetFrameRate_ZeroDoesNotWait()
    {
        _timeManager.TargetFrameRate = 0;

        Assert.DoesNotThrow(() => _timeManager.WaitForTargetFrameRate());
    }

    #endregion

    #region 重置

    [Test]
    public void Reset_ClearsAllState()
    {
        _timeManager.BeginFrame();
        Thread.Sleep(10);
        _timeManager.BeginFrame();
        _timeManager.IncrementFixedFrameCount();
        _timeManager.TimeScale = 2f;

        _timeManager.Reset();

        Assert.That(_timeManager.FrameCount, Is.EqualTo(0));
        Assert.That(_timeManager.FixedFrameCount, Is.EqualTo(0));
        Assert.That(_timeManager.TotalTime, Is.EqualTo(0f));
        Assert.That(_timeManager.UnscaledTotalTime, Is.EqualTo(0f));
        Assert.That(_timeManager.TimeScale, Is.EqualTo(1f));
        Assert.That(_timeManager.TargetFrameRate, Is.EqualTo(0));
        Assert.That(_timeManager.FixedDeltaTime, Is.EqualTo(1f / 60f).Within(0.0001f));
    }

    #endregion
}
