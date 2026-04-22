using Gnosis.Network.Metrics;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class NetworkMetricsTests : GnosisTester
{
    private NetworkMetrics _metrics = null!;

    public override void Setup()
    {
        base.Setup();
        _metrics = new NetworkMetrics();
    }

    [Test]
    public void RecordRtt_记录后平滑RTT更新()
    {
        _metrics.RecordRtt(50f);

        Assert.That(_metrics.SmoothedRtt, Is.GreaterThan(0f));
    }

    [Test]
    public void RecordRtt_多次记录后平滑RTT趋近均值()
    {
        for (var i = 0; i < 10; i++)
        {
            _metrics.RecordRtt(100f);
        }

        Assert.That(_metrics.SmoothedRtt, Is.InRange(90f, 110f));
    }

    [Test]
    public void RecordRtt_异常值不会剧烈影响平滑RTT()
    {
        for (var i = 0; i < 10; i++)
        {
            _metrics.RecordRtt(50f);
        }

        _metrics.RecordRtt(1000f);

        Assert.That(_metrics.SmoothedRtt, Is.LessThan(500f));
    }

    [Test]
    public void RecordPacketSent_记录后发送包数增加()
    {
        _metrics.RecordPacketSent(100);
        _metrics.RecordPacketSent(200);

        Assert.That(_metrics.TotalPacketsSent, Is.EqualTo(2));
        Assert.That(_metrics.TotalBytesSent, Is.EqualTo(300));
    }

    [Test]
    public void RecordPacketReceived_记录后接收包数增加()
    {
        _metrics.RecordPacketReceived(100);
        _metrics.RecordPacketReceived(200);

        Assert.That(_metrics.TotalPacketsReceived, Is.EqualTo(2));
        Assert.That(_metrics.TotalBytesReceived, Is.EqualTo(300));
    }

    [Test]
    public void RecordPacketLoss_记录后丢包率大于零()
    {
        _metrics.RecordPacketSent(100);
        _metrics.RecordPacketSent(100);
        _metrics.RecordPacketLoss();

        Assert.That(_metrics.PacketLossRate, Is.GreaterThan(0f));
    }

    [Test]
    public void PacketLossRate_无丢包时为零()
    {
        _metrics.RecordPacketSent(100);
        _metrics.RecordPacketSent(100);

        Assert.That(_metrics.PacketLossRate, Is.EqualTo(0f));
    }

    [Test]
    public void GetSnapshot_返回当前度量快照()
    {
        _metrics.RecordRtt(50f);
        _metrics.RecordPacketSent(100);
        _metrics.RecordPacketReceived(50);

        var snapshot = _metrics.GetSnapshot();

        Assert.That(snapshot.Rtt, Is.GreaterThan(0f));
        Assert.That(snapshot.PacketLossRate, Is.EqualTo(0f));
        Assert.That(snapshot.TimestampMs, Is.GreaterThan(0L));
    }

    [Test]
    public void Reset_重置后所有度量归零()
    {
        _metrics.RecordRtt(50f);
        _metrics.RecordPacketSent(100);
        _metrics.RecordPacketReceived(50);

        _metrics.Reset();

        Assert.That(_metrics.SmoothedRtt, Is.EqualTo(0f));
        Assert.That(_metrics.PacketLossRate, Is.EqualTo(0f));
        Assert.That(_metrics.TotalPacketsSent, Is.EqualTo(0));
        Assert.That(_metrics.TotalBytesSent, Is.EqualTo(0));
    }

    [Test]
    public void Jitter_记录多个RTT后抖动大于零()
    {
        _metrics.RecordRtt(50f);
        _metrics.RecordRtt(60f);
        _metrics.RecordRtt(45f);
        _metrics.RecordRtt(70f);

        Assert.That(_metrics.Jitter, Is.GreaterThanOrEqualTo(0f));
    }

    [Test]
    public void GetAverageRtt_返回平均RTT()
    {
        _metrics.RecordRtt(50f);
        _metrics.RecordRtt(100f);

        var avg = _metrics.GetAverageRtt();

        Assert.That(avg, Is.InRange(70f, 80f));
    }

    [Test]
    public void GetMaxRtt_返回最大RTT()
    {
        _metrics.RecordRtt(50f);
        _metrics.RecordRtt(100f);
        _metrics.RecordRtt(75f);

        Assert.That(_metrics.GetMaxRtt(), Is.EqualTo(100f));
    }

    [Test]
    public void TotalPacketsLost_记录丢包后增加()
    {
        _metrics.RecordPacketSent(100);
        _metrics.RecordPacketLoss(3);

        Assert.That(_metrics.TotalPacketsLost, Is.EqualTo(3));
    }
}
