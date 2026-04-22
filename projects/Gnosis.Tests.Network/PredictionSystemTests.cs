using Gnosis.Network.Prediction;
using Gnosis.Testing;
using NUnit.Framework;

namespace Gnosis.Tests.Network;

public class PredictionSystemTests : GnosisTester
{
    private PredictionSystem _system = null!;

    public override void Setup()
    {
        base.Setup();
        _system = new PredictionSystem();
    }

    [Test]
    public void RecordPrediction_记录后历史数量增加()
    {
        _system.RecordPrediction(1, [1.0f, 2.0f, 3.0f]);

        Assert.That(_system.PredictionHistoryCount, Is.EqualTo(1));
    }

    [Test]
    public void RecordPrediction_多次记录后历史数量增加()
    {
        _system.RecordPrediction(1, [1.0f, 2.0f, 3.0f]);
        _system.RecordPrediction(2, [4.0f, 5.0f, 6.0f]);
        _system.RecordPrediction(3, [7.0f, 8.0f, 9.0f]);

        Assert.That(_system.PredictionHistoryCount, Is.EqualTo(3));
    }

    [Test]
    public void Reconcile_服务器状态与预测一致时无校正()
    {
        _system.RecordPrediction(1, [1.0f, 2.0f, 3.0f]);

        var correction = _system.Reconcile(1, [1.0f, 2.0f, 3.0f], 0.01f);

        Assert.That(correction.IsCorrected, Is.False);
    }

    [Test]
    public void Reconcile_服务器状态与预测不一致时触发校正()
    {
        _system.RecordPrediction(1, [1.0f, 2.0f, 3.0f]);

        var correction = _system.Reconcile(1, [10.0f, 20.0f, 30.0f], 0.01f);

        Assert.That(correction.IsCorrected, Is.True);
    }

    [Test]
    public void Reconcile_误差在阈值内不校正()
    {
        _system.RecordPrediction(1, [1.0f, 2.0f, 3.0f]);

        var correction = _system.Reconcile(1, [1.005f, 2.005f, 3.005f], 0.01f);

        Assert.That(correction.IsCorrected, Is.False);
    }

    [Test]
    public void Reconcile_无预测历史时返回无校正()
    {
        var correction = _system.Reconcile(1, [1.0f, 2.0f, 3.0f], 0.01f);

        Assert.That(correction.IsCorrected, Is.False);
    }

    [Test]
    public void Reconcile_校正后返回服务器状态()
    {
        _system.RecordPrediction(1, [1.0f, 2.0f, 3.0f]);

        var serverState = new float[] { 10.0f, 20.0f, 30.0f };
        var correction = _system.Reconcile(1, serverState, 0.01f);

        Assert.That(correction.CorrectedState, Is.EqualTo(serverState));
    }

    [Test]
    public void ClearHistory_清除后历史数量为零()
    {
        _system.RecordPrediction(1, [1.0f, 2.0f, 3.0f]);
        _system.RecordPrediction(2, [4.0f, 5.0f, 6.0f]);

        _system.ClearHistory();

        Assert.That(_system.PredictionHistoryCount, Is.EqualTo(0));
    }

    [Test]
    public void SetMaxHistorySize_超过最大历史时自动清理旧记录()
    {
        _system.SetMaxHistorySize(3);

        _system.RecordPrediction(1, [1.0f]);
        _system.RecordPrediction(2, [2.0f]);
        _system.RecordPrediction(3, [3.0f]);
        _system.RecordPrediction(4, [4.0f]);

        Assert.That(_system.PredictionHistoryCount, Is.EqualTo(3));
    }

    [Test]
    public void SmoothCorrection_校正结果在预测与服务器状态之间()
    {
        _system.RecordPrediction(1, [0.0f]);
        var serverState = new float[] { 100.0f };

        var correction = _system.Reconcile(1, serverState, 0.01f);
        var smoothed = _system.SmoothCorrection([0.0f], correction.CorrectedState!, 0.5f);

        Assert.That(smoothed[0], Is.InRange(0.0f, 100.0f));
    }
}
