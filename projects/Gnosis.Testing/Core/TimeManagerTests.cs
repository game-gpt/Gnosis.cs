using System.Threading;
using Gnosis.Core;
using NUnit.Framework;

namespace Gnosis.Testing.Core;

[TestFixture]
public class TimeManagerTests
{
    private TimeManager _timeManager = null!;

    [SetUp]
    public void Setup()
    {
        _timeManager = new TimeManager();
    }

    #region 初始状态测试

    [Test]
    public void 初始状态_DeltaTime为零()
    {
        Assert.That(_timeManager.DeltaTime, Is.EqualTo(0f));
    }

    [Test]
    public void 初始状态_UnscaledDeltaTime为零()
    {
        Assert.That(_timeManager.UnscaledDeltaTime, Is.EqualTo(0f));
    }

    [Test]
    public void 初始状态_TotalTime为零()
    {
        Assert.That(_timeManager.TotalTime, Is.EqualTo(0f));
    }

    [Test]
    public void 初始状态_UnscaledTotalTime为零()
    {
        Assert.That(_timeManager.UnscaledTotalTime, Is.EqualTo(0f));
    }

    [Test]
    public void 初始状态_FrameCount为零()
    {
        Assert.That(_timeManager.FrameCount, Is.EqualTo(0));
    }

    [Test]
    public void 初始状态_FixedFrameCount为零()
    {
        Assert.That(_timeManager.FixedFrameCount, Is.EqualTo(0));
    }

    [Test]
    public void 初始状态_TimeScale为1()
    {
        Assert.That(_timeManager.TimeScale, Is.EqualTo(1f));
    }

    [Test]
    public void 初始状态_TargetFrameRate为0()
    {
        Assert.That(_timeManager.TargetFrameRate, Is.EqualTo(0));
    }

    [Test]
    public void 初始状态_FixedDeltaTime为1除60()
    {
        Assert.That(_timeManager.FixedDeltaTime, Is.EqualTo(1f / 60f).Within(0.0001f));
    }

    #endregion

    #region BeginFrame 测试

    [Test]
    public void BeginFrame_递增FrameCount()
    {
        _timeManager.BeginFrame();
        Assert.That(_timeManager.FrameCount, Is.EqualTo(1));

        _timeManager.BeginFrame();
        Assert.That(_timeManager.FrameCount, Is.EqualTo(2));
    }

    [Test]
    public void BeginFrame_计算UnscaledDeltaTime()
    {
        Thread.Sleep(16);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.UnscaledDeltaTime, Is.GreaterThan(0f));
    }

    [Test]
    public void BeginFrame_DeltaTime等于UnscaledDeltaTime乘TimeScale()
    {
        Thread.Sleep(16);
        _timeManager.BeginFrame();

        var expected = _timeManager.UnscaledDeltaTime * _timeManager.TimeScale;
        Assert.That(_timeManager.DeltaTime, Is.EqualTo(expected).Within(0.0001f));
    }

    [Test]
    public void BeginFrame_累加UnscaledTotalTime()
    {
        Thread.Sleep(16);
        _timeManager.BeginFrame();
        var firstTotal = _timeManager.UnscaledTotalTime;

        Thread.Sleep(16);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.UnscaledTotalTime, Is.GreaterThan(firstTotal));
    }

    [Test]
    public void BeginFrame_累加TotalTime()
    {
        Thread.Sleep(16);
        _timeManager.BeginFrame();
        var firstTotal = _timeManager.TotalTime;

        Thread.Sleep(16);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.TotalTime, Is.GreaterThan(firstTotal));
    }

    #endregion

    #region 时间缩放测试

    [Test]
    public void TimeScale_设为2时_DeltaTime翻倍()
    {
        _timeManager.TimeScale = 2f;
        Thread.Sleep(16);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.DeltaTime, Is.EqualTo(_timeManager.UnscaledDeltaTime * 2f).Within(0.0001f));
    }

    [Test]
    public void TimeScale_设为0时_DeltaTime为零()
    {
        _timeManager.TimeScale = 0f;
        Thread.Sleep(16);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.DeltaTime, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(_timeManager.UnscaledDeltaTime, Is.GreaterThan(0f));
    }

    [Test]
    public void TimeScale_设为0时_TotalTime不增长()
    {
        _timeManager.TimeScale = 0f;
        Thread.Sleep(16);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.TotalTime, Is.EqualTo(0f).Within(0.0001f));
        Assert.That(_timeManager.UnscaledTotalTime, Is.GreaterThan(0f));
    }

    [Test]
    public void TimeScale_设为负数时_抛出异常()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _timeManager.TimeScale = -1f);
    }

    [Test]
    public void TimeScale_设为0点5时_DeltaTime减半()
    {
        _timeManager.TimeScale = 0.5f;
        Thread.Sleep(16);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.DeltaTime, Is.EqualTo(_timeManager.UnscaledDeltaTime * 0.5f).Within(0.0001f));
    }

    #endregion

    #region 固定时间步测试

    [Test]
    public void FixedDeltaTime_可修改()
    {
        _timeManager.FixedDeltaTime = 1f / 30f;
        Assert.That(_timeManager.FixedDeltaTime, Is.EqualTo(1f / 30f).Within(0.0001f));
    }

    [Test]
    public void IncrementFixedFrameCount_递增FixedFrameCount()
    {
        _timeManager.IncrementFixedFrameCount();
        Assert.That(_timeManager.FixedFrameCount, Is.EqualTo(1));

        _timeManager.IncrementFixedFrameCount();
        Assert.That(_timeManager.FixedFrameCount, Is.EqualTo(2));
    }

    #endregion

    #region 帧率控制测试

    [Test]
    public void TargetFrameRate_设为负数时_抛出异常()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _timeManager.TargetFrameRate = -1);
    }

    [Test]
    public void TargetFrameRate_可修改()
    {
        _timeManager.TargetFrameRate = 60;
        Assert.That(_timeManager.TargetFrameRate, Is.EqualTo(60));
    }

    [Test]
    public void WaitForTargetFrameRate_目标帧率为0时_不等待()
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _timeManager.WaitForTargetFrameRate();
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.LessThan(5));
    }

    [Test]
    public void WaitForTargetFrameRate_帧提前完成时_等待剩余时间()
    {
        _timeManager.TargetFrameRate = 30;
        _timeManager.BeginFrame();

        Thread.Sleep(10);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        _timeManager.WaitForTargetFrameRate();
        sw.Stop();

        Assert.That(sw.ElapsedMilliseconds, Is.GreaterThan(10));
    }

    #endregion

    #region Reset 测试

    [Test]
    public void Reset_重置所有时间状态()
    {
        Thread.Sleep(16);
        _timeManager.BeginFrame();
        _timeManager.IncrementFixedFrameCount();
        _timeManager.TimeScale = 2f;
        _timeManager.TargetFrameRate = 60;
        _timeManager.FixedDeltaTime = 1f / 30f;

        _timeManager.Reset();

        Assert.That(_timeManager.DeltaTime, Is.EqualTo(0f));
        Assert.That(_timeManager.UnscaledDeltaTime, Is.EqualTo(0f));
        Assert.That(_timeManager.TotalTime, Is.EqualTo(0f));
        Assert.That(_timeManager.UnscaledTotalTime, Is.EqualTo(0f));
        Assert.That(_timeManager.FrameCount, Is.EqualTo(0));
        Assert.That(_timeManager.FixedFrameCount, Is.EqualTo(0));
        Assert.That(_timeManager.TimeScale, Is.EqualTo(1f));
        Assert.That(_timeManager.TargetFrameRate, Is.EqualTo(0));
        Assert.That(_timeManager.FixedDeltaTime, Is.EqualTo(1f / 60f).Within(0.0001f));
    }

    [Test]
    public void Reset后_BeginFrame正常工作()
    {
        Thread.Sleep(16);
        _timeManager.BeginFrame();

        _timeManager.Reset();

        Thread.Sleep(16);
        _timeManager.BeginFrame();

        Assert.That(_timeManager.FrameCount, Is.EqualTo(1));
        Assert.That(_timeManager.UnscaledDeltaTime, Is.GreaterThan(0f));
        Assert.That(_timeManager.TotalTime, Is.GreaterThan(0f));
    }

    #endregion

    #region 多帧累计测试

    [Test]
    public void 多帧累计_TotalTime接近UnscaledTotalTime()
    {
        for (var i = 0; i < 5; i++)
        {
            Thread.Sleep(10);
            _timeManager.BeginFrame();
        }

        Assert.That(_timeManager.TotalTime, Is.EqualTo(_timeManager.UnscaledTotalTime).Within(0.01f));
    }

    [Test]
    public void 多帧累计_TimeScale为2时_TotalTime为UnscaledTotalTime两倍()
    {
        _timeManager.TimeScale = 2f;

        for (var i = 0; i < 5; i++)
        {
            Thread.Sleep(10);
            _timeManager.BeginFrame();
        }

        Assert.That(_timeManager.TotalTime, Is.EqualTo(_timeManager.UnscaledTotalTime * 2f).Within(0.05f));
    }

    #endregion
}
